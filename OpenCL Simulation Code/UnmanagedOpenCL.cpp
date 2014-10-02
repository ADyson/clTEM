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

		Structure->AtomicStructureParameterisation = Buffer( new clMemory(12*103*sizeof(float),CL_MEM_READ_ONLY));
		Structure->AtomicStructureParameterisation->Write(fparams);
		fparams.clear();
	}
	return 0;
};

void UnmanagedOpenCL::InitialiseSimulation(int resolution, bool Full3D)
{
	// Note, shouldnt pass any of the clstate should, should just change all accesses to the clState static version instead.
	TS = SimulationPtr(new TEMSimulation(temparams,stemparams));
	TS->Initialise(resolution,Structure,Full3D);
};

void UnmanagedOpenCL::InitialiseReSizedSimulation(int resolution,float startx, float starty ,float endx, float endy, bool Full3D)
{
	// Note, shouldnt pass any of the clstate should, should just change all accesses to the clState static version instead.
	TS = SimulationPtr(new TEMSimulation(temparams,stemparams));
	TS->InitialiseReSized(resolution,Structure,startx,starty,endx,endy,Full3D);
};

// Calls different initialiser to make a probe wavefunction instead of plane wave
void UnmanagedOpenCL::InitialiseSTEMSimulation(int resolution,float startx, float starty ,float endx, float endy, bool Full3D)
{
	TS = SimulationPtr(new TEMSimulation(temparams,stemparams));
	TS->InitialiseSTEM(resolution,Structure,startx,starty,endx,endy,Full3D);
};

void UnmanagedOpenCL::MakeSTEMWaveFunction(float posx, float posy)
{
	TS->MakeSTEMWaveFunction(posx,posy);
};

void UnmanagedOpenCL::MultisliceStep(int stepnumber, int steps)
{
	TS->MultisliceStep(stepnumber,steps);
};


void UnmanagedOpenCL::InitialiseSTEMSimulation(int resolution,float startx, float starty ,float endx, float endy, bool Full3D, int waves)
{
	TS = SimulationPtr(new TEMSimulation(temparams,stemparams));
	TS->InitialiseSTEM(resolution,Structure,startx,starty,endx,endy,Full3D,waves);
};

void UnmanagedOpenCL::MakeSTEMWaveFunction(float posx, float posy, int waves)
{
	TS->MakeSTEMWaveFunction(posx,posy, waves);
};

void UnmanagedOpenCL::MultisliceStep(int stepnumber, int steps, int waves)
{
	TS->MultisliceStep(stepnumber,steps, waves);
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
	GotDevice=true;

	// reupload new structure. (and param).
	if(GotStruct)
	{
		Structure->GotDevice = true;
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

size_t UnmanagedOpenCL::MemoryUsed()
{
	return clState::GetTotalSize();
}

int UnmanagedOpenCL::getCLdevCount()
{
	return clState::GetNumDevices();
}

std::string UnmanagedOpenCL::getCLdevString(int i, bool getShort)
{
	return clState::GetDeviceString(i, getShort);
}

uint64_t UnmanagedOpenCL::getCLdevGlobalMemory()
{
	return clState::GetDeviceGlobalMemory();
}

UnmanagedOpenCL::~UnmanagedOpenCL() 
{
};
