__kernel void clBinnedAtomicPotential(__global float2* Potential, 
										  __global const float* restrict clAtomXPos, 
										  __global const float* restrict clAtomYPos, 
										  __global const float* restrict clAtomZPos, 
										  __global const int* restrict clAtomZNum, 
										  __global const float* restrict clfParams, 
										  __global const int* restrict clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma)
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
	__local float fp[12*103];

	int startj = fmax(floor((gy * get_local_size(1) * yBlocks * pixelscale)/ (MaxY-MinY)) - loadBlocksY,0) ;
	int endj = fmin(ceil(((gy+1) * get_local_size(1) * yBlocks * pixelscale)/ (MaxY-MinY)) + loadBlocksY,yBlocks-1);
	int starti = fmax(floor((gx * get_local_size(0) * xBlocks * pixelscale) / (MaxX-MinX)) - loadBlocksX,0) ;
	int endi = fmin(ceil(((gx+1) * get_local_size(0) * xBlocks * pixelscale)/ (MaxX-MinX)) + loadBlocksX,xBlocks-1);

	int lz = 0;
	float rad;
	float p2 = 0;
	float p1 = 0;
	float nrrad;

	// Populate shared memory with fparams;
	for(int i = lid ; i < 12*103 ; i += get_local_size(0)*get_local_size(1))
	{
		fp[i] = clfParams[i];
	}

	barrier(CLK_LOCAL_MEM_FENCE);

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

			for (int l = 0; l < end-start; l++) 
			{
				int ZNum = atZ[l];
				int stride = 12*(ZNum-1);

				float subrad = (xid*pixelscale-atx[l])*(xid*pixelscale-atx[l]) + (yid*pixelscale-aty[l])*(yid*pixelscale-aty[l]);

				rad = fmax(native_sqrt(subrad + (z - 0.0f*dz/10.0f-atz[l])*(z - 0.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 1.0f*dz/10.0f-atz[l])*(z - 1.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz += (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 2.0f*dz/10.0f-atz[l])*(z - 2.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 3.0f*dz/10.0f-atz[l])*(z - 3.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 4.0f*dz/10.0f-atz[l])*(z - 4.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 5.0f*dz/10.0f-atz[l])*(z - 5.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 6.0f*dz/10.0f-atz[l])*(z - 6.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 7.0f*dz/10.0f-atz[l])*(z - 7.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 8.0f*dz/10.0f-atz[l])*(z - 8.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{	
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz += (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 9.0f*dz/10.0f-atz[l])*(z - 9.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
					p2 = p1;
				}

				rad = fmax(native_sqrt(subrad + (z - 10.0f*dz/10.0f-atz[l])*(z - 10.0f*dz/10.0f-atz[l])),0.25f*pixelscale);
				p1 = 0;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					nrrad = native_recip(rad);
					p1 += (150.4121417f * nrrad * fp[stride+0]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+1])));
					p1 += (150.4121417f * nrrad * fp[stride+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+2+1])));
					p1 += (150.4121417f * nrrad * fp[stride+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(fp[stride+4+1])));
					p1 += (266.5157269f * fp[stride+6] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+6+1])) * native_powr(fp[stride+6+1],-1.5f));
					p1 += (266.5157269f * fp[stride+8] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+8+1])) * native_powr(fp[stride+8+1],-1.5f));
					p1 += (266.5157269f * fp[stride+10] * native_exp (native_divide(-3.141592f*rad*3.141592f*rad,fp[stride+10+1])) * native_powr(fp[stride+10+1],-1.5f));

					sumz +=  (p1+p2)*0.5f;
				}

			}

			barrier(CLK_LOCAL_MEM_FENCE);
		}
	}
	if(xid < width && yid < height)
	{
		Potential[Index].x = native_cos((dz/10.0f)*sigma*sumz);
		Potential[Index].y = native_sin((dz/10.0f)*sigma*sumz);
	}
}
