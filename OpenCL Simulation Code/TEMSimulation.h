#include "CommonStructs.h"
#include "MultisliceStructure.h"
#include "clFourier.h"
#include "clMemory.h"
#include "clState.h"
#include <memory>

class TEMSimulation
{
public:

	/// <summary>
	/// Constructor, sets the TEM and STEM parameters
	/// </summary>
	/// <param name ="temparams"> Pointer to TEM parameters used in the simulation.</param>
	/// <param name ="temparams"> Pointer to STEM parameters used in the simulation.</param>
	TEMSimulation(TEMParameters* temparams, STEMParameters* stemparams);

	/// <summary>
	/// Destructor, does nothing.
	/// </summary>
	~TEMSimulation();

	// used for TEM, needs to be renamed
	//InitialiseReSized
	void initialiseCTEMSimulation(int res, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints);
	void initialiseSTEMSimulation(int resolution, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves = 1);

	void initialiseSTEMWaveFunction(float posx, float posy, int wave);

	void doMultisliceStep(int stepno, int steps, int waves);
	void doMultisliceStepFD(int stepno, int waves);

	float getSTEMPixel(float inner, float outer, float xc, float yc, int wave);

	void getCTEMImage(float* data, int resolution);
	void getCTEMImage(float* data, int resolution, float dose, int binning, int detector);

	void simulateCTEM();
	void simulateCTEM(int detector, int binning);

	void getDiffImage(float* data, int resolution, int wave);
	void getSTEMDiff(int wave);
	void getEWImage(float* data, int resolution, int wave);
	void getEWImage2(float* data, int resolution, int wave);

	//void AddTDSDiffImage(float* data, int resolution); same as GetDiffImage
	void addTDS(int wave);
	void clearTDS(int wave);

	float FloatSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);

	TEMParameters* TEMParams;
	STEMParameters* STEMParams;
	MultisliceStructure* AtomicStructure;

	int resolution;
	float pixelscale;
	float wavelength;
	float bandwidthkmax;

	std::vector<std::vector<float>> clTDSx;
	std::vector<std::vector<float>> clTDSk;
	float imagemin;
	float imagemax;

	std::vector<float> ewmin;
	std::vector<float> ewmax;
	std::vector<float> ewmin2;
	std::vector<float> ewmax2;
	std::vector<float> diffmin;
	std::vector<float> diffmax;
	std::vector<float> tdsmin;
	std::vector<float> tdsmax;

	// Variable for finite difference
	int NumberOfFDSlices;
	float FDdz;
	bool FDMode;
	float FDsigma;

	// openCL stuff
	cl_int status;
	Buffer clImageWaveFunction;
	Buffer clXFrequencies;
	Buffer clYFrequencies;
	Buffer clTDSMaskDiff;
	Buffer clPropagator;
	Buffer clPotential;
	std::vector<Buffer> clWaveFunction1;
	std::vector<Buffer> clWaveFunction2;
	std::vector<Buffer> clWaveFunction3;
	std::vector<Buffer> clWaveFunction4;
	std::vector<Buffer> clWaveFunction1Minus;
	std::vector<Buffer> clWaveFunction1Plus;
	std::vector<Buffer> clTDSDiff;

	//Kernels
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
	Kernel SumReduction;
	// FD Only
	Kernel GradKernel;
	Kernel FiniteDifference;

//private:
	// no idea if these are used
	//float ComplSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	//float ComplSumReduction(Buffer &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	//float FloatSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
	//float FloatSumReduction(Buffer &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize);
//
};