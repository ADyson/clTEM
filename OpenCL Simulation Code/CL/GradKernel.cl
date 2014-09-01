__kernel void clGrad( __global float2* Input, __global float* restrict clXFrequencies, 
						__global float* restrict clYFrequencies,
						int width, int height)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	int Index = xid + width* yid;

	if(xid < width && yid < height)
	{
		Input[Index].x *= -4.0f * 3.1415129f * 3.141592f * (clXFrequencies[xid]*clXFrequencies[xid] + clYFrequencies[yid]*clYFrequencies[yid]);
		Input[Index].y *= -4.0f * 3.1415129f * 3.141592f * (clXFrequencies[xid]*clXFrequencies[xid] + clYFrequencies[yid]*clYFrequencies[yid]);
	}
}

