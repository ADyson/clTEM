#pragma once

#include "Initialisation.h"

class CTEMSimulation : public virtual SimulationInitialisation
{
	clKernel InitialisePlaneWavefunction;
	clKernel ImagingKernel;

	// for phase image?
	std::vector<float> ewmin2;
	std::vector<float> ewmax2;

	void initialiseCTEMSimulation();
	void initialiseWavefunction();
};