#include "UnmanagedOpenCL.h"

#include <complex>

const float d2r = 3.141592653589793238462643383279502884 / 180;

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

void UnmanagedOpenCL::setCTEMParams(
	float Voltage,
	float Beta,
	float Delta,
	float Aperture,
	float C10,
	float C12Mag, float C12Ang,
	float C21Mag, float C21Ang,
	float C23Mag, float C23Ang,
	float C30,
	float C32Mag, float C32Ang,
	float C34Mag, float C34Ang,
	float C41Mag, float C41Ang,
	float C43Mag, float C43Ang,
	float C45Mag, float C45Ang,
	float C50,
	float C52Mag, float C52Ang,
	float C54Mag, float C54Ang,
	float C56Mag, float C56Ang
	)
{
	TEMParams->Voltage = Voltage;

	TEMParams->C10 = C10;
	TEMParams->C12 = std::complex<float>(C12Mag * std::cos(d2r * C12Ang), C12Mag * std::sin(d2r * C12Ang));
	TEMParams->C21 = std::complex<float>(C21Mag * std::cos(d2r * C21Ang), C21Mag * std::sin(d2r * C21Ang));
	TEMParams->C23 = std::complex<float>(C23Mag * std::cos(d2r * C23Ang), C23Mag * std::sin(d2r * C23Ang));
	TEMParams->C30 = C30;
	TEMParams->C32 = std::complex<float>(C32Mag * std::cos(d2r * C32Ang), C32Mag * std::sin(d2r * C32Ang));
	TEMParams->C34 = std::complex<float>(C34Mag * std::cos(d2r * C34Ang), C34Mag * std::sin(d2r * C34Ang));
	TEMParams->C41 = std::complex<float>(C41Mag * std::cos(d2r * C41Ang), C41Mag * std::sin(d2r * C41Ang));
	TEMParams->C43 = std::complex<float>(C43Mag * std::cos(d2r * C43Ang), C43Mag * std::sin(d2r * C43Ang));
	TEMParams->C45 = std::complex<float>(C45Mag * std::cos(d2r * C45Ang), C45Mag * std::sin(d2r * C45Ang));
	TEMParams->C50 = C50;
	TEMParams->C52 = std::complex<float>(C52Mag * std::cos(d2r * C52Ang), C52Mag * std::sin(d2r * C52Ang));
	TEMParams->C54 = std::complex<float>(C54Mag * std::cos(d2r * C54Ang), C54Mag * std::sin(d2r * C54Ang));
	TEMParams->C56 = std::complex<float>(C56Mag * std::cos(d2r * C56Ang), C56Mag * std::sin(d2r * C56Ang));

	TEMParams->Beta = Beta / 1000;
	TEMParams->Delta = Delta / 10;
	TEMParams->Aperture = Aperture;
};

void UnmanagedOpenCL::setSTEMParams(
	float Voltage,
	float Aperture,
	float C10,
	float C12Mag, float C12Ang,
	float C21Mag, float C21Ang,
	float C23Mag, float C23Ang,
	float C30,
	float C32Mag, float C32Ang,
	float C34Mag, float C34Ang,
	float C41Mag, float C41Ang,
	float C43Mag, float C43Ang,
	float C45Mag, float C45Ang,
	float C50,
	float C52Mag, float C52Ang,
	float C54Mag, float C54Ang,
	float C56Mag, float C56Ang
	)
{
	STEMParams->Voltage = Voltage;

	STEMParams->C10 = C10;
	STEMParams->C12 = std::complex<float>(C12Mag * std::cos(d2r * C12Ang), C12Mag * std::sin(d2r * C12Ang));
	STEMParams->C21 = std::complex<float>(C21Mag * std::cos(d2r * C21Ang), C21Mag * std::sin(d2r * C21Ang));
	STEMParams->C23 = std::complex<float>(C23Mag * std::cos(d2r * C23Ang), C23Mag * std::sin(d2r * C23Ang));
	STEMParams->C30 = C30;
	STEMParams->C32 = std::complex<float>(C32Mag * std::cos(d2r * C32Ang), C32Mag * std::sin(d2r * C32Ang));
	STEMParams->C34 = std::complex<float>(C34Mag * std::cos(d2r * C34Ang), C34Mag * std::sin(d2r * C34Ang));
	STEMParams->C41 = std::complex<float>(C41Mag * std::cos(d2r * C41Ang), C41Mag * std::sin(d2r * C41Ang));
	STEMParams->C43 = std::complex<float>(C43Mag * std::cos(d2r * C43Ang), C43Mag * std::sin(d2r * C43Ang));
	STEMParams->C45 = std::complex<float>(C45Mag * std::cos(d2r * C45Ang), C45Mag * std::sin(d2r * C45Ang));
	STEMParams->C50 = C50;
	STEMParams->C52 = std::complex<float>(C52Mag * std::cos(d2r * C52Ang), C52Mag * std::sin(d2r * C52Ang));
	STEMParams->C54 = std::complex<float>(C54Mag * std::cos(d2r * C54Ang), C54Mag * std::sin(d2r * C54Ang));
	STEMParams->C56 = std::complex<float>(C56Mag * std::cos(d2r * C56Ang), C56Mag * std::sin(d2r * C56Ang));

	STEMParams->Aperture = Aperture;
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