__kernel void clBinnedAtomicPotential(__global float2* Potential, __global float* clAtomXPos, __global float* clAtomYPos, __global float* clAtomZPos, __global int* clAtomZNum, __constant float* clfParams, __global int* clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	if(xid < width && yid < height)
	{
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

		for(int k = topz; k <= bottomz; k++)
		{
			for (int j = floor((gy * get_local_size(1) * yBlocks * pixelscale/ ( MaxY-MinY )) - loadBlocksY ); j <= ceil(((gy+1) * get_local_size(1) * yBlocks * pixelscale/ ( MaxY - MinY )) + loadBlocksY); j++)
			{
				for (int i = floor((gx * get_local_size(0) * xBlocks * pixelscale / (MaxX-MinX )) - loadBlocksX ); i <= ceil(((gx+1) * get_local_size(0) * xBlocks * pixelscale/ ( MaxX - MinX )) + loadBlocksX); i++)
				{
				// Check bounds to avoid unneccessarily loading blocks when i am at edge of sample.
					if(0 <= j && j < yBlocks)
					{
						// Check bounds to avoid unneccessarily loading blocks when i am at edge of sample.
						if (0 <= i && i < xBlocks )
						{
							// Check if there is an atom in bin, arrays are not overwritten when there are no extra atoms so if you don't check could add contribution more than once.
							for (int l = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + i]; l < clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + i+1]; l++)
							{
								for (int h = 0; h < 10; h++)
								{
									float rad = sqrt((xid*pixelscale-clAtomXPos[l] + MinX)*(xid*pixelscale-clAtomXPos[l] + MinX) + (yid*pixelscale-clAtomYPos[l] + MinY)*(yid*pixelscale-clAtomYPos[l] + MinY) + (z - (h*(dz/10.0f))-clAtomZPos[l])*(z - (h*(dz/10.0f))-clAtomZPos[l]));

									if(rad < 0.25f * pixelscale)
										rad = 0.25f * pixelscale;

									int ZNum = clAtomZNum[l];
									if( rad < 5.0f) // Should also make sure is not too small
									{
										sumz += (dz/10.0f)*(150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12  ]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+1  ])));
										sumz += (dz/10.0f)*(150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12+2]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+2+1])));
										sumz += (dz/10.0f)*(150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12+4]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+4+1])));
										sumz += (dz/10.0f)*(266.5157269f * clfParams[(ZNum-1)*12+6] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+6+1]) * sqrt(clfParams[(ZNum-1)*12+6+1])/(clfParams[(ZNum-1)*12+6+1]*clfParams[(ZNum-1)*12+6+1]*clfParams[(ZNum-1)*12+6+1]));
										sumz += (dz/10.0f)*(266.5157269f * clfParams[(ZNum-1)*12+8] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+8+1]) * sqrt(clfParams[(ZNum-1)*12+8+1])/(clfParams[(ZNum-1)*12+8+1]*clfParams[(ZNum-1)*12+8+1]*clfParams[(ZNum-1)*12+8+1]));
										sumz += (dz/10.0f)*(266.5157269f * clfParams[(ZNum-1)*12+10] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+10+1]) * sqrt(clfParams[(ZNum-1)*12+10+1])/(clfParams[(ZNum-1)*12+10+1]*clfParams[(ZNum-1)*12+10+1]*clfParams[(ZNum-1)*12+10+1]));
									}
								}
							}
						}
					}
				}
			}
		}
		Potential[Index].x = cos(sigma*sumz);
		Potential[Index].y = sin(sigma*sumz);
	}
}