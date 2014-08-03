__kernel void clBinnedAtomicPotentialOpt(__global float2* Potential, 
										  __global const float* restrict clAtomXPos, 
										  __global const float* restrict clAtomYPos, 
										  __global const float* restrict clAtomZPos, 
										  __global const int* restrict clAtomZNum, 
										  __constant float* clfParams, 
										  __global const int* restrict clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma,
										  float startx, float starty)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	int lid = get_local_id(0) + get_local_size(0)*get_local_id(1);
	int Index = xid + width*yid;
	int topz = slice - loadSlicesZ;
	int bottomz = slice + loadSlicesZ;
	float sumz = 0.0f;
	int gx = get_group_id(0);
	int gy = get_group_id(1);
	if(topz < 0 )
		topz = 0;
	if(bottomz >= slices )
		bottomz = slices-1;

	__local float atx[256];
	__local float aty[256];
	__local float atz[256];
	__local int atZ[256];

	int startj = fmax(floor(((starty + gy * get_local_size(1) * pixelscale) * yBlocks ) / (MaxY-MinY)) - loadBlocksY,0) ;
	int endj = fmin(ceil(((starty + (gy+1) * get_local_size(1) * pixelscale) * yBlocks) / (MaxY-MinY)) + loadBlocksY,yBlocks-1);
	int starti = fmax(floor(((startx + gx * get_local_size(0) * pixelscale) * xBlocks)  / (MaxX-MinX)) - loadBlocksX,0) ;
	int endi = fmin(ceil(((startx + (gx+1) * get_local_size(0) * pixelscale) * xBlocks) / (MaxX-MinX)) + loadBlocksX,xBlocks-1);

	for(int k = topz; k <= bottomz; k++)
	{
		for (int j = startj ; j <= endj; j++)
		{
			//Need list of atoms to load, so we can load in sequence
			int start = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + starti];
			int end = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + endi + 1];
				
			int gid = start + lid;

			if(lid < end-start)
			{
				atx[lid] = clAtomXPos[gid];
				aty[lid] = clAtomYPos[gid];
				atz[lid] = clAtomZPos[gid];
				atZ[lid] = clAtomZNum[gid];
			}

			barrier(CLK_LOCAL_MEM_FENCE);

			float p2=0;
			for (int l = 0; l < end-start; l++) 
			{
				float xyrad2 = (startx + xid*pixelscale-atx[l])*(startx + xid*pixelscale-atx[l]) + (starty + yid*pixelscale-aty[l])*(starty + yid*pixelscale-aty[l]);

				int ZNum = atZ[l];
				for (int h = 0; h <= 20; h++)
				{
					float rad = native_sqrt(xyrad2 + (z - h*dz/20.0f -atz[l])*(z - h*dz/20.0f-atz[l]));

					if(rad < 0.25f * pixelscale)
						rad = 0.25f * pixelscale;

					float p1 = 0;

					if( rad < 3.0f) // Should also make sure is not too small
					{
						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12  ]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+1  ])));
						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+2+1])));
						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+4+1])));
						p1 += (266.5157269f * clfParams[(ZNum-1)*12+6] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+6+1]) * native_powr(clfParams[(ZNum-1)*12+6+1],-1.5f));
						p1 += (266.5157269f * clfParams[(ZNum-1)*12+8] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+8+1]) * native_powr(clfParams[(ZNum-1)*12+8+1],-1.5f));
						p1 += (266.5157269f * clfParams[(ZNum-1)*12+10] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+10+1]) * native_powr(clfParams[(ZNum-1)*12+10+1],-1.5f));

						sumz += (h!=0) * (p1+p2)*0.5f;
						p2 = p1;
						//sumz +=p1;
					}
				}
			}

			barrier(CLK_LOCAL_MEM_FENCE);
		}
	}
	if(xid < width && yid < height)
	{
		Potential[Index].x = native_cos((dz/20.0f)*sigma*sumz);
		Potential[Index].y = native_sin((dz/20.0f)*sigma*sumz);
	}
}
