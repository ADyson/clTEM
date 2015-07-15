#include "STEMSimulation.h"

void STEMSimulation::InitialiseSTEMSimulation(MicroscopeParameters* params, MultisliceStructure* Structure, int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves = 1)
{
	initialiseProbeSimulation(params, Structure, resolution, startx, starty, endx, endy, Full3D, FD, dz, full3dints, waves);
	TDSMaskingAbsKernel = clKernel(UnmanagedOpenCL::ctx, floatabsbandPassSource, 8, "clFloatAbsBandPass");
}

float STEMSimulation::getSTEMPixel(float inner, float outer, float xc, float yc, int wave)
{
	clWorkGroup WorkSize(resolution, resolution, 1);

	fftShift.SetArg(0, clWaveFunction2[wave - 1], ArgumentType::Input);
	fftShift(WorkSize);

	float pxFreq = (resolution * pixelscale);

	float innerFreq = inner / (1000 * wavelength);
	float innerPx = innerFreq*pxFreq;

	float outerFreq = outer / (1000 * wavelength);
	float outerPx = outerFreq*pxFreq;

	float xcFreq = xc / (1000 * wavelength);
	float xcPx = xcFreq*pxFreq;

	float ycFreq = yc / (1000 * wavelength);
	float ycPx = ycFreq*pxFreq;

	TDSMaskingAbsKernel.SetArg(0, clTDSMaskDiff, ArgumentType::Output);
	TDSMaskingAbsKernel.SetArg(1, clWaveFunction3[0], ArgumentType::Input);
	TDSMaskingAbsKernel.SetArg(2, resolution);
	TDSMaskingAbsKernel.SetArg(3, resolution);
	TDSMaskingAbsKernel.SetArg(4, innerPx);
	TDSMaskingAbsKernel.SetArg(5, outerPx);
	TDSMaskingAbsKernel.SetArg(6, xcPx);
	TDSMaskingAbsKernel.SetArg(7, ycPx);

	TDSMaskingAbsKernel(WorkSize);

	int totalSize = resolution*resolution;
	int nGroups = totalSize / 256;

	clWorkGroup globalSizeSum(totalSize, 1, 1);
	clWorkGroup localSizeSum(256, 1, 1);

	return FloatSumReduction(clTDSMaskDiff, globalSizeSum, localSizeSum, nGroups, totalSize);
}

float STEMSimulation::FloatSumReduction(clMemory<float, Manual>::Ptr Array, clWorkGroup globalSizeSum, clWorkGroup localSizeSum, int nGroups, int totalSize)
{
	clMemory<float, Manual>::Ptr outArray = UnmanagedOpenCL::ctx.CreateBuffer<float, Manual>(nGroups);
	SumReduction.SetArg(0, Array, ArgumentType::Input);

	// Only really need to do these 3 once...
	SumReduction.SetArg(1, outArray, ArgumentType::Output);
	SumReduction.SetArg(2, totalSize);
	SumReduction.SetLocalMemoryArg<float>(3, 256);
	SumReduction(globalSizeSum, localSizeSum);

	// Now copy back 
	std::vector< float> sums = outArray->CreateLocalCopy();

	// Find out which numbers to read back
	float sum = 0;
	for (int i = 0; i < nGroups; i++)
	{
		sum += sums[i];
	}
	return sum;
}