#pragma once
#include "CL\cl.h"
#include "clKernel.h"

class clState
{
private:
	clState(void);
	~clState(void);
public:
	static cl_int status;
	static cl_context context;
	static clDevice* cldev;
	static clQueue* clq;
	static bool OpenCLAvailable;
	static std::vector<cl_device_id*> devices;
	static std::vector<std::string> devicenames;
	static std::vector<int> deviceid;
	static std::vector<int> deviceplatform;
	static std::vector<cl_uint> numdevices;
	static void Setup();
	static cl_int GetStatus();
	static cl_context GetContext();
	static clDevice* GetDevicePtr();
	static clQueue* GetQueuePtr();
	static void SetDevice(int index);
};

