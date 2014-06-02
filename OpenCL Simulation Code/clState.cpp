#include "clState.h"
#include "stringFunc.h"

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
std::vector<std::string> clState::devicenamesShort;
std::vector<std::string> clState::devicenamesLong;
std::vector<cl_uint> clState::numdevices;
std::vector<size_t> clState::Allocated;

// Call this once somewhere during plugin load.
void clState::Setup()
{
	size_t valueSize;
	char* value;
	char* Pvalue;

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

		clGetPlatformInfo(platforms[i], CL_PLATFORM_NAME, 0, NULL, &valueSize);
		Pvalue = (char*) malloc(valueSize);
        clGetPlatformInfo(platforms[i], CL_PLATFORM_NAME, valueSize, Pvalue, NULL);
		std::string pName = Pvalue;

        // for each device get and store name, platform, and device number
        for (int j = 0; j < numdevices[i]; j++) 
		{
			// get device name
            clGetDeviceInfo(devices[i][j], CL_DEVICE_NAME, 0, NULL, &valueSize);
            value = (char*) malloc(valueSize);
            clGetDeviceInfo(devices[i][j], CL_DEVICE_NAME, valueSize, value, NULL);
			std::string dName = value;
			devicenamesLong.push_back(std::to_string(i) + ": " + trim(pName) + ", " + std::to_string(j) + ": " + trim(dName));
			devicenamesShort.push_back(std::to_string(i) + "," + std::to_string(j) + ": " + trim(dName));
			deviceid.push_back(j);
			deviceplatform.push_back(i);
            free(value);
		}
		free(Pvalue);
	}

	free(platforms);
	
	if(status!=CL_SUCCESS)
	{
		throw "OpenCL Error setting device/platform";
	}
}

int clState::GetNumDevices()
{
		return devicenamesShort.size();
}

std::string clState::GetDeviceString(int i, bool getShort)
{
	if (getShort)
	{
		return devicenamesShort[i];
	}
	else
	{
		return devicenamesLong[i];
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

int clState::RegisterMemory(size_t size)
{
	Allocated.push_back(size);
	int Index = Allocated.size()-1;
	return Index;
}

void clState::DeRegisterMemory(int index)
{
	Allocated[index] = 0;
}

size_t clState::GetTotalSize()
{
	size_t sum = 0;
	for each(size_t s in Allocated)
	{
		sum += s;
	}
	return sum;
}