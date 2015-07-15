#pragma once

#include "MainSimulation.h"
#include "mtf.h" // contains all the NTF/DQE type stuff for dose images

class CTEMSimulation : public virtual MicroscopeSimulation
{
protected:
	clKernel InitPlaneWavefunction;
	clKernel ImagingKernel;

	// for phase image?
	float ewAbsMin;
	float ewAbsMax;
	float ewPhaseMin;
	float ewPhaseMax;
	float imageMin;
	float imageMax;

	CTEMSimulation(){};

public:

	void initialiseCTEMSimulation(std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints);

	// These just get the Absolute and phase?
	void getEWAbsoluteImage(float* data, int resolution);
	void getEWPhaseImage(float* data, int resolution);

	void simulateCTEM();
	void simulateCTEM(int detector, int binning, float doseperpix, float conversionfactor);

	void getCTEMImage(float* data, int resolution);

	float getImageMin() { return imageMin; }
	float getImageMax() { return imageMax; }
	float getEWAbsoluteMin() { return ewAbsMin; }
	float getEWAbsoluteMax() { return ewAbsMax; }
	float getEWPhaseMin() { return ewPhaseMin; }
	float getEWPhaseMax() { return ewPhaseMax; }

};