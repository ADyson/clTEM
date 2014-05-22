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
	clKernel* BandLimit;
	clKernel* fftShift;
	clKernel* ImagingKernel;

	clKernel* InitialiseSTEMWavefunction;
	clKernel* WFabsolute;
	clKernel* MultiplyCL;
	clKernel* MaskingKernel;

	cl_context context;
	clQueue* clq;
	clDevice* cldev;
	cl_int status;

	cl_mem clXFrequencies;
	cl_mem clYFrequencies;

	cl_mem clWaveFunction1;
	cl_mem clWaveFunction2;
	cl_mem clWaveFunction3;
	cl_mem clWaveFunction4;
	cl_mem clImageWaveFunction;

	cl_mem clPropagator;
	cl_mem clPotential;

	std::vector<float> clTDSx;
	std::vector<float> clTDSk;


	int resolution;
	float pixelscale;
	float wavelength;
	float bandwidthkmax;

	TEMSimulation(cl_context &context, clQueue* clq, clDevice* cldev, TEMParameters* temparams, STEMParameters* stemparams);

	void Initialise(int resolution, MultisliceStructure* Structure);
	void InitialiseSTEM(int resolution, MultisliceStructure* Structure);
	void MakeSTEMWaveFunction(int posx, int posy);

	void MultisliceStep(int stepno, int steps);

	float MeasureSTEMPixel();
	float SumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);

	void GetCTEMImage(float* data, int resolution);
	// Diff of EW
	void GetDiffImage(float* data, int resolution);
	// Diff of Im
	void GetImDiffImage(float* data, int resolution);
	void GetEWImage(float* data, int resolution);

	void AddTDSDiffImage(float* data, int resolution);
	void AddTDS();
	void SimulateCTEM();

	// Image contrast limits (technically ew atm)
	float imagemin;
	float imagemax;
	float ewmin;
	float ewmax;
	float diffmin;
	float diffmax;

};