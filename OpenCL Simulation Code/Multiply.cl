__kernel void clComplexMultiply(__global float2* In1, __global float2* In2, __global float2* Out, int width, int height)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	if(xid < width && yid < height)
	{
		int Index = xid + width*yid;

		Out[Index].x = In1[Index].x * In2[Index].x - In1[Index].y * In2[Index].y;
		Out[Index].y = In1[Index].x * In2[Index].y + In1[Index].y * In2[Index].x;
		
	}
}