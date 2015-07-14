#include "CTEMSimulation.h"

void CTEMSimulation::initialiseCTEMSimulation()
{
	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	clWaveFunction2.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	clWaveFunction3.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	clWaveFunction4.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));

	// might not need to be vectors, only if stem needs them
	if (isFD)
	{
		clWaveFunction1Minus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		clWaveFunction1Plus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	}

	InitialisePlaneWavefunction = clKernel(UnmanagedOpenCL::ctx, InitialiseWavefunctionSource.c_str(), 4, "clInitialiseWavefunction");

	float InitialValue = 1.0f;
	if (isFD) // in Adam's code, this get's reset later anyway?
		InitialisePlaneWavefunction.SetArg(0, clWaveFunction1Minus[0], ArgumentType::Output);
	else
		InitialisePlaneWavefunction.SetArg(0, clWaveFunction1[0], ArgumentType::Output);
	InitialisePlaneWavefunction.SetArg(1, resolution);
	InitialisePlaneWavefunction.SetArg(2, resolution);
	InitialisePlaneWavefunction.SetArg(3, InitialValue);

	ImagingKernel = clKernel(UnmanagedOpenCL::ctx, imagingKernelSource.c_str(), 24, "clImagingKernel");
}

void CTEMSimulation::initialiseWavefunction()
{
	ewmin2.resize(1); // CTEM specific? (so only need 1)
	ewmax2.resize(1);

	// repeated but should be pretty lightweight
	clWorkGroup WorkSize(resolution, resolution, 1);

	InitialisePlaneWavefunction(WorkSize);
	UnmanagedOpenCL::ctx.WaitForQueueFinish();
}