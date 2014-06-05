#include <CL\OpenCL.h>
#include "clKernel.h"
#include "MultisliceStructure.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include "TEMSimulation.h"
#include "CommonStructs.h"
#include "clState.h"
#include <memory>


#pragma unmanaged

typedef std::unique_ptr<TEMSimulation> SimulationPtr;

struct AtomParameterisation
{
	float a,b,c,d,e,f,g,h,i,j,k,l;
};


class UnmanagedOpenCL
{
public:
	// Structure Stuff
	bool GotStruct;

	bool GotDevice;


	MultisliceStructure* Structure;
	SimulationPtr TS;

	TEMParameters* temparams;
	STEMParameters* stemparams;

	// Methods
	int SetupStructure(std::string filepath);
	int UploadParameterisation();

	void SetParamsTEM(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture, float astig2mag, float astig2ang, float b2mag, float b2ang);
	void SetParamsSTEM(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture);

	void InitialiseSimulation(int resolution);
	void InitialiseSTEMSimulation(int resolution);
	void MultisliceStep(int stepno, int steps);
	void MakeSTEMWaveFunction(float posx, float posy);

	void GetAtomCoords(float* x, float* y, float* z, int* a, int length);
	void SetDevice(int index);
	size_t MemoryUsed();

	UnmanagedOpenCL();
	~UnmanagedOpenCL();

	int getCLdevCount();
	std::string getCLdevString(int i, bool getShort);
};