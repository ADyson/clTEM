#include "CL\OpenCL.h"

class clDevice
{
public:
	cl_uint numDevices;
	cl_device_id* devices;
	clDevice::clDevice(cl_uint numDevices, cl_device_id* devices);
	clDevice();
	~clDevice();
};

