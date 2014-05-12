#include "UnmanagedOpenCL.h"



int UnmanagedOpenCL::SetupStructure(std::string filepath)
{
	if (GotStruct) {
		Structure->ClearStructure();
	} 
	else {
		Structure = new MultisliceStructure(context,clq,cldev);
		Structure->ImportAtoms(filepath);
	}

	// TODO: properly implement success/failure reporting
	return 1;
};

int UnmanagedOpenCL::UploadParameterisation()
{
	char inputparamsFilename[] = "fparams.dat";

	// Read in fparams data for calculating projected atomic potential.

	std::ifstream inparams;
	inparams.open(inputparamsFilename , std::ios::in);
	
	std::vector<AtomParameterisation> fparams;
	AtomParameterisation buffer;

	if (!inparams) 
	{
		throw "Can't find atomic parameterisation file";
	}
	
	
	while ((inparams >> buffer.a >> buffer.b >> buffer.c >> buffer.d >> buffer.e >> buffer.f >> buffer.g >> buffer.h >> buffer.i >> buffer.j >> buffer.k >> buffer.l))
	{
		fparams.push_back (buffer);
	}

	inparams.close();

	Structure->AtomicStructureParameterisation = clCreateBuffer(context,CL_MEM_READ_ONLY,12*103*sizeof(float),0,&status);
	clEnqueueWriteBuffer(clq->cmdQueue,Structure->AtomicStructureParameterisation,CL_TRUE,0,12*103*sizeof(float),&fparams[0],0,NULL,NULL);
	fparams.clear();

	return 0;
};

void UnmanagedOpenCL::InitialiseSimulation(int resolution)
{
	TS = new TEMSimulation(context,clq,cldev,temparams,stemparams);
	TS->Initialise(resolution,Structure);
};

// Calls different initialiser to make a probe wavefunction instead of plane wave
void UnmanagedOpenCL::InitialiseSTEMSimulation(int resolution)
{
	TS = new TEMSimulation(context,clq,cldev,temparams,stemparams);
	TS->InitialiseSTEM(resolution,Structure);
};

void UnmanagedOpenCL::MultisliceStep(int stepnumber, int steps)
{
	TS->MultisliceStep(stepnumber,steps);
};

void UnmanagedOpenCL::SetParamsTEM(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture, float astig2mag, float astig2ang, float b2mag, float b2ang)
{
	temparams->defocus = df;
	temparams->astigmag = astigmag;
	temparams->astigang = astigang;
	temparams->kilovoltage = kilovoltage;
	temparams->spherical = spherical;
	temparams->beta = beta;
	temparams->delta = delta;
	temparams->aperturesizemrad = aperture;
	temparams->astig2mag = astig2mag;
	temparams->astig2ang = astig2ang;
	temparams->b2mag = b2mag;
	temparams->b2ang = b2ang;

};

void UnmanagedOpenCL::SetParamsSTEM(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture)
{
	stemparams->defocus = df;
	stemparams->astigmag = astigmag;
	stemparams->astigang = astigang;
	stemparams->kilovoltage = kilovoltage;
	stemparams->spherical = spherical;
	stemparams->beta = beta;
	stemparams->delta = delta;
	stemparams->aperturesizemrad = aperture;
};

UnmanagedOpenCL::UnmanagedOpenCL() 
{
	// TODO: make this changeable
	int PLATFORM = 0;
	int DEVNUMBER = 0;

	GotStruct = false;

	// Setup OpenCL device etc here...
	OpenCLAvailable = false;
	context = NULL;
	numDevices = 0;
	devices = NULL;

	// Maybe Can Do OpenCL setup and device registering here - Print to Ouput with device data?
	// Discover and initialize available platforms
	cl_uint numPlatforms = 0;
	cl_platform_id * platforms = NULL;

	// Use clGetPlatformIds() to retrieve the number of platforms
	status = clGetPlatformIDs(0,NULL,&numPlatforms);

	// Allocate enough space for each platform
	platforms = (cl_platform_id*)malloc(numPlatforms*sizeof(cl_platform_id));

	// Fill in platforms with clGetPlatformIDs()
	status = clGetPlatformIDs(numPlatforms,platforms,NULL);

	// Discover and initialize available devices	
	// use clGetDeviceIDs() to retrieve number of devices present
	status = clGetDeviceIDs(platforms[PLATFORM],CL_DEVICE_TYPE_ALL,0,NULL,&numDevices);

	// Allocate enough space for each device
	devices = (cl_device_id*)malloc(numDevices*sizeof(cl_device_id));

	// Fill in devices with clGetDeviceIDs()
	status = clGetDeviceIDs(platforms[PLATFORM],CL_DEVICE_TYPE_ALL,numDevices,devices,NULL);

	// Most of initialisation is done, would be nice to print device information...
	//Getting the device name
	size_t deviceNameLength = 4096;
	size_t actualSize;
	char* tempDeviceName = (char*)malloc(4096);
	char* deviceName;
	status |= clGetDeviceInfo(devices[DEVNUMBER], CL_DEVICE_NAME, deviceNameLength, tempDeviceName, &actualSize);

	if(status == CL_SUCCESS)
	{
		deviceName = (char*)malloc(actualSize);
		memcpy(deviceName, tempDeviceName, actualSize);
		free(tempDeviceName);
		std::string devName(deviceName);
		OpenCLAvailable = true;
	}

	if(status!=CL_SUCCESS)
	{

	}

	context = clCreateContext(NULL,numDevices,devices,NULL,NULL,&status);

	clq = new clQueue();
	clq->SetupQueue(context,devices[DEVNUMBER]);
	cldev = new clDevice(numDevices,devices);

	temparams = new TEMParameters();
	stemparams = new STEMParameters();
};

UnmanagedOpenCL::~UnmanagedOpenCL() 
{

};
