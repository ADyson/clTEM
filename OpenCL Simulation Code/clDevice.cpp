#include "clDevice.h"

clDevice::clDevice(cl_uint numDevices, cl_device_id* devices)
{
	this->numDevices = numDevices;
	this->devices = devices;
}
