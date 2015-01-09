#include "UnmanagedOpenCL.h"

 clContext UnmanagedOpenCL::ctx = OpenCL::MakeContext(OpenCL::GetDeviceList());

UnmanagedOpenCL::UnmanagedOpenCL()
{
	GotStruct = false;
	GotDevice = false;
	TEMParams = new TEMParameters();
	STEMParams = new STEMParameters();
};

UnmanagedOpenCL::~UnmanagedOpenCL()
{
	if(GotStruct)
		delete Structure;
};

void UnmanagedOpenCL::setCLdev(int index)
{
	// Check if got a device already
	if (GotDevice)
	{
		if (GotStruct)
			if (Structure->sorted)
				Structure->ClearStructure();
	}

	ctx = OpenCL::MakeTwoQueueContext(OpenCL::GetDeviceByIndex(OpenCL::GetDeviceList(),index));
	GotDevice = true;

	// reupload new structure. (and param).
	if (GotStruct)
	{
		Structure->GotDevice = true;
		uploadParameterisation();
		Structure->SortAtoms(false);
	}
};

int UnmanagedOpenCL::getCLdevCount()
{
	return OpenCL::GetDeviceList().size();
};

std::string UnmanagedOpenCL::getCLdevString(int i, bool getShort)
{
	auto ls = OpenCL::GetDeviceList();
	return OpenCL::GetDeviceByIndex(ls,i).GetDeviceName();
};

uint64_t UnmanagedOpenCL::getCLdevGlobalMemory()
{
	// Not implemented yet
	return 0;
};

size_t UnmanagedOpenCL::getCLMemoryUsed()
{
	return ctx.GetOccupiedMemorySize();
};

int UnmanagedOpenCL::importStructure(std::string filepath)
{
	if (GotStruct) {
		Structure->ClearStructure();
		delete Structure;
	}

	Structure = new MultisliceStructure();
	Structure->ImportAtoms(filepath);
	Structure->filepath = filepath;
	Structure->GotDevice = GotDevice;
	GotStruct = true;

	return 1;
};

int UnmanagedOpenCL::uploadParameterisation()
{
	if (GotDevice)
	{
		char inputparamsFilename[] = "fparams.dat";

		// Read in fparams data for calculating projected atomic potential.
		std::ifstream inparams;
		inparams.open(inputparamsFilename, std::ios::in);

		std::vector<float> fparams;
		AtomParameterisation buffer;

		if (!inparams)
		{
			throw "Can't find atomic parameterisation file";
		}

		while ((inparams >> buffer.a >> buffer.b >> buffer.c >> buffer.d >> buffer.e >> buffer.f >> buffer.g >> buffer.h >> buffer.i >> buffer.j >> buffer.k >> buffer.l))
		{
			fparams.push_back(buffer.a);
			fparams.push_back(buffer.b);
			fparams.push_back(buffer.c);
			fparams.push_back(buffer.d);
			fparams.push_back(buffer.e);
			fparams.push_back(buffer.f);
			fparams.push_back(buffer.g);
			fparams.push_back(buffer.h);
			fparams.push_back(buffer.i);
			fparams.push_back(buffer.j);
			fparams.push_back(buffer.k);
			fparams.push_back(buffer.l);
		}

		inparams.close();

		Structure->AtomicStructureParameterisation = ctx.CreateBuffer<float,Manual>(12*103);
		Structure->AtomicStructureParameterisation->Write(fparams);
		fparams.clear();
	}
	return 0;
};

void UnmanagedOpenCL::doMultisliceStep(int stepnumber, int steps, int waves)
{
	TS->doMultisliceStep(stepnumber, steps, waves);
};

void UnmanagedOpenCL::setCTEMParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture, float astig2mag, float astig2ang, float b2mag, float b2ang)
{
	TEMParams->defocus = df;
	TEMParams->astigmag = astigmag;
	TEMParams->astigang = astigang;
	TEMParams->kilovoltage = kilovoltage;
	TEMParams->spherical = spherical;
	TEMParams->beta = beta;
	TEMParams->delta = delta;
	TEMParams->aperturesizemrad = aperture;
	TEMParams->astig2mag = astig2mag;
	TEMParams->astig2ang = astig2ang;
	TEMParams->b2mag = b2mag;
	TEMParams->b2ang = b2ang;
};

void UnmanagedOpenCL::setSTEMParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture)
{
	STEMParams->defocus = df;
	STEMParams->astigmag = astigmag;
	STEMParams->astigang = astigang;
	STEMParams->kilovoltage = kilovoltage;
	STEMParams->spherical = spherical;
	STEMParams->beta = beta;
	STEMParams->delta = delta;
	STEMParams->aperturesizemrad = aperture;
};

void UnmanagedOpenCL::initialiseCTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints)
{
	// Note, shouldnt pass any of the clstate should, should just change all accesses to the clState static version instead.
	TS.reset(new TEMSimulation(TEMParams, STEMParams));
	TS->initialiseCTEMSimulation(resolution, Structure, startx, starty, endx, endy, Full3D, FD, dz, full3dints);
};

void UnmanagedOpenCL::initialiseSTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves)
{
	TS.reset(new TEMSimulation(TEMParams, STEMParams));
	TS->initialiseSTEMSimulation(resolution, Structure, startx, starty, endx, endy, Full3D,FD, dz, full3dints, waves);
};

void UnmanagedOpenCL::initialiseSTEMWaveFunction(float posx, float posy, int waves)
{
	TS->initialiseSTEMWaveFunction(posx, posy, waves);
};