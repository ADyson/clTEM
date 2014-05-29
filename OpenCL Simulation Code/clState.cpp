#include "clState.h"


clState::clState(void)
{
}


clState::~clState(void)
{
}

// initialise statics?
cl_int clState::status = 0;
cl_context clState::context = NULL;
clDevice* clState::cldev = NULL;
clQueue* clState::clq = NULL;
bool clState::OpenCLAvailable = false;
std::vector<cl_device_id*> clState::devices;
std::vector<int> clState::deviceid;
std::vector<int> clState::deviceplatform;
std::vector<std::string> clState::devicenames;
std::vector<cl_uint> clState::numdevices;

// Call this once somewhere during plugin load.
void clState::Setup()
{
	size_t valueSize;
	char* value;

	//Setup OpenCL
	context = NULL;
	
	// Maybe Can Do OpenCL setup and device registering here - Print to Ouput with device data?
	// Discover and initialize available platforms
	cl_uint numPlatforms = 0;
	cl_platform_id * platforms = NULL;


	   // get all platforms
    clGetPlatformIDs(0, NULL, &numPlatforms);
    platforms = (cl_platform_id*) malloc(sizeof(cl_platform_id) * numPlatforms);
    clGetPlatformIDs(numPlatforms, platforms, NULL);

    for (int i = 0; i < numPlatforms; i++) 
	{
		numdevices.push_back(0);
		devices.push_back(NULL);
       
		// get all devices
        clGetDeviceIDs(platforms[i], CL_DEVICE_TYPE_ALL, 0, NULL, &numdevices[i]);
        devices[i] = (cl_device_id*) malloc(sizeof(cl_device_id) * numdevices[i]);
        clGetDeviceIDs(platforms[i], CL_DEVICE_TYPE_ALL, numdevices[i], devices[i], NULL);

        // for each device get and store name, platform, and device number
        for (int j = 0; j < numdevices[i]; j++) 
		{
			// get device name
            clGetDeviceInfo(devices[i][j], CL_DEVICE_NAME, 0, NULL, &valueSize);
            value = (char*) malloc(valueSize);
            clGetDeviceInfo(devices[i][j], CL_DEVICE_NAME, valueSize, value, NULL);
			devicenames.push_back(value);
			deviceid.push_back(j);
			deviceplatform.push_back(i);
            free(value);
		}
	}

	free(platforms);
	
	if(status!=CL_SUCCESS)
	{
		throw "OpenCL Error setting device/platform";
	}
}

void clState::SetDevice(int index)
{
	context = clCreateContext(NULL,numdevices[deviceplatform[index]],devices[deviceplatform[index]],NULL,NULL,&status);
	clq = new clQueue();
	clq->SetupQueue(context,devices[deviceplatform[index]][deviceid[index]]);
	cldev = new clDevice(numdevices[deviceplatform[index]],devices[deviceplatform[index]]);
	OpenCLAvailable = true;
}


cl_context clState::GetContext()
{
	return context;
}

clDevice* clState::GetDevicePtr()
{
	return cldev;
}

clQueue* clState::GetQueuePtr()
{
	return clq;
}

cl_int clState::GetStatus()
{
	return status;
}