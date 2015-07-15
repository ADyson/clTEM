#include "CBEDSimulation.h"

void CBEDSimulation::initialiseProbeSimulation(std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves = 1)
{
	isFD = FD;
	clTDSDiff = OCL::ctx.CreateBuffer<cl_float, Manual>(resolution*resolution);

	// Initialise Wavefunctions and Create other buffers...
	for (int i = 1; i <= waves; i++)
	{
		clWaveFunction1.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		clWaveFunction2.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		clWaveFunction4.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));

		if (isFD)
		{
			clWaveFunction1Minus.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
			clWaveFunction1Plus.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		}
	}
	clWaveFunction3.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));

	clTDSMaskDiff = OCL::ctx.CreateBuffer<cl_float, Manual>(resolution*resolution);

	InitProbeWavefunction = clKernel(OCL::ctx, InitialiseSTEMWavefunctionSourceTest.c_str(), 24, "clInitProbeWavefunction");

	InitialiseSimulation(params, Structure, res, startx, starty, endx, endy, Full3D, FD, dz, full3dints, waves);
}

void CBEDSimulation::initialiseProbeWaveFunction(float posx, float posy, int wave)
{
	clWorkGroup WorkSize(resolution, resolution, 1);

	// Fix inverted images
	posx = resolution - 1 - posx;
	posy = resolution - 1 - posy;

	InitProbeWavefunction.SetArg(0, clWaveFunction2[wave - 1]);
	InitProbeWavefunction.SetArg(1, resolution);
	InitProbeWavefunction.SetArg(2, resolution);
	InitProbeWavefunction.SetArg(3, clXFrequencies);
	InitProbeWavefunction.SetArg(4, clYFrequencies);
	InitProbeWavefunction.SetArg(5, posx);
	InitProbeWavefunction.SetArg(6, posy);
	InitProbeWavefunction.SetArg(7, pixelscale);
	InitProbeWavefunction.SetArg(8, wavelength);
	InitProbeWavefunction.SetArg(9, mParams->C10);
	InitProbeWavefunction.SetArg(10, mParams->C12);
	InitProbeWavefunction.SetArg(11, mParams->C21);
	InitProbeWavefunction.SetArg(12, mParams->C23);
	InitProbeWavefunction.SetArg(13, mParams->C30);
	InitProbeWavefunction.SetArg(14, mParams->C32);
	InitProbeWavefunction.SetArg(15, mParams->C34);
	InitProbeWavefunction.SetArg(16, mParams->C41);
	InitProbeWavefunction.SetArg(17, mParams->C43);
	InitProbeWavefunction.SetArg(18, mParams->C45);
	InitProbeWavefunction.SetArg(19, mParams->C50);
	InitProbeWavefunction.SetArg(20, mParams->C52);
	InitProbeWavefunction.SetArg(21, mParams->C54);
	InitProbeWavefunction.SetArg(22, mParams->C56);
	InitProbeWavefunction.SetArg(23, mParams->Aperture);

	InitProbeWavefunction(WorkSize);

	// IFFT
	FourierTrans(clWaveFunction2[wave - 1], clWaveFunction1[wave - 1], Direction::Inverse);

	if (isFD)
	{
		// Copy into both initialwavefunctions
		clEnqueueCopyBuffer(OCL::ctx.GetIOQueue(), clWaveFunction1[wave - 1]->GetBuffer(), clWaveFunction1Minus[wave - 1]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, 0, 0);
	}
};