#include "TEMSimulation.h"
#include "clKernelCodes2.h"
#include <minmax.h>
#include "mtf.h"


TEMSimulation::TEMSimulation(TEMParameters* temparams, STEMParameters* stemparams)
{
	TEMParams = temparams;
	STEMParams = stemparams;
};

TEMSimulation::~TEMSimulation()
{
};

void TEMSimulation::initialiseCTEMSimulation(int res, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints)
{
	// Maintain whether we initialised for FD for later multislicestep calls...
	FDMode = FD;

	resolution = res;
	AtomicStructure = Structure;
	AtomicStructure->dz = dz;

	// Get size of input structure
	float RealSizeX = endx - startx;
	float RealSizeY = endy - starty;
	pixelscale = max(RealSizeX, RealSizeY) / (resolution);

	// Work out size of each binned block of atoms
	float BlockScaleX = (AtomicStructure->MaximumX - AtomicStructure->MinimumX) / AtomicStructure->xBlocks;
	float BlockScaleY = (AtomicStructure->MaximumY - AtomicStructure->MinimumY) / AtomicStructure->yBlocks;

	// Work out area that is to be simulated
	float SimSizeX = pixelscale * resolution;
	float SimSizeY = SimSizeX;

	float	Pi = 3.1415926f;
	float	V = TEMParams->kilovoltage;
	float	a0 = 52.9177e-012f;
	float	a0a = a0*1e+010f;
	float	echarge = 1.6e-019f;
	wavelength = 6.63e-034f*3e+008f / sqrt((echarge*V * 1000 * (2 * 9.11e-031f*9e+016f + echarge*V * 1000)))*1e+010f;
	float	sigma = 2 * Pi * ((511.0f + V) / (2.0f*511.0f + V)) / (V * wavelength);
	float	sigma2 = (2 * Pi / (wavelength * V * 1000)) * ((9.11e-031f*9e+016f + echarge*V * 1000) / (2 * 9.11e-031f*9e+016f + echarge*V * 1000));
	float	fix = 300.8242834f / (4 * Pi*Pi*a0a*echarge);
	float	V2 = V * 1000;

	FDsigma = sigma2;

	// Now we can set up frequencies and fourier transforms.

	int imidx = floor(resolution / 2 + 0.5);
	int imidy = floor(resolution / 2 + 0.5);

	std::vector<float> k0x;
	std::vector<float> k0y;

	float temp;

	for (int i = 1; i <= resolution; i++)
	{
		if ((i - 1) > imidx)
			temp = ((i - 1) - resolution) / SimSizeX;
		else temp = (i - 1) / SimSizeX;
		k0x.push_back(temp);
	}

	for (int i = 1; i <= resolution; i++)
	{
		if ((i - 1) > imidy)
			temp = ((i - 1) - resolution) / SimSizeY;
		else temp = (i - 1) / SimSizeY;
		k0y.push_back(temp);
	}

	// Find maximum frequency for bandwidth limiting rule....

	bandwidthkmax = 0;

	float	kmaxx = pow((k0x[imidx - 1] * 1 / 2), 2);
	float	kmaxy = pow((k0y[imidy - 1] * 1 / 2), 2);

	if (kmaxy <= kmaxx)
	{
		bandwidthkmax = kmaxy;
	}
	else
	{
		bandwidthkmax = kmaxx;
	};

	// k not k^2.
	bandwidthkmax = sqrt(bandwidthkmax);

	// Bandlimit by FDdz size
	float fnkx = resolution;
	float fnky = resolution;

	float p1 = fnkx / (2 * SimSizeX);
	float p2 = fnky / (2 * SimSizeY);
	float p12 = p1*p1;
	float p22 = p2*p2;

	float ke2 = (.666666f)*(p12 + p22);

	float quadraticA = (ke2*ke2 * 16 * Pi*Pi*Pi*Pi) - (32 * Pi*Pi*Pi*ke2*sigma2*V2 / wavelength) + (16 * Pi*Pi*sigma2*sigma2*V2*V2 / (wavelength*wavelength));
	float quadraticB = 16 * Pi*Pi*(ke2 - (sigma2*V2 / (Pi*wavelength)) - (1 / (4 * wavelength*wavelength)));
	float quadraticC = 3;
	float quadraticB24AC = quadraticB * quadraticB - 4 * quadraticA*quadraticC;

	// Now use these to determine acceptable resolution or enforce extra band limiting beyond 2/3
	if (quadraticB24AC<0)
	{
		//TODO: Need an actual exception and message for these circumstances..
		/*
		cout << "No stable solution exists for these conditions in FD Multislice" << endl;
		return;
		*/
	}

	float b24ac = sqrtf(quadraticB24AC);
	float maxStableDz = (-quadraticB + b24ac) / (2 * quadraticA);
	maxStableDz = 0.99*sqrtf(maxStableDz);

	// Presumably because it would take ages otherwise???
	if (maxStableDz>0.06)
		maxStableDz = 0.06;

	FDdz = maxStableDz;

	int	nFDSlices = ceil((AtomicStructure->MaximumZ - AtomicStructure->MinimumZ) / maxStableDz);
	// Prevent 0 slices for perfectly flat sample
	nFDSlices += (nFDSlices == 0);

	// Set class variables
	NumberOfFDSlices = nFDSlices;

	clXFrequencies = Buffer(new clMemory(resolution*sizeof(cl_float)));
	clYFrequencies = Buffer(new clMemory(resolution*sizeof(cl_float)));

	clXFrequencies->Write(k0x);
	clYFrequencies->Write(k0y);

	// Setup Fourier Transforms
	FourierTrans = FourierKernel(new clFourier(clState::context, clState::clq));
	FourierTrans->Setup(resolution, resolution);

	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
	clWaveFunction2.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
	clWaveFunction3.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
	clWaveFunction4.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));

	// might not need to be vectors, onbly if stem needs them
	if (FD)
	{
		clWaveFunction1Minus.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
		clWaveFunction1Plus.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
	}

	clTDSk.resize(1);
	clTDSx.resize(1);

	clTDSx[0].resize(resolution*resolution);
	clTDSk[0].resize(resolution*resolution);

	clImageWaveFunction = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPropagator = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPotential = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));

	// Set initial wavefunction to 1+0i
	Kernel InitialiseWavefunction = Kernel(new clKernel(InitialiseWavefunctionSource, clState::context, clState::cldev, "clInitialiseWavefunction", clState::clq));
	InitialiseWavefunction->BuildKernelOld();

	SumReduction = Kernel(new clKernel(floatSumReductionsource2, clState::context, clState::cldev, "clFloatSumReduction", clState::clq));
	SumReduction->BuildKernelOld();

	BandLimit = Kernel(new clKernel(BandLimitSource, clState::context, clState::cldev, "clBandLimit", clState::clq));
	BandLimit->BuildKernelOld();

	fftShift = Kernel(new clKernel(fftShiftSource, clState::context, clState::cldev, "clfftShift", clState::clq));
	fftShift->BuildKernelOld();

	fftShift->SetArgS(clWaveFunction2[0], clWaveFunction3[0], resolution, resolution);

	float InitialValue = 1.0f;
	InitialiseWavefunction->SetArgS(clWaveFunction1[0], resolution, resolution, InitialValue);

	BandLimit->SetArgS(clWaveFunction3[0], resolution, resolution, bandwidthkmax, clXFrequencies, clYFrequencies);

	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	if (FD)
	{
		InitialiseWavefunction->SetArgT(0, clWaveFunction1Minus);
	}

	InitialiseWavefunction->Enqueue(WorkSize);

	if (Full3D)
	{
		BinnedAtomicPotential = Kernel(new clKernel(clState::context, clState::cldev, "clBinnedAtomicPotentialOpt", clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialOpt2.cl");
	}
	else if (FD)
	{
		BinnedAtomicPotential = Kernel(new clKernel(clState::context, clState::cldev, "clBinnedAtomicPotentialOptFD", clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialOptFD2.cl");
	}
	else
	{
		BinnedAtomicPotential = Kernel(new clKernel(clState::context, clState::cldev, "clBinnedAtomicPotentialConventional", clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialConventional2.cl");
	}

	BinnedAtomicPotential->BuildKernel();

	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f / BlockScaleX);
	int loadblocksy = ceil(3.0f / BlockScaleY);
	int loadblocksz = ceil(3.0f / AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential->SetArgT(0, clPotential);
	BinnedAtomicPotential->SetArgT(5, AtomicStructure->AtomicStructureParameterisation);
	BinnedAtomicPotential->SetArgT(7, resolution);
	BinnedAtomicPotential->SetArgT(8, resolution);
	BinnedAtomicPotential->SetArgT(12, AtomicStructure->dz);
	BinnedAtomicPotential->SetArgT(13, pixelscale);
	BinnedAtomicPotential->SetArgT(14, AtomicStructure->xBlocks);
	BinnedAtomicPotential->SetArgT(15, AtomicStructure->yBlocks);
	BinnedAtomicPotential->SetArgT(16, AtomicStructure->MaximumX);
	BinnedAtomicPotential->SetArgT(17, AtomicStructure->MinimumX);
	BinnedAtomicPotential->SetArgT(18, AtomicStructure->MaximumY);
	BinnedAtomicPotential->SetArgT(19, AtomicStructure->MinimumY);
	BinnedAtomicPotential->SetArgT(20, loadblocksx);
	BinnedAtomicPotential->SetArgT(21, loadblocksy);
	BinnedAtomicPotential->SetArgT(22, loadblocksz);
	BinnedAtomicPotential->SetArgT(23, sigma2); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential->SetArgT(24, startx); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential->SetArgT(25, starty); // Not sure why i am using sigma 2 and not sigma...

	if (Full3D)
		BinnedAtomicPotential->SetArgT(26, full3dints);

	// Also need to generate propagator.
	GeneratePropagator = Kernel(new clKernel(clState::context, clState::cldev, "clGeneratePropagator", clState::clq));
	GeneratePropagator->loadProgSource("GeneratePropagator.cl");
	GeneratePropagator->BuildKernel();

	GeneratePropagator->SetArgT(0, clPropagator);
	GeneratePropagator->SetArgT(1, clXFrequencies);
	GeneratePropagator->SetArgT(2, clYFrequencies);
	GeneratePropagator->SetArgT(3, resolution);
	GeneratePropagator->SetArgT(4, resolution);

	if (FD)
	{
		GeneratePropagator->SetArgT(5, FDdz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	}
	else
	{
		GeneratePropagator->SetArgT(5, AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	}


	GeneratePropagator->SetArgT(6, wavelength);
	GeneratePropagator->SetArgT(7, bandwidthkmax);

	GeneratePropagator->Enqueue(WorkSize);

	// And multiplication kernel
	ComplexMultiply = Kernel(new clKernel(clState::context, clState::cldev, "clComplexMultiply", clState::clq));
	ComplexMultiply->loadProgSource("Multiply.cl");
	ComplexMultiply->BuildKernel();

	ComplexMultiply->SetArgT(3, resolution);
	ComplexMultiply->SetArgT(4, resolution);

	// And the imaging kernel
	ImagingKernel = Kernel(new clKernel(imagingKernelSource, clState::context, clState::cldev, "clImagingKernel", clState::clq));
	ImagingKernel->BuildKernelOld();

	int waves = 1;
	ewmin.resize(waves);
	ewmax.resize(waves);
	ewmin2.resize(waves);
	ewmax2.resize(waves);
	diffmin.resize(waves);
	diffmax.resize(waves);
	tdsmin.resize(waves);
	tdsmax.resize(waves);

	if (FD)
	{
		// Need Grad Kernel and FiniteDifference also
		GradKernel = Kernel(new clKernel(clState::context, clState::cldev, "clGrad", clState::clq));
		GradKernel->loadProgSource("GradKernel.cl");
		GradKernel->BuildKernel();

		FiniteDifference = Kernel(new clKernel(clState::context, clState::cldev, "clFiniteDifference", clState::clq));
		FiniteDifference->loadProgSource("FiniteDifference.cl");
		FiniteDifference->BuildKernel();

		InitialiseWavefunction->SetArgT(0, clWaveFunction1[0]);
		InitialiseWavefunction->Enqueue(WorkSize);
	}

	 clFinish(clState::clq->cmdQueue);
};

void TEMSimulation::initialiseSTEMSimulation(int res, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, float dz, int full3dints, int waves)
{
	FDMode = false;

	resolution = res;
	AtomicStructure = Structure;
	AtomicStructure->dz = dz;

	// Get size of input structure
	float RealSizeX = endx - startx;
	float RealSizeY = endy - starty;
	pixelscale = max(RealSizeX, RealSizeY) / resolution;

	// Work out size of each binned block of atoms
	float BlockScaleX = (AtomicStructure->MaximumX - AtomicStructure->MinimumX) / AtomicStructure->xBlocks;
	float BlockScaleY = (AtomicStructure->MaximumY - AtomicStructure->MinimumY) / AtomicStructure->yBlocks;

	// Work out area that is to be simulated
	float SimSizeX = pixelscale * resolution;
	float SimSizeY = SimSizeX;

	float	Pi = 3.1415926f;
	float	V = STEMParams->kilovoltage;
	float	a0 = 52.9177e-012f;
	float	a0a = a0*1e+010f;
	float	echarge = 1.6e-019f;
	wavelength = 6.63e-034f*3e+008f / sqrt((echarge*V * 1000 * (2 * 9.11e-031f*9e+016f + echarge*V * 1000)))*1e+010f;
	float	sigma = 2 * Pi * ((511.0f + V) / (2.0f*511.0f + V)) / (V * wavelength);
	float	sigma2 = (2 * Pi / (wavelength * V * 1000)) * ((9.11e-031f*9e+016f + echarge*V * 1000) / (2 * 9.11e-031f*9e+016f + echarge*V * 1000));
	float	fix = 300.8242834f / (4 * Pi*Pi*a0a*echarge);
	float	V2 = V * 1000;

	// Now we can set up frequencies and fourier transforms.
	int imidx = floor(resolution / 2 + 0.5);
	int imidy = floor(resolution / 2 + 0.5);

	std::vector<float> k0x;
	std::vector<float> k0y;

	float temp;

	for (int i = 1; i <= resolution; i++)
	{
		if ((i - 1) > imidx)
			temp = ((i - 1) - resolution) / SimSizeX;
		else temp = (i - 1) / SimSizeX;
		k0x.push_back(temp);
	}

	for (int i = 1; i <= resolution; i++)
	{
		if ((i - 1) > imidy)
			temp = ((i - 1) - resolution) / SimSizeY;
		else temp = (i - 1) / SimSizeY;
		k0y.push_back(temp);
	}

	// Find maximum frequency for bandwidth limiting rule....
	bandwidthkmax = 0;

	float	kmaxx = pow((k0x[imidx - 1] * 1 / 2), 2);
	float	kmaxy = pow((k0y[imidy - 1] * 1 / 2), 2);

	if (kmaxy <= kmaxx)
	{
		bandwidthkmax = kmaxy;
	}
	else
	{
		bandwidthkmax = kmaxx;
	};

	// k not k^2.
	bandwidthkmax = sqrt(bandwidthkmax);

	// Bandlimit by FDdz size

	clXFrequencies = Buffer(new clMemory(resolution*sizeof(cl_float)));
	clYFrequencies = Buffer(new clMemory(resolution*sizeof(cl_float)));

	clXFrequencies->Write(k0x);
	clYFrequencies->Write(k0y);

	// Setup Fourier Transforms
	FourierTrans = FourierKernel(new clFourier(clState::context, clState::clq));
	FourierTrans->Setup(resolution, resolution);

	clTDSk.resize(waves);
	clTDSx.resize(waves);

	// Initialise Wavefunctions and Create other buffers...
	for (int i = 1; i <= waves; i++)
	{
		clWaveFunction1.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
		clWaveFunction2.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
		clWaveFunction4.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));
		clTDSDiff.push_back(Buffer(new clMemory(resolution*resolution*sizeof(cl_float))));

		clTDSx[i - 1].resize(resolution*resolution);
		clTDSk[i - 1].resize(resolution*resolution);
	}

	clWaveFunction3.push_back(Buffer(new clMemory(resolution * resolution * sizeof(cl_float2))));

	clImageWaveFunction = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));

	clTDSMaskDiff = Buffer(new clMemory(resolution*resolution*sizeof(cl_float)));

	clPropagator = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPotential = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));

	// Set initial wavefunction to 1+0i
	InitialiseSTEMWavefunction = Kernel(new clKernel(InitialiseSTEMWavefunctionSource, clState::context, clState::cldev, "clInitialiseSTEMWavefunction", clState::clq));
	InitialiseSTEMWavefunction->BuildKernelOld();

	SumReduction = Kernel(new clKernel(floatSumReductionsource2, clState::context, clState::cldev, "clFloatSumReduction", clState::clq));
	SumReduction->BuildKernelOld();

	BandLimit = Kernel(new clKernel(BandLimitSource, clState::context, clState::cldev, "clBandLimit", clState::clq));
	BandLimit->BuildKernelOld();

	fftShift = Kernel(new clKernel(fftShiftSource, clState::context, clState::cldev, "clfftShift", clState::clq));
	fftShift->BuildKernelOld();

	fftShift->SetArgT(0, clWaveFunction2[0]);
	fftShift->SetArgT(1, clWaveFunction3[0]);
	fftShift->SetArgT(2, resolution);
	fftShift->SetArgT(3, resolution);

	MultiplyCL = Kernel(new clKernel(multiplySource, clState::context, clState::cldev, "clMultiply", clState::clq));
	MultiplyCL->BuildKernelOld();

	MaskingKernel = Kernel(new clKernel(bandPassSource, clState::context, clState::cldev, "clBandPass", clState::clq));
	MaskingKernel->BuildKernelOld();

	TDSMaskingKernel = Kernel(new clKernel(floatbandPassSource, clState::context, clState::cldev, "clFloatBandPass", clState::clq));
	TDSMaskingKernel->BuildKernelOld();

	BandLimit->SetArgT(0, clWaveFunction3[0]);
	BandLimit->SetArgT(1, resolution);
	BandLimit->SetArgT(2, resolution);
	BandLimit->SetArgT(3, bandwidthkmax);
	BandLimit->SetArgT(4, clXFrequencies);
	BandLimit->SetArgT(5, clYFrequencies);

	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	WFabsolute = Kernel(new clKernel(abssource2, clState::context, clState::cldev, "clAbs", clState::clq));
	WFabsolute->BuildKernelOld();

	if (Full3D)
	{
		BinnedAtomicPotential = Kernel(new clKernel(clState::context, clState::cldev, "clBinnedAtomicPotentialOpt", clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialOpt2.cl");
	}
	else
	{
		BinnedAtomicPotential = Kernel(new clKernel(clState::context, clState::cldev, "clBinnedAtomicPotentialConventional", clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialConventional2.cl");
	}
	BinnedAtomicPotential->BuildKernel();
	//BinnedAtomicPotential->BuildKernelOld();

	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f / ((AtomicStructure->MaximumX - AtomicStructure->MinimumX) / (AtomicStructure->xBlocks)));
	int loadblocksy = ceil(3.0f / ((AtomicStructure->MaximumY - AtomicStructure->MinimumY) / (AtomicStructure->yBlocks)));
	int loadblocksz = ceil(3.0f / AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential->SetArgT(0, clPotential);
	BinnedAtomicPotential->SetArgT(5, AtomicStructure->AtomicStructureParameterisation);
	BinnedAtomicPotential->SetArgT(7, resolution);
	BinnedAtomicPotential->SetArgT(8, resolution);
	BinnedAtomicPotential->SetArgT(12, AtomicStructure->dz);
	BinnedAtomicPotential->SetArgT(13, pixelscale);
	BinnedAtomicPotential->SetArgT(14, AtomicStructure->xBlocks);
	BinnedAtomicPotential->SetArgT(15, AtomicStructure->yBlocks);
	BinnedAtomicPotential->SetArgT(16, AtomicStructure->MaximumX);
	BinnedAtomicPotential->SetArgT(17, AtomicStructure->MinimumX);
	BinnedAtomicPotential->SetArgT(18, AtomicStructure->MaximumY);
	BinnedAtomicPotential->SetArgT(19, AtomicStructure->MinimumY);
	BinnedAtomicPotential->SetArgT(20, loadblocksx);
	BinnedAtomicPotential->SetArgT(21, loadblocksy);
	BinnedAtomicPotential->SetArgT(22, loadblocksz);
	BinnedAtomicPotential->SetArgT(23, sigma2); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential->SetArgT(24, startx);
	BinnedAtomicPotential->SetArgT(25, starty);

	if (Full3D)
		BinnedAtomicPotential->SetArgT(26, full3dints);

	// Also need to generate propagator.
	GeneratePropagator = Kernel(new clKernel(clState::context, clState::cldev, "clGeneratePropagator", clState::clq));
	GeneratePropagator->loadProgSource("GeneratePropagator.cl");
	GeneratePropagator->BuildKernel();

	GeneratePropagator->SetArgT(0, clPropagator);
	GeneratePropagator->SetArgT(1, clXFrequencies);
	GeneratePropagator->SetArgT(2, clYFrequencies);
	GeneratePropagator->SetArgT(3, resolution);
	GeneratePropagator->SetArgT(4, resolution);
	GeneratePropagator->SetArgT(5, AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	GeneratePropagator->SetArgT(6, wavelength);
	GeneratePropagator->SetArgT(7, bandwidthkmax);

	GeneratePropagator->Enqueue(WorkSize);

	// And multiplication kernel
	ComplexMultiply = Kernel(new clKernel(clState::context, clState::cldev, "clComplexMultiply", clState::clq));
	ComplexMultiply->loadProgSource("Multiply.cl");
	ComplexMultiply->BuildKernel();

	ComplexMultiply->SetArgT(3, resolution);
	ComplexMultiply->SetArgT(4, resolution);

	// And the imaging kernel
	ImagingKernel = Kernel(new clKernel(imagingKernelSource, clState::context, clState::cldev, "clImagingKernel", clState::clq));
	ImagingKernel->BuildKernelOld();

	ewmin.resize(waves);
	ewmax.resize(waves);
	diffmin.resize(waves);
	diffmax.resize(waves);
	tdsmin.resize(waves);
	tdsmax.resize(waves);

	clFinish(clState::clq->cmdQueue);
};

void TEMSimulation::initialiseSTEMWaveFunction(float posx, float posy, int wave)
{
	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	// Fix inverted images
	posx = resolution - 1 - posx;
	posy = resolution - 1 - posy;

	InitialiseSTEMWavefunction->SetArgS(clWaveFunction2[wave - 1], resolution, resolution, clXFrequencies, clYFrequencies, posx, posy, STEMParams->aperturesizemrad, pixelscale, STEMParams->defocus, STEMParams->spherical, wavelength);
	InitialiseSTEMWavefunction->Enqueue(WorkSize);

	// IFFT
	FourierTrans->Enqueue(clWaveFunction2[wave - 1], clWaveFunction1[wave - 1], CLFFT_BACKWARD);
};

void TEMSimulation::doMultisliceStep(int stepno, int steps, int waves)
{
	if (FDMode)
	{
		doMultisliceStepFD(stepno, waves);
		return;
	}

	// Work out current z position based on step size and current step
	// Should be one set of bins for each individual slice
	int slice = stepno - 1;
	int slices = steps;

	// Didn't have MinimumZ so it wasnt correctly rescaled z-axis from 0 to SizeZ...
	float currentz = AtomicStructure->MaximumZ - AtomicStructure->MinimumZ - slice * AtomicStructure->dz;

	int topz = slice - ceil(3.0f / AtomicStructure->dz);
	int bottomz = slice + ceil(3.0f / AtomicStructure->dz);

	if (topz < 0)
		topz = 0;
	if (bottomz >= slices)
		bottomz = slices - 1;

	BinnedAtomicPotential->SetArgT(1, AtomicStructure->clAtomx);
	BinnedAtomicPotential->SetArgT(2, AtomicStructure->clAtomy);
	BinnedAtomicPotential->SetArgT(3, AtomicStructure->clAtomz);
	BinnedAtomicPotential->SetArgT(4, AtomicStructure->clAtomZ);
	BinnedAtomicPotential->SetArgT(6, AtomicStructure->clBlockStartPositions);
	BinnedAtomicPotential->SetArgT(9, slice);
	BinnedAtomicPotential->SetArgT(10, slices);
	BinnedAtomicPotential->SetArgT(11, currentz);

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	size_t* LocalWork = new size_t[3];

	LocalWork[0] = 16;
	LocalWork[1] = 16;
	LocalWork[2] = 1;

	BinnedAtomicPotential->Enqueue3D(Work, LocalWork);

	FourierTrans->Enqueue(clPotential, clWaveFunction3[0], CLFFT_FORWARD);
	BandLimit->Enqueue(Work);
	FourierTrans->Enqueue(clWaveFunction3[0], clPotential, CLFFT_BACKWARD);

	// Now for the rest of the multislice steps
	for (int i = 1; i <= waves; i++)
	{
		//Multiply with wavefunction
		ComplexMultiply->SetArgT(0, clPotential);
		ComplexMultiply->SetArgT(1, clWaveFunction1[i - 1]);
		ComplexMultiply->SetArgT(2, clWaveFunction2[i - 1]);
		ComplexMultiply->Enqueue(Work);

		// Propagate
		FourierTrans->Enqueue(clWaveFunction2[i - 1], clWaveFunction3[0], CLFFT_FORWARD);

		// BandLimit OK here?
		ComplexMultiply->SetArgT(0, clWaveFunction3[0]);
		ComplexMultiply->SetArgT(1, clPropagator);
		ComplexMultiply->SetArgT(2, clWaveFunction2[i - 1]);
		ComplexMultiply->Enqueue(Work);


		FourierTrans->Enqueue(clWaveFunction2[i - 1], clWaveFunction1[i - 1], CLFFT_BACKWARD);
	}
	clFinish(clState::clq->cmdQueue);
};

void TEMSimulation::doMultisliceStepFD(int stepno, int waves)
{
	// Work out current z position based on step size and current step
	// Should be one set of bins for each individual slice
	int slice = stepno - 1; // this slice needs to be which bunch of atoms we are in line with...
	int slices = AtomicStructure->nSlices;

	// Didn't have MinimumZ so it wasnt correctly rescaled z-axis from 0 to SizeZ...
	float currentz = AtomicStructure->MaximumZ - AtomicStructure->MinimumZ - slice * FDdz;

	int atomslice = floor(slice*FDdz / AtomicStructure->dz);

	int topz = slice - ceil(3.0f / AtomicStructure->dz);
	int bottomz = slice + ceil(3.0f / AtomicStructure->dz);

	if (topz < 0)
		topz = 0;
	if (bottomz >= slices)
		bottomz = slices - 1;

	BinnedAtomicPotential->SetArgT(1, AtomicStructure->clAtomx);
	BinnedAtomicPotential->SetArgT(2, AtomicStructure->clAtomy);
	BinnedAtomicPotential->SetArgT(3, AtomicStructure->clAtomz);
	BinnedAtomicPotential->SetArgT(4, AtomicStructure->clAtomZ);
	BinnedAtomicPotential->SetArgT(6, AtomicStructure->clBlockStartPositions);
	BinnedAtomicPotential->SetArgT(9, atomslice);
	BinnedAtomicPotential->SetArgT(10, slices);
	BinnedAtomicPotential->SetArgT(11, currentz);

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	size_t* LocalWork = new size_t[3];

	LocalWork[0] = 16;
	LocalWork[1] = 16;
	LocalWork[2] = 1;

	BinnedAtomicPotential->Enqueue3D(Work, LocalWork);

	FourierTrans->Enqueue(clPotential, clWaveFunction3[0], CLFFT_FORWARD);
	BandLimit->Enqueue(Work);
	FourierTrans->Enqueue(clWaveFunction3[0], clPotential, CLFFT_BACKWARD);

	// //FT Psi into Grad2.
	// FourierTrans->Enqueue(clWaveFunction1, clWaveFunction3, CLFFT_FORWARD);

	// //Grad Kernel on Grad2.
	// GradKernel->SetArgS(clWaveFunction3, clXFrequencies, clYFrequencies, resolution, resolution);
	// GradKernel->Enqueue(Work);

	// //IFT Grad2 into Grad.
	// FourierTrans->Enqueue(clWaveFunction3, clWaveFunction4, CLFFT_BACKWARD);

	// //FD Kernel
	// FiniteDifference->SetArgS(clPotential, clWaveFunction4, clWaveFunction1Minus, clWaveFunction1, clWaveFunction1Plus, FDdz, wavelength, FDsigma, resolution, resolution);
	// FiniteDifference->Enqueue(Work);


	// //Bandlimit PsiPlus
	// FourierTrans->Enqueue(clWaveFunction1Plus, clWaveFunction3, CLFFT_FORWARD);
	// BandLimit->Enqueue(Work);
	// FourierTrans->Enqueue(clWaveFunction3, clWaveFunction1Plus, CLFFT_BACKWARD);

	// // Psi becomes PsiMinus
	// clEnqueueCopyBuffer(clState::clq->cmdQueue, clWaveFunction1->buffer, clWaveFunction1Minus->buffer, 0, 0, resolution*resolution*sizeof(cl_float2), 0, nullptr, nullptr);

	// // PsiPlus becomes Psi.
	// clEnqueueCopyBuffer(clState::clq->cmdQueue, clWaveFunction1Plus->buffer, clWaveFunction1->buffer, 0, 0, resolution*resolution*sizeof(cl_float2), 0, nullptr, nullptr);



	// // To maintain status with other versions resulting end arrays should still be as follows.
	// // Finished wavefunction in real spaaaaaace in clWaveFunction1.
	// // Finished wavefunction in reciprocal spaaaaaace in clWaveFunction2.
	// // 3 and 4 were previously temporary.

	// FourierTrans->Enqueue(clWaveFunction1, clWaveFunction2, CLFFT_FORWARD);

	// clFinish(clState::clq->cmdQueue);
};

float TEMSimulation::getSTEMPixel(float inner, float outer, float xc, float yc, int wave)
{
	// NOTE FOR TDS SHOULD USE THE clTDSk vector and mask this to get results.... (can use TDS everytime its just set to 1 run??).
	// clWaveFunction3 should contain the diffraction pattern, shouldnt be needed elsewhere is STEM mode so should be safe to modify?
	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	float pxFreq = (resolution * pixelscale);

	float innerFreq = inner / (1000 * wavelength);
	float innerPx = innerFreq*pxFreq;

	float outerFreq = outer / (1000 * wavelength);
	float outerPx = outerFreq*pxFreq;

	float xcFreq = xc / (1000 * wavelength);
	float xcPx = xcFreq*pxFreq;

	float ycFreq = yc / (1000 * wavelength);
	float ycPx = ycFreq*pxFreq;

	clTDSDiff[wave - 1]->Write(clTDSk[wave - 1]);

	TDSMaskingKernel->SetArgS(clTDSMaskDiff, clTDSDiff[wave - 1], resolution, resolution, innerPx, outerPx, xcPx, ycPx);

	TDSMaskingKernel->Enqueue(WorkSize);

	int totalSize = resolution*resolution;
	int nGroups = totalSize / 256;

	size_t* globalSizeSum = new size_t[3];
	size_t* localSizeSum = new size_t[3];

	globalSizeSum[0] = totalSize;
	globalSizeSum[1] = 1;
	globalSizeSum[2] = 1;
	localSizeSum[0] = 256;
	localSizeSum[1] = 1;
	localSizeSum[2] = 1;

	return FloatSumReduction(clTDSMaskDiff->buffer, globalSizeSum, localSizeSum, nGroups, totalSize);
};

void TEMSimulation::getCTEMImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1[0]->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] = sqrt(compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (data[i] > max)
			max = data[i];
		if (data[i] < min)
			min = data[i];
	}

	imagemin = min;
	imagemax = max;
};

void TEMSimulation::getCTEMImage(float* data, int resolution, float doseperpix, int binning, int detector)
{
	std::vector<float*> ntfs;

	ntfs.push_back(NULL);
	ntfs.push_back(oriusNTF);
	ntfs.push_back(k2NTF);

	Buffer Temp1 = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	Buffer ntfbuffer = Buffer(new clMemory(725 * sizeof(cl_float)));

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	Kernel NTF = Kernel(new clKernel(NTFSource, clState::context, clState::cldev, "clNTF", clState::clq));
	NTF->BuildKernelOld();

	Kernel ABS = Kernel(new clKernel(SqAbsSource, clState::context, clState::cldev, "clSqAbs", clState::clq));
	ABS->BuildKernelOld();

	float conversionfactor = 8; //CCD counts per electron.
	float Ntot = doseperpix*binning*binning; // Get this passed in, its dose per pixel i think.

	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1[0]->Read(compdata);

	for (int i = 0; i < resolution * resolution; i++)
	{
		double random = ((double)rand() / (RAND_MAX + 1));
		double random2 = ((double)rand() / (RAND_MAX + 1));
		double rstdnormal = sqrt(-2.0f * +log(FLT_MIN + random))*(sin(2.0f * CL_M_PI * random2));

		float val = sqrt(compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);
		// Get absolute value for display...	
		compdata[i].s[0] = floor(Ntot * val + sqrt(fabs(Ntot*val))*rstdnormal); // Was round not floor
		compdata[i].s[1] = 0;

	}

	clWaveFunction1[0]->Write(compdata);

	FourierTrans->Enqueue(clWaveFunction1[0], Temp1, CLFFT_FORWARD);

	clEnqueueWriteBuffer(clState::clq->cmdQueue, ntfbuffer->buffer, CL_TRUE, 0, 725 * sizeof(float), ntfs[detector], 0, NULL, NULL);

	NTF->SetArgS(Temp1, ntfbuffer, resolution, resolution, binning);
	NTF->Enqueue(Work);

	FourierTrans->Enqueue(Temp1, clWaveFunction1[0], CLFFT_BACKWARD);

	clWaveFunction1[0]->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{

		float val = sqrt(compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);
		// Get absolute value for display...	
		data[i] = val; // Was round not floor

		// Find max,min for contrast limits
		if (data[i] > max)
			max = data[i];
		if (data[i] < min)
			min = data[i];
	}

	imagemin = min;
	imagemax = max;
};

void TEMSimulation::simulateCTEM()
{
	// Set up some temporary memory objects for the image simulation
	Buffer Temp1 = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	Buffer dqebuffer = Buffer(new clMemory(725 * sizeof(cl_float)));

	Kernel DQE = Kernel(new clKernel(DQESource, clState::context, clState::cldev, "clDQE", clState::clq));
	DQE->BuildKernelOld();

	Kernel ABS = Kernel(new clKernel(SqAbsSource, clState::context, clState::cldev, "clSqAbs", clState::clq));
	ABS->BuildKernelOld();

	// Set arguments for imaging kernel
	ImagingKernel->SetArgT(0, clWaveFunction2[0]);
	ImagingKernel->SetArgT(1, clImageWaveFunction);
	ImagingKernel->SetArgT(2, resolution);
	ImagingKernel->SetArgT(3, resolution);
	ImagingKernel->SetArgT(4, TEMParams->spherical);
	ImagingKernel->SetArgT(5, TEMParams->defocus);
	ImagingKernel->SetArgT(6, TEMParams->astigmag);
	ImagingKernel->SetArgT(7, TEMParams->astigang);
	ImagingKernel->SetArgT(8, TEMParams->astig2mag);
	ImagingKernel->SetArgT(9, TEMParams->astig2ang);
	ImagingKernel->SetArgT(10, TEMParams->aperturesizemrad);
	ImagingKernel->SetArgT(11, wavelength);
	ImagingKernel->SetArgT(12, clXFrequencies);
	ImagingKernel->SetArgT(13, clYFrequencies);
	ImagingKernel->SetArgT(14, TEMParams->beta);
	ImagingKernel->SetArgT(15, TEMParams->delta);

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	ImagingKernel->Enqueue(Work);

	// Now get and display absolute value
	FourierTrans->Enqueue(clImageWaveFunction, clWaveFunction1[0], CLFFT_BACKWARD);

	ABS->SetArgS(clWaveFunction1[0], Temp1, resolution, resolution);
	ABS->Enqueue(Work);

	FourierTrans->Enqueue(Temp1, clImageWaveFunction, CLFFT_FORWARD);
	int binning = 1;
	DQE->SetArgS(clImageWaveFunction, dqebuffer, resolution, resolution, binning);
	DQE->Enqueue(Work);

	FourierTrans->Enqueue(clImageWaveFunction, Temp1, CLFFT_BACKWARD);

	ABS->SetArgS(Temp1, clImageWaveFunction, resolution, resolution);
	ABS->Enqueue(Work);

	// Maybe update diffractogram image also...
	clEnqueueCopyBuffer(clState::clq->cmdQueue, clImageWaveFunction->buffer, clWaveFunction4[0]->buffer, 0, 0, resolution*resolution*sizeof(cl_float2), 0, 0, 0);
};

void TEMSimulation::simulateCTEM(int detector, int binning)
{
	std::vector<float*> dqes;

	dqes.push_back(NULL);
	dqes.push_back(oriusDQE);
	dqes.push_back(k2DQE);

	// Set up some temporary memory objects for the image simulation
	Buffer Temp1 = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	Buffer dqebuffer = Buffer(new clMemory(725 * sizeof(cl_float)));

	Kernel DQE = Kernel(new clKernel(DQESource, clState::context, clState::cldev, "clDQE", clState::clq));
	DQE->BuildKernelOld();

	Kernel ABS = Kernel(new clKernel(SqAbsSource, clState::context, clState::cldev, "clSqAbs", clState::clq));
	ABS->BuildKernelOld();

	// Set arguments for imaging kernel
	ImagingKernel->SetArgT(0, clWaveFunction2[0]);
	ImagingKernel->SetArgT(1, clImageWaveFunction);
	ImagingKernel->SetArgT(2, resolution);
	ImagingKernel->SetArgT(3, resolution);
	ImagingKernel->SetArgT(4, TEMParams->spherical);
	ImagingKernel->SetArgT(5, TEMParams->defocus);
	ImagingKernel->SetArgT(6, TEMParams->astigmag);
	ImagingKernel->SetArgT(7, TEMParams->astigang);
	ImagingKernel->SetArgT(8, TEMParams->astig2mag);
	ImagingKernel->SetArgT(9, TEMParams->astig2ang);
	ImagingKernel->SetArgT(10, TEMParams->aperturesizemrad);
	ImagingKernel->SetArgT(11, wavelength);
	ImagingKernel->SetArgT(12, clXFrequencies);
	ImagingKernel->SetArgT(13, clYFrequencies);
	ImagingKernel->SetArgT(14, TEMParams->beta);
	ImagingKernel->SetArgT(15, TEMParams->delta);

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	ImagingKernel->Enqueue(Work);

	// Now get and display absolute value
	FourierTrans->Enqueue(clImageWaveFunction, clWaveFunction1[0], CLFFT_BACKWARD);

	ABS->SetArgS(clWaveFunction1[0], Temp1, resolution, resolution);
	ABS->Enqueue(Work);

	FourierTrans->Enqueue(Temp1, clImageWaveFunction, CLFFT_FORWARD);

	clEnqueueWriteBuffer(clState::clq->cmdQueue, dqebuffer->buffer, CL_TRUE, 0, 725 * sizeof(float), dqes[detector], 0, NULL, NULL);
	DQE->SetArgS(clImageWaveFunction, dqebuffer, resolution, resolution, binning);
	DQE->Enqueue(Work);

	FourierTrans->Enqueue(clImageWaveFunction, Temp1, CLFFT_BACKWARD);

	ABS->SetArgS(Temp1, clImageWaveFunction, resolution, resolution);
	ABS->Enqueue(Work);

	// Maybe update diffractogram image also...
	clEnqueueCopyBuffer(clState::clq->cmdQueue, clImageWaveFunction->buffer, clWaveFunction4[0]->buffer, 0, 0, resolution*resolution*sizeof(cl_float2), 0, 0, 0);
};

void TEMSimulation::getDiffImage(float* data, int resolution, int wave)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	fftShift->SetArgT(0, clWaveFunction2[wave - 1]);
	fftShift->Enqueue(Work);

	clWaveFunction3[0]->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...
		data[i] += (compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (data[i] > max)
			max = data[i];
		if (data[i] < min)
			min = data[i];
	}

	diffmin[wave - 1] = min;
	diffmax[wave - 1] = max;
};

void TEMSimulation::getSTEMDiff(int wave)
{
	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	fftShift->SetArgT(0, clWaveFunction2[wave - 1]);
	fftShift->Enqueue(Work);

	clWaveFunction3[0]->Read(compdata);

	max = CL_FLT_MIN;
	min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...
		clTDSk[wave - 1][i] = (compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (clTDSk[wave - 1][i] > max)
			max = clTDSk[wave - 1][i];
		if (clTDSk[wave - 1][i] < min)
			min = clTDSk[wave - 1][i];
	}

	tdsmin[wave - 1] = min;
	tdsmax[wave - 1] = max;
};

void TEMSimulation::getEWImage(float* data, int resolution, int wave)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1[wave - 1]->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...
		data[i] = sqrt(compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (data[i] > max)
			max = data[i];
		if (data[i] < min)
			min = data[i];
	}

	ewmin[wave - 1] = min;
	ewmax[wave - 1] = max;
};

void TEMSimulation::getEWImage2(float* data, int resolution, int wave)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1[0]->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get sqrt abs value for display...
		data[i] = hypot(compdata[i].s[1], compdata[i].s[0]);

		// Find max,min for contrast limits
		if (data[i] > max)
			max = data[i];
		if (data[i] < min)
			min = data[i];
	}

	ewmin2[wave - 1] = min;
	ewmax2[wave - 1] = max;
};

void TEMSimulation::addTDS(int wave)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1[wave - 1]->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...
		clTDSx[wave - 1][i] += (compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (clTDSx[wave - 1][i] > max)
			max = clTDSx[wave - 1][i];
		if (clTDSx[wave - 1][i] < min)
			min = clTDSx[wave - 1][i];
	}

	ewmin[wave - 1] = min;
	ewmax[wave - 1] = max;

	// Original data is complex so copy complex version down first

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	fftShift->SetArgT(0, clWaveFunction2[wave - 1]);
	fftShift->Enqueue(Work);

	clWaveFunction3[0]->Read(compdata);

	max = CL_FLT_MIN;
	min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...
		clTDSk[wave - 1][i] += (compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (clTDSk[wave - 1][i] > max)
			max = clTDSk[wave - 1][i];
		if (clTDSk[wave - 1][i] < min)
			min = clTDSk[wave - 1][i];
	}

	tdsmin[wave - 1] = min;
	tdsmax[wave - 1] = max;
};

void TEMSimulation::clearTDS(int waves)
{
	for (int i = 1; i <= waves; i++)
	{
		fill(clTDSk[i - 1].begin(), clTDSk[i - 1].end(), 0);
		fill(clTDSx[i - 1].begin(), clTDSx[i - 1].end(), 0);
	}
};

float TEMSimulation::FloatSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize)
{


	clMemory outArray;
	outArray.Create(nGroups*sizeof(cl_float));

	// Create host array to store reduction results.
	std::vector< float> sums(nGroups);

	SumReduction->SetArgT(0, Array);

	// Only really need to do these 3 once...
	SumReduction->SetArgT(1, outArray);
	SumReduction->SetArgT(2, totalSize);
	SumReduction->SetArgLocalMemory(3, 256, clFloat);

	SumReduction->Enqueue3D(globalSizeSum, localSizeSum);

	// Now copy back 
	clEnqueueReadBuffer(clState::clq->cmdQueue, outArray.buffer, CL_TRUE, 0, nGroups*sizeof(cl_float), &sums[0], 0, NULL, NULL);

	// Find out which numbers to read back
	float sum = 0;

	for (int i = 0; i < nGroups; i++)
	{
		sum += sums[i];
	}

	return sum;
};