#include "CommonStructs.h"
#include "MultisliceStructure.h"
#include "clFourier.h"

class TEMSimulation
{
public:
	TEMParameters* TEMParams;
	STEMParameters* STEMParams;
	MultisliceStructure* AtomicStructure;
	clFourier* FourierTrans;
	clKernel* BinnedAtomicPotential;
	clKernel* GeneratePropagator;
	clKernel* ComplexMultiply;

	cl_context context;
	clQueue* clq;
	clDevice* cldev;
	cl_int status;

	cl_mem clXFrequencies;
	cl_mem clYFrequencies;

	cl_mem clWaveFunction1;
	cl_mem clWaveFunction2;
	cl_mem clWaveFunction3;

	cl_mem clPropagator;
	cl_mem clPotential;


	int resolution;
	float pixelscale;
	float wavelength;
	float bandwidthkmax;

	TEMSimulation(cl_context &context, clQueue* clq, clDevice* cldev, TEMParameters* temparams, STEMParameters* stemparams);

	void Initialise(int resolution, MultisliceStructure* Structure);
	void InitialiseSTEM(int resolution, MultisliceStructure* Structure);


	void MultisliceStep(int stepno, int steps);

	void GetCTEMImage(float* data, int resolution);

};