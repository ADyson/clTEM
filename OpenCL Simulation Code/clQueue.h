#include "CL\OpenCL.h"

class clQueue
{
public:
	cl_command_queue cmdQueue;
	cl_int status;
	cl_int SetupQueue(cl_context &context,cl_device_id device);
	clQueue();
	~clQueue();
};
