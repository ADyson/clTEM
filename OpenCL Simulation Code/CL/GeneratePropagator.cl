__kernel void clGeneratePropagator(__global float2* Propagator, __global float* clXFrequencies, __global float* clYFrequencies, int width, int height, float dz, float wavel, float kmax)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	if(xid < width && yid < height)
	{
		int Index = xid + width*yid;
		float k0x = clXFrequencies[xid];
		float k0y = clYFrequencies[yid];
		float Pi = 3.14159265f;

		k0x*=k0x;
		k0y*=k0y;

		if (sqrt(k0x+k0y) < kmax)
		{
			Propagator[Index].x = cos(Pi*dz*wavel*(k0x+k0y));
			Propagator[Index].y = -1*sin(Pi*dz*wavel*(k0x+k0y));
		} else 
		{
			Propagator[Index].x = 0.0f;
			Propagator[Index].y = 0.0f;
		}		
	}
}