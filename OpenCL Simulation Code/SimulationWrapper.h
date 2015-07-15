#pragma once
#include "STEMSimulation.h"
#include "CTEMSimulation.h"

// A bodgy wrapper class so everything can be kept in one place
class SimulationWrapper : CTEMSimulation, STEMSimulation
{
	SimulationWrapper(MicroscopeParameters* params, int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves = 1)
	{
		waves = 2;
	}
};