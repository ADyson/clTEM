#pragma once

#include "MainSimulation.h"
#include "mtf.h" // contains all the NTF/DQE type stuff for dose images

class CTEMSimulation : public virtual MicroscopeSimulation
{
private:
	void initialisePlaneWavefunction();

protected:
	clKernel InitPlaneWavefunction;
	clKernel ImagingKernel;

	// for phase image?
	std::vector<float> ewmin;
	std::vector<float> ewmax;
	std::vector<float> ewmin2;
	std::vector<float> ewmax2;
	float imagemin;
	float imagemax;

	CTEMSimulation(){};

public:

	void initialiseCTEMSimulation(MicroscopeParameters* params, MultisliceStructure* Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints);

	// These just get the Absolute and phase?
	void getEWImage(float* data, int resolution, int wave);
	void getEWImage2(float* data, int resolution, int wave);

	void simulateCTEM();
	void simulateCTEM(int detector, int binning, float doseperpix, float conversionfactor);

	void getCTEMImage(float* data, int resolution);
};