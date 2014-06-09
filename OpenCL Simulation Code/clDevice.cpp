#include "clDevice.h"

clDevice::clDevice(cl_uint numDevices, cl_device_id* devices)
{
	this->numDevices = numDevices;
	this->devices = *(devices);
}

clDevice::clDevice(cl_device_id devices)
{
	this->numDevices = 1;
	this->devices = devices;
}

cl_device_id* clDevice::DevPtr()
{
	return &devices;
}