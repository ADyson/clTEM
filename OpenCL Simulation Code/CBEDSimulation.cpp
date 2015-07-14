#include "CBEDSimulation.h"

void CBEDSimulation::initialiseProbeSimulation(int waves)
{
	clTDSDiff = UnmanagedOpenCL::ctx.CreateBuffer<cl_float, Manual>(resolution*resolution);

	// Initialise Wavefunctions and Create other buffers...
	for (int i = 1; i <= waves; i++)
	{
		clWaveFunction1.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		clWaveFunction2.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		clWaveFunction4.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));

		if (isFD)
		{
			clWaveFunction1Minus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
			clWaveFunction1Plus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		}
	}
	clWaveFunction3.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));

	clTDSMaskDiff = UnmanagedOpenCL::ctx.CreateBuffer<cl_float, Manual>(resolution*resolution);

	MultiplyCL = clKernel(UnmanagedOpenCL::ctx, multiplySource, 4, "clMultiply");
	MaskingKernel = clKernel(UnmanagedOpenCL::ctx, bandPassSource, 6, "clBandPass");
	TDSMaskingKernel = clKernel(UnmanagedOpenCL::ctx, floatbandPassSource, 8, "clFloatBandPass");
	TDSMaskingAbsKernel = clKernel(UnmanagedOpenCL::ctx, floatabsbandPassSource, 8, "clFloatAbsBandPass");

	InitialiseProbeWavefunction = clKernel(UnmanagedOpenCL::ctx, InitialiseSTEMWavefunctionSourceTest.c_str(), 24, "clInitialiseSTEMWavefunction");

	WFabsolute = clKernel(UnmanagedOpenCL::ctx, abssource2, 3, "clAbs");
}