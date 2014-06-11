#include "CL\OpenCL.h"

class clDevice
{
public:
	cl_uint numDevices;
	cl_device_id devices;
	clDevice(cl_uint numDevices, cl_device_id* devices);
	clDevice(cl_device_id devices);
	cl_device_id* DevPtr();
	clDevice();
	~clDevice();
};

