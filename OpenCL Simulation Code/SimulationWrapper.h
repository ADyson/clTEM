#pragma once
#include "STEMSimulation.h"
#include "CTEMSimulation.h"

namespace SimulationType
{
	// Unspecified first so containers will default to this value.
	enum SimTypes
	{
		CTEM,
		STEM,
		CBED
	};
};

// A bodgy wrapper class so everything can be kept in one place, but the methods are seperated into different class
// Makes use of diamond inheritance
class SimulationWrapper : public CTEMSimulation, public STEMSimulation
{
public:
	SimulationWrapper(SimulationType::SimTypes choice, std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves = 1)
	{
		if (choice == SimulationType::CTEM)
			initialiseCTEMSimulation(params, Structure, res, startx, starty, endx, endy, Full3D, FD, dz, full3dints);
		else if (choice == SimulationType::STEM)
			InitialiseSTEMSimulation(params, Structure, res, startx, starty, endx, endy, Full3D, FD, dz, full3dints, waves);
		else if (choice == SimulationType::CBED)
			initialiseProbeSimulation(params, Structure, res, startx, starty, endx, endy, Full3D, FD, dz, full3dints, waves);

		// should probably have an else to catch errors?
	}
};