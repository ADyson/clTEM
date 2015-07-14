#pragma once

#include "clWrapper.h"
#include "CommonStructs.h"
#include "MultisliceStructure.h"
#include "UnmanagedOpenCL.h"

//TODO: move all these to .cl files?
#include "clKernelCodes2.h"

class SimulationInitialisation
{
protected:

	//pointer to struct holding the needed parameters
	std::unique_ptr<MicroscopeParameters> mParams;

	//Pointer to container of the structure
	std::unique_ptr<MultisliceStructure> AtomicStructure;

	//Is finite difference being used
	bool isFD;
	//Simulation resolution
	int resolution;
	//Simulation pixel scale
	float pixelscale;
	//Wavelength for the voltage used
	//float wavelength // Can be retrieved from params? (quickly?)
	//upper value to mask above in reciprocal space
	//float bandwidthkmax;

	// OpenCL

	std::vector<clMemory<cl_float2, Manual>::Ptr> clWaveFunction1;
	std::vector<clMemory<cl_float2, Manual>::Ptr> clWaveFunction2;
	std::vector<clMemory<cl_float2, Manual>::Ptr> clWaveFunction3;
	std::vector<clMemory<cl_float2, Manual>::Ptr> clWaveFunction4;
	std::vector<clMemory<cl_float2, Manual>::Ptr> clWaveFunction1Minus; //for finite difference?
	std::vector<clMemory<cl_float2, Manual>::Ptr> clWaveFunction1Plus; //for finite difference?

	clMemory<float, Manual>::Ptr clXFrequencies;
	clMemory<float, Manual>::Ptr clYFrequencies;
	clMemory<cl_float2, Manual>::Ptr clPropagator;
	clMemory<cl_float2, Manual>::Ptr clPotential;
	clMemory<cl_float2, Manual>::Ptr clImageWaveFunction;

	// kernels
	clFourier FourierTrans;
	clKernel SumReduction;
	clKernel BandLimit;
	clKernel fftShift;
	clKernel BinnedAtomicPotential;
	clKernel GeneratePropagator;
	clKernel ComplexMultiply;





	// Unknown

	// store max/min values of images?
	std::vector<float> ewmin;
	std::vector<float> ewmax;
	std::vector<float> diffmin;
	std::vector<float> diffmax;

	std::vector<float> clTDSk;

	// Finite difference variables
	// --Seem to get set no matter what?
	float FDsigma;
	float FDdz; // slice thickness?
	int NumberOfFDSlices; // number of slices?

public:
	SimulationInitialisation() : FourierTrans(UnmanagedOpenCL::ctx, 1024, 1024){}
	void InitialiseSimulation(MicroscopeParameters* params, int res, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);
};