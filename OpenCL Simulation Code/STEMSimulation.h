#pragma once

#include "CBEDSimulation.h"

class STEMSimulation : public CBEDSimulation
{
private:
	float FloatSumReduction(clMemory<float, Manual>::Ptr Array, clWorkGroup globalSizeSum, clWorkGroup localSizeSum, int nGroups, int totalSize);

	clKernel TDSMaskingAbsKernel;

protected:
	STEMSimulation(){}

public:
	void InitialiseSTEMSimulation(MicroscopeParameters* params, MultisliceStructure* Structure, int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);

	float getSTEMPixel(float inner, float outer, float xc, float yc, int wave);
};