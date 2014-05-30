#include "clQueue.h"


cl_int clQueue::SetupQueue(cl_context &context, cl_device_id device)
{
	this->cmdQueue = clCreateCommandQueue(context,device,0,&status);

	if(!status==CL_SUCCESS)
	{
		return status;
	}


	return status;
}

clQueue::clQueue(void)
{
}