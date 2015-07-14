#pragma once

#include "Initialisation.h"

//TODO: split into STEM specific stuff too
class CBEDSimulation : public virtual SimulationInitialisation
{
	//Probe specific OpenCL stuff
	clMemory<float, Manual>::Ptr clTDSDiff;

	clKernel MultiplyCL;
	clKernel MaskingKernel;
	clKernel TDSMaskingKernel;
	clKernel TDSMaskingAbsKernel;

	clKernel InitialiseProbeWavefunction;
	clKernel WFabsolute;

	clMemory<float, Manual>::Ptr clTDSMaskDiff;

	void initialiseProbeSimulation(int waves);
};