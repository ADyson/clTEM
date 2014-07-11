float bessi0(float x)
{
	int i;
	float ax, sum, t;

	float i0a[] = { 1.0, 3.5156229, 3.0899424, 1.2067492,
		0.2659732, 0.0360768, 0.0045813 };

	float i0b[] = { 0.39894228, 0.01328592, 0.00225319,
		-0.00157565, 0.00916281, -0.02057706, 0.02635537,
		-0.01647633, 0.00392377};

	ax = abs( x );

	if( ax <= 3.75 ) 
	{
		t = x / 3.75;
		t = t * t;
		sum = i0a[6];

		for( i=5; i>=0; i--) sum = sum*t + i0a[i]; 

	} else
	{
		t = 3.75 / ax;
		sum = i0b[8];
		for( i=7; i>=0; i--) sum = sum*t + i0b[i];
		sum = native_exp( ax ) * sum / native_sqrt( ax );
	}

	return( sum );
}

float bessk0(float x)
{
	int i;
	float ax, x2, sum;
	float k0a[] = { -0.57721566, 0.42278420, 0.23069756, 0.03488590, 0.00262698, 0.00010750, 0.00000740};
	float k0b[] = { 1.25331414, -0.07832358, 0.02189568, -0.01062446, 0.00587872, -0.00251540, 0.00053208};

	ax = abs( x );

	if( (ax > 0.0)  && ( ax <=  2.0 ) )
	{
		x2 = ax/2.0;
		x2 = x2 * x2;
		sum = k0a[6];
		for( i=5; i>=0; i--) sum = sum*x2 + k0a[i];
		sum = -log(ax/2.0) * bessi0(x) + sum;

	} else if( ax > 2.0 ) 
	{
		x2 = 2.0/ax;
		sum = k0b[6];

		for( i=5; i>=0; i--) sum = sum*x2 + k0b[i];

		sum = native_exp( -ax ) * sum / native_sqrt( ax );

	} else sum = 1.0e20;

	return ( sum );
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
	// __local float atz[256];
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
				// atz[lid] = clAtomZPos[gid];
				atZ[lid] = clAtomZNum[gid];
			}

			barrier(CLK_LOCAL_MEM_FENCE);

			float p2=0;
			for (int l = 0; l < end-start; l++) 
			{
				int ZNum = atZ[l];
			
					float rad = native_sqrt((xid*pixelscale-atx[l])*(xid*pixelscale-atx[l]) + (yid*pixelscale-aty[l])*(yid*pixelscale-aty[l]));

					if(rad < 0.25f * pixelscale)
						rad = 0.25f * pixelscale;

					float p1 = 0;

					if( rad < 3.0f) // Should also make sure is not too small
					{
						// do vzatom2 (from CUDA version) here. Can't pass pointer so jsut do function here instead of calling sperate function.
						// vzatom2( ZNum, rad, clfParams) CAREFUL in cuda radius is squared, here it is not

						int i;
						float suml, sumg, x;

						/* Lorenzian, Gaussian consts */
						if( rad < 0.25 ) rad = 0.25f; // was 0.15

						/* avoid singularity at r=0 */
					   suml = sumg = 0.0;

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