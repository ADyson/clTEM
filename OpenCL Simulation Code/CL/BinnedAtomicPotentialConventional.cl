float bessi0(float x)
{
	int i;
	float ax, sum, t;

	float i0a[] = { 1.0f, 3.5156229f, 3.0899424f, 1.2067492f, 0.2659732f, 0.0360768f, 0.0045813f };

	float i0b[] = { 0.39894228f, 0.01328592f, 0.00225319f, -0.00157565f, 0.00916281f, -0.02057706f, 0.02635537f, -0.01647633f, 0.00392377f};

	ax = fabs( x );

	if( ax <= 3.75f ) 
	{
		t = x / 3.75f;
		t = t * t;
		sum = i0a[6];

		for( i=5; i>=0; i--) sum = sum*t + i0a[i]; 

	} else
	{
		t = 3.75f / ax;
		sum = i0b[8];
		for( i=7; i>=0; i--) sum = sum*t + i0b[i];
		sum = native_exp( ax ) * sum / native_sqrt( ax );
	}

	return sum;
}

float bessk0(float x)
{
	int i;
	float ax, x2, sum;
	float k0a[] = { -0.57721566f, 0.42278420f, 0.23069756f, 0.03488590f, 0.00262698f, 0.00010750f, 0.00000740f };
	float k0b[] = { 1.25331414f, -0.07832358f, 0.02189568f, -0.01062446f, 0.00587872f, -0.00251540f, 0.00053208f };

	ax = fabs( x );

	if( (ax > 0.0f)  && ( ax <=  2.0f ) )
	{
		x2 = ax/2.0f;
		x2 = x2 * x2;
		sum = k0a[6];
		for( i=5; i>=0; i--) sum = sum*x2 + k0a[i];
		sum = -log(ax/2.0f) * bessi0(x) + sum;

	} else if( ax > 2.0f ) 
	{
		x2 = 2.0f/ax;
		sum = k0b[6];

		for( i=5; i>=0; i--) sum = sum*x2 + k0b[i];

		sum = native_exp( -ax ) * sum / native_sqrt( ax );

	} else sum = 1.0e20;

	return sum;
}

__kernel void clBinnedAtomicPotentialConventional(__global float2* Potential, 
										  __global const float* restrict clAtomXPos, 
										  __global const float* restrict clAtomYPos, 
										  __global const float* restrict clAtomZPos, 
										  __global const int* restrict clAtomZNum, 
										  __constant float* clfParams, 
										  __global const int* restrict clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	int lid = get_local_id(0) + get_local_size(0)*get_local_id(1);
	int Index = xid + width*yid;
	int topz = slice;
	int bottomz = slice;
	float sumz = 0.0f;
	int gx = get_group_id(0);
	int gy = get_group_id(1);
	if(topz < 0 )
		topz = 0;
	if(bottomz >= slices )
		bottomz = slices-1;

	__local float atx[256];
	__local float aty[256];
	__local int atZ[256];

	int startj = fmax(floor((gy * get_local_size(1) * yBlocks * pixelscale)/ (MaxY-MinY)) - loadBlocksY,0) ;
	int endj = fmin(ceil(((gy+1) * get_local_size(1) * yBlocks * pixelscale)/ (MaxY-MinY)) + loadBlocksY,yBlocks-1);
	int starti = fmax(floor((gx * get_local_size(0) * xBlocks * pixelscale) / (MaxX-MinX)) - loadBlocksX,0) ;
	int endi = fmin(ceil(((gx+1) * get_local_size(0) * xBlocks * pixelscale)/ (MaxX-MinX)) + loadBlocksX,xBlocks-1);

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
				atZ[lid] = clAtomZNum[gid];
			}

			barrier(CLK_LOCAL_MEM_FENCE);

			for (int l = 0; l < end-start; l++) 
			{
				int ZNum = atZ[l];
			
				float rad = native_sqrt((xid*pixelscale-atx[l])*(xid*pixelscale-atx[l]) + (yid*pixelscale-aty[l])*(yid*pixelscale-aty[l]));

				if(rad < 0.25f * pixelscale)
					rad = 0.25f * pixelscale;

				if( rad < 3.0f) // Should also make sure is not too small
				{
					int i;
					float suml, sumg, x;

					/* Lorenzian, Gaussian consts */

					/* avoid singularity at r=0 */
					suml = sumg = 0.0f;

					/* Lorenztians */
					x = 2.0f*3.141592654f*rad;

					for( i=0; i<2*3; i+=2 )
						suml += clfParams[(ZNum-1)*12+i]* bessk0( x*native_sqrt(clfParams[(ZNum-1)*12+i+1]) );

					/* Gaussians */
					x = 3.141592654f*rad;
					x = x*x;

					for( i=2*3; i<2*(3+3); i+=2 )
						sumg += clfParams[(ZNum-1)*12+i] * native_exp (-x/clfParams[(ZNum-1)*12+i+1]) / clfParams[(ZNum-1)*12+i+1];

					sumz += 300.8242834f*suml + 150.4121417f*sumg;
				}

			}

			barrier(CLK_LOCAL_MEM_FENCE);
		}
	}
	if(xid < width && yid < height)
	{
		Potential[Index].x = native_cos(sigma*sumz);
		Potential[Index].y = native_sin(sigma*sumz);
	}
}