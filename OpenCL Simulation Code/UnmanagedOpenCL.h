#pragma once

#include <CL\OpenCL.h>
#include "MultisliceStructure.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include "SimulationWrapper.h"
#include "CommonStructs.h"
#include "clWrapper.h"
#include <memory>

#pragma unmanaged

typedef std::unique_ptr<SimulationWrapper> SimulationPtr;


struct AtomParameterisation
{
	float a, b, c, d, e, f, g, h, i, j, k, l;
};


class UnmanagedOpenCL
{
public:
	UnmanagedOpenCL();
	~UnmanagedOpenCL();

	//static clContext ctx;

	void setCLdev(int i); //SetDevice
	int getCLdevCount();
	std::string getCLdevString(int i, bool getShort);
	size_t getCLMemoryUsed(); //MemoryUsed

	int importStructure(std::string filepath); //SetupStructure
	int uploadParameterisation();

	void doMultisliceStep(int stepno, int steps, int waves);

	void setMicroscopeParams(
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
		float C56Mag, float C56Ang,
		float Beta = 1.0f,
		float Delta = 1.0f
		);

	void initialiseCTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints);
	void initialiseSTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);
	void initialiseCBEDSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);

	void initialiseSTEMWaveFunction(float posx, float posy, int wave);

	bool GotStruct;
	bool GotDevice;

	
	std::shared_ptr<MultisliceStructure> Structure;
	SimulationPtr TS;

	std::shared_ptr<MicroscopeParameters> mParams;

};
