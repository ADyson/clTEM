#pragma once

#include "clWrapper.h"
#include "CommonStructs.h"
#include "MultisliceStructure.h"

//TODO: move all these to .cl files?
#include "clKernelCodes2.h"


// Class containing mthods for all/multiple simulation types.
// The bulk of the code is in the initialisation, simulation specific initialisation is performed in their respective classes
// (which should inherit from this)
class MicroscopeSimulation
{
protected:

	// these pointers can be made into smart objects later
	//pointer to struct holding the needed parameters
	std::shared_ptr<MicroscopeParameters> mParams;

	//Pointer to container of the structure
	std::shared_ptr<MultisliceStructure> AtomicStructure;

	//Is finite difference being used
	bool isFD;
	//Simulation resolution
	int resolution;
	//Simulation pixel scale
	float pixelscale;
	//Wavelength for the voltage used
	float wavelength; // Can be retrieved from params? (quickly?)
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
	// FD Only
	clKernel GradKernel;
	clKernel FiniteDifference;


	// Unknown

	// store max/min values of diffraction patterns.
	std::vector<float> diffMin;
	std::vector<float> diffMax;

	std::vector<float> clTDSk;

	// Finite difference variables
	// --Seem to get set no matter what?
	float FDsigma;
	float FDdz; // slice thickness?
	int NumberOfFDSlices; // number of slices?

	MicroscopeSimulation() : FourierTrans(OCL::ctx, 1024, 1024), mParams(new MicroscopeParameters)
	{
		//InitialiseSimulation(params, res, Structure, startx, starty, endx, endy, Full3D, FD, dz, full3dints, waves);
	}

public:

	void InitialiseSimulation(std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);

	void updateParams(std::shared_ptr<MicroscopeParameters> params)
	{
		mParams = params;
	}

	void doMultisliceStep(int stepno, int steps, int waves);
	void doMultisliceStepFD(int stepno, int waves);

	void getDiffImage(float* data, int resolution, int wave);

	int getFDSlices() { return NumberOfFDSlices; }
	float getDiffractionMin(int i){ return diffMin[i]; }
	float getDiffractionMax(int i){ return diffMax[i]; }
};