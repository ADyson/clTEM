#pragma once

#include "MainSimulation.h"

//TODO: split into STEM specific stuff too
class CBEDSimulation : public virtual MicroscopeSimulation
{
protected:
	//Probe specific OpenCL stuff
	clMemory<float, Manual>::Ptr clTDSDiff;

	clKernel InitProbeWavefunction;

	clMemory<float, Manual>::Ptr clTDSMaskDiff;

	CBEDSimulation(){};
public:

	void initialiseProbeSimulation(std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);

	void initialiseProbeWaveFunction(float posx, float posy, int wave);

};