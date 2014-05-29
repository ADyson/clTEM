#include "UnmanagedOpenCL.h"

int UnmanagedOpenCL::SetupStructure(std::string filepath)
{
	if (GotStruct) {
		Structure->ClearStructure();
	} 
	
	Structure = new MultisliceStructure();
	Structure->ImportAtoms(filepath);
	Structure->filepath = filepath;
	Structure->GotDevice = GotDevice;
	GotStruct=true;
	

	// TODO: properly implement success/failure reporting
	return 1;
};

int UnmanagedOpenCL::UploadParameterisation()
{
	if(GotDevice)
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

		Structure->AtomicStructureParameterisation = clCreateBuffer(clState::context,CL_MEM_READ_ONLY,12*103*sizeof(float),0,&clState::status);
		clEnqueueWriteBuffer(clState::clq->cmdQueue,Structure->AtomicStructureParameterisation,CL_TRUE,0,12*103*sizeof(float),&fparams[0],0,NULL,NULL);
		fparams.clear();
	}
	return 0;
};

void UnmanagedOpenCL::InitialiseSimulation(int resolution)
{
	// Note, shouldnt pass any of the clstate should, should just change all accesses to the clState static version instead.
	TS = std::unique_ptr<TEMSimulation>(new TEMSimulation(temparams,stemparams));
	TS->Initialise(resolution,Structure);
};

// Calls different initialiser to make a probe wavefunction instead of plane wave
void UnmanagedOpenCL::InitialiseSTEMSimulation(int resolution)
{
	//TS = new TEMSimulation(clState::context,clState::clq,clState::cldev,temparams,stemparams);
	TS = std::unique_ptr<TEMSimulation>(new TEMSimulation(temparams,stemparams));;
	TS->InitialiseSTEM(resolution, Structure);
};

void UnmanagedOpenCL::MakeSTEMWaveFunction(int posx, int posy)
{
	TS->MakeSTEMWaveFunction(posx,posy);
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

void UnmanagedOpenCL::SetDevice(int index)
{
	// Check if got a device already
	if(GotDevice)
	{
		if(GotStruct)
			if(Structure->sorted)
			Structure->ClearStructure();
	}


	// Get new device
	clState::SetDevice(index);
	Structure->GotDevice = true;
	GotDevice=true;

	// reupload new structure. (and param).
	if(GotStruct)
	{
		Structure->ImportAtoms(Structure->filepath);
		UploadParameterisation();
		Structure->SortAtoms(false);
	}
}

UnmanagedOpenCL::UnmanagedOpenCL() 
{
	GotStruct = false;
	GotDevice = false;

	clState::Setup();
	//clState::SetDevice(1);

	temparams = new TEMParameters();
	stemparams = new STEMParameters();
};

UnmanagedOpenCL::~UnmanagedOpenCL() 
{

};
