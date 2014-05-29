#include "CommonStructs.h"
#include "MultisliceStructure.h"
#include "clFourier.h"
#include "clState.h"
#include <memory>

class TEMSimulation
{
public:
	TEMParameters* TEMParams;
	STEMParameters* STEMParams;
	MultisliceStructure* AtomicStructure;

	std::unique_ptr<clFourier> FourierTrans;

	Kernel BinnedAtomicPotential;
	Kernel GeneratePropagator;
	Kernel ComplexMultiply;
	Kernel BandLimit;
	Kernel fftShift;
	Kernel ImagingKernel;
	Kernel InitialiseSTEMWavefunction;
	Kernel WFabsolute;
	Kernel MultiplyCL;
	Kernel MaskingKernel;
	Kernel TDSMaskingKernel;

	//cl_context context;
	//clQueue* clq;
	//clDevice* cldev;
	cl_int status;

	Buffer clXFrequencies;
	Buffer clYFrequencies;

	Buffer clWaveFunction1;
	Buffer clWaveFunction2;
	Buffer clWaveFunction3;
	Buffer clWaveFunction4;

	cl_mem clImageWaveFunction;
	cl_mem clTDSDiff;
	cl_mem clTDSMaskDiff;

	cl_mem clPropagator;
	cl_mem clPotential;

	std::vector<float> clTDSx;
	std::vector<float> clTDSk;


	int resolution;
	float pixelscale;
	float wavelength;
	float bandwidthkmax;

	TEMSimulation(TEMParameters* temparams, STEMParameters* stemparams);
	~TEMSimulation();

	void Initialise(int resolution, MultisliceStructure* Structure);
	void InitialiseSTEM(int resolution, MultisliceStructure* Structure);
	void MakeSTEMWaveFunction(int posx, int posy);

	void MultisliceStep(int stepno, int steps);

	float MeasureSTEMPixel(float inner, float outer);
	float SumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	float FloatSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	void GetCTEMImage(float* data, int resolution);
	// Diff of EW
	void GetDiffImage(float* data, int resolution);
	// Diff of Im
	void GetImDiffImage(float* data, int resolution);
	void GetEWImage(float* data, int resolution);

	void AddTDSDiffImage(float* data, int resolution);

	void AddTDS();
	void ClearTDS();
	void SimulateCTEM();

	// Image contrast limits (technically ew atm)
	float imagemin;
	float imagemax;
	float ewmin;
	float ewmax;
	float diffmin;
	float diffmax;
	float tdsmin;
	float tdsmax;
};