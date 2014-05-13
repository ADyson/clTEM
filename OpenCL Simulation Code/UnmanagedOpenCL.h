#include <CL\OpenCL.h>
#include "clKernel.h"
#include "MultisliceStructure.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include "TEMSimulation.h"
#include "CommonStructs.h"

#pragma unmanaged

struct AtomParameterisation
{
	float a,b,c,d,e,f,g,h,i,j,k,l;
};


class UnmanagedOpenCL
{
public:
	// Structure Stuff
	bool GotStruct;
	MultisliceStructure* Structure;
	TEMSimulation* TS;

	TEMParameters* temparams;
	STEMParameters* stemparams;

	// Methods
	int SetupStructure(std::string filepath);
	int UploadParameterisation();

	void SetParamsTEM(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture, float astig2mag, float astig2ang, float b2mag, float b2ang);
	void SetParamsSTEM(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture);

	void InitialiseSimulation(int resolution);
	void InitialiseSTEMSimulation(int resolution, int posx, int posy);
	void MultisliceStep(int stepno, int steps);


	// OpenCl Context Stuff
	bool OpenCLAvailable;
	cl_command_queue cmdQueue;
	cl_context context;
	// Use this to check status after every API call
	cl_int status;
	cl_uint numDevices;
	cl_device_id* devices;
	clDevice* cldev;
	clQueue* clq;

	UnmanagedOpenCL();
	~UnmanagedOpenCL();
};