#include <CL\OpenCL.h>
#include "MultisliceStructure.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include "TEMSimulation.h"
#include "CommonStructs.h"
#include "clWrapper.h"
#include <memory>

#pragma unmanaged
#pragma once

typedef std::unique_ptr<TEMSimulation> SimulationPtr;


struct AtomParameterisation
{
	float a, b, c, d, e, f, g, h, i, j, k, l;
};


class UnmanagedOpenCL
{
public:
	UnmanagedOpenCL();
	~UnmanagedOpenCL();

	static clContext ctx;

	void setCLdev(int i); //SetDevice
	int getCLdevCount();
	std::string getCLdevString(int i, bool getShort);
	uint64_t getCLdevGlobalMemory();
	size_t getCLMemoryUsed(); //MemoryUsed

	int importStructure(std::string filepath); //SetupStructure
	int uploadParameterisation();

	void doMultisliceStep(int stepno, int steps, int waves);

	void setCTEMParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture, float astig2mag, float astig2ang, float b2mag, float b2ang);
	void setSTEMParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture);

	void initialiseCTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints);
	void initialiseSTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD,float dz, int full3dints, int waves);

	void initialiseSTEMWaveFunction(float posx, float posy, int wave);

	bool GotStruct;
	bool GotDevice;

	MultisliceStructure* Structure;
	SimulationPtr TS;

	TEMParameters* TEMParams;
	STEMParameters* STEMParams;

};
