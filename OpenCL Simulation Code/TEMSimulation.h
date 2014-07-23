#include "CommonStructs.h"
#include "MultisliceStructure.h"
#include "clFourier.h"
#include "clMemory.h"
#include "clState.h"
#include <memory>

class TEMSimulation
{
public:

	TEMSimulation(TEMParameters* temparams, STEMParameters* stemparams);
	~TEMSimulation();

	// Function to calculate sum of an opencl memory object
	float SumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	float SumReduction(Buffer &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	float FloatSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	float FloatSumReduction(Buffer &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);

	// Simulation steps
	void Initialise(int resolution, MultisliceStructure* Structure, bool Full3D);
	void InitialiseReSized(int resolution, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D);
	void InitialiseSTEM(int resolution, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, int waves = 1);
	void MakeSTEMWaveFunction(float posx, float posy);
	void MakeSTEMWaveFunction(float posx, float posy, int wave);

	void MultisliceStep(int stepno, int steps);
	void MultisliceStep(int stepno, int steps, int waves);
	float MeasureSTEMPixel(float inner, float outer);
	float MeasureSTEMPixel(float inner, float outer, int wave);
	void GetCTEMImage(float* data, int resolution);
	void GetCTEMImage(float* data, int resolution, float dose, int binning, int detector);
	void GetDiffImage(float* data, int resolution);
	void GetDiffImage(float* data, int resolution, int wave);
	void GetImDiffImage(float* data, int resolution);
	void GetImDiffImage(float* data, int resolution, int wave);
	void GetEWImage(float* data, int resolution);
	void GetEWImage(float* data, int resolution, int wave);
	void AddTDSDiffImage(float* data, int resolution);
	void AddTDS();
	void AddTDSDiffImage(float* data, int resolution, int wave);
	void AddTDS(int wave);
	void ClearTDS();
	void ClearTDS(int waves);
	void SimulateCTEM();
	void SimulateCTEM(int detector, int binning);

	TEMParameters* TEMParams;
	STEMParameters* STEMParams;
	MultisliceStructure* AtomicStructure;

	// OpenCL Kernels
	FourierKernel FourierTrans;
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
	
	cl_int status;

	// OpenCL memory objects
	Buffer clXFrequencies;
	Buffer clYFrequencies;
	std::vector<Buffer> clWaveFunction1;
	std::vector<Buffer> clWaveFunction2;
	std::vector<Buffer> clWaveFunction3;
	std::vector<Buffer> clWaveFunction4;
	Buffer clImageWaveFunction;
	std::vector<Buffer> clTDSDiff;
	Buffer clTDSMaskDiff;
	Buffer clPropagator;
	Buffer clPotential;

	std::vector<std::vector<float>> clTDSx;
	std::vector<std::vector<float>> clTDSk;

	// Simulation variables
	int resolution;
	float pixelscale;
	float wavelength;
	float bandwidthkmax;

	// Image contrast limits (technically ew atm)
	float imagemin;
	float imagemax;
	std::vector<float> ewmin;
	std::vector<float> ewmax;
	std::vector<float> diffmin;
	std::vector<float> diffmax;
	std::vector<float> tdsmin;
	std::vector<float> tdsmax;
};