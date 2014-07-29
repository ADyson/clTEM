#include "TEMSimulation.h"
#include "clKernelCodes2.h"
#include <minmax.h>
#include "mtf.h"


TEMSimulation::TEMSimulation(TEMParameters* temparams, STEMParameters* stemparams)
{
	//this->context = clState::context;
	//this->clq = clState::clq;
	//this->cldev = clState::cldev;
	this->TEMParams = temparams;
	this->STEMParams = stemparams;

};

void TEMSimulation::Initialise(int resolution, MultisliceStructure* Structure, bool Full3D)
{

	this->resolution = resolution;
	this->AtomicStructure = Structure;

	// Get size of input structure
	float RealSizeX = AtomicStructure->MaximumX-AtomicStructure->MinimumX;
	float RealSizeY = AtomicStructure->MaximumY-AtomicStructure->MinimumY;
	pixelscale = max(RealSizeX,RealSizeY)/(resolution);

	// Work out size of each binned block of atoms
	float BlockScaleX = RealSizeX/AtomicStructure->xBlocks; 
	float BlockScaleY = RealSizeY/AtomicStructure->yBlocks;

	// Work out area that is to be simulated
	float SimSizeX = pixelscale * resolution;
	float SimSizeY = SimSizeX;

	float	Pi		= 3.1415926f;	
	float	V		= TEMParams->kilovoltage;
	float	a0		= 52.9177e-012f;
	float	a0a		= a0*1e+010f;
	float	echarge	= 1.6e-019f;
	wavelength		= 6.63e-034f*3e+008f/sqrt((echarge*V*1000*(2*9.11e-031f*9e+016f + echarge*V*1000)))*1e+010f;
	float	sigma	= 2 * Pi * ((511.0f + V) / (2.0f*511.0f + V)) / (V * wavelength);
	float	sigma2	= (2*Pi/(wavelength * V * 1000)) * ((9.11e-031f*9e+016f + echarge*V*1000)/(2*9.11e-031f*9e+016f + echarge*V*1000));
	float	fix		= 300.8242834f/(4*Pi*Pi*a0a*echarge);
	float	V2		= V*1000;

	// Now we can set up frequencies and fourier transforms.

	int imidx = floor(resolution/2 + 0.5);
	int imidy = floor(resolution/2 + 0.5);

	std::vector<float> k0x;
	std::vector<float> k0y;

	float temp;

	for(int i=1 ; i <= resolution ; i++)
	{
		if ((i - 1) > imidx)
			temp = ((i - 1) - resolution)/SimSizeX;
		else temp = (i - 1)/SimSizeX;
		k0x.push_back (temp);
	}

	for(int i=1 ; i <= resolution ; i++)
	{
		if ((i - 1) > imidy)
			temp = ((i - 1) - resolution)/SimSizeY;
		else temp = (i - 1)/SimSizeY;
		k0y.push_back (temp);
	}

	// Find maximum frequency for bandwidth limiting rule....

	 bandwidthkmax=0;

	float	kmaxx = pow((k0x[imidx-1]*1/2),2);
	float	kmaxy = pow((k0y[imidy-1]*1/2),2);
	
	if(kmaxy <= kmaxx)
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
	FourierTrans->Setup(resolution,resolution);

	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction2 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction3 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction4 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));

	clTDSx.resize(resolution*resolution);
	clTDSk.resize(resolution*resolution);

	clImageWaveFunction = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPropagator = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPotential = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));

	// Set initial wavefunction to 1+0i
	Kernel InitialiseWavefunction = Kernel(new clKernel(InitialiseWavefunctionSource, clState::context, clState::cldev, "clInitialiseWavefunction", clState::clq));
	InitialiseWavefunction->BuildKernelOld();

	BandLimit = Kernel(new clKernel(BandLimitSource,clState::context,clState::cldev,"clBandLimit",clState::clq));
	BandLimit->BuildKernelOld();

	fftShift = Kernel( new clKernel(fftShiftSource,clState::context,clState::cldev,"clfftShift",clState::clq));
	fftShift->BuildKernelOld();

	fftShift->SetArgT(0,clWaveFunction2);
	fftShift->SetArgT(1,clWaveFunction3);
	fftShift->SetArgT(2,resolution);
	fftShift->SetArgT(3,resolution);

	float InitialValue = 1.0f;
	InitialiseWavefunction->SetArgT(0,clWaveFunction1);
	InitialiseWavefunction->SetArgT(1,resolution);
	InitialiseWavefunction->SetArgT(2,resolution);
	InitialiseWavefunction->SetArgT(3,InitialValue);

	BandLimit->SetArgT(0,clWaveFunction3);
	BandLimit->SetArgT(1,resolution);
	BandLimit->SetArgT(2,resolution);
	BandLimit->SetArgT(3,bandwidthkmax);
	BandLimit->SetArgT(4,clXFrequencies);
	BandLimit->SetArgT(5,clYFrequencies);

	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	InitialiseWavefunction->Enqueue(WorkSize);

	if (Full3D)
	{
		BinnedAtomicPotential = Kernel( new clKernel(clState::context,clState::cldev,"clBinnedAtomicPotentialOpt",clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialOpt.cl");
	}
	else
	{
		BinnedAtomicPotential = Kernel( new clKernel(clState::context,clState::cldev,"clBinnedAtomicPotentialConventional",clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialConventional.cl");		
	}
	BinnedAtomicPotential->BuildKernel();

	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f/BlockScaleX);
	int loadblocksy = ceil(3.0f/BlockScaleY);
	int loadblocksz = ceil(3.0f/AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential->SetArgT(0,clPotential);
	BinnedAtomicPotential->SetArgT(5,AtomicStructure->AtomicStructureParameterisation);
	BinnedAtomicPotential->SetArgT(7,resolution);
	BinnedAtomicPotential->SetArgT(8,resolution);
	BinnedAtomicPotential->SetArgT(12,AtomicStructure->dz);
	BinnedAtomicPotential->SetArgT(13,pixelscale);
	BinnedAtomicPotential->SetArgT(14,AtomicStructure->xBlocks);
	BinnedAtomicPotential->SetArgT(15,AtomicStructure->yBlocks);
	BinnedAtomicPotential->SetArgT(16,AtomicStructure->MaximumX);
	BinnedAtomicPotential->SetArgT(17,AtomicStructure->MinimumX);
	BinnedAtomicPotential->SetArgT(18,AtomicStructure->MaximumY);
	BinnedAtomicPotential->SetArgT(19,AtomicStructure->MinimumY);
	BinnedAtomicPotential->SetArgT(20,loadblocksx);
	BinnedAtomicPotential->SetArgT(21,loadblocksy);
	BinnedAtomicPotential->SetArgT(22,loadblocksz);
	BinnedAtomicPotential->SetArgT(23,sigma2); // Not sure why i am using sigma 2 and not sigma...
	
	// Also need to generate propagator.
	GeneratePropagator = Kernel( new clKernel(clState::context,clState::cldev,"clGeneratePropagator",clState::clq));
	GeneratePropagator->loadProgSource("GeneratePropagator.cl");
	GeneratePropagator->BuildKernel();

	GeneratePropagator->SetArgT(0,clPropagator);
	GeneratePropagator->SetArgT(1,clXFrequencies);
	GeneratePropagator->SetArgT(2,clYFrequencies);
	GeneratePropagator->SetArgT(3,resolution);
	GeneratePropagator->SetArgT(4,resolution);
	GeneratePropagator->SetArgT(5,AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	GeneratePropagator->SetArgT(6,wavelength);
	GeneratePropagator->SetArgT(7,bandwidthkmax);

	GeneratePropagator->Enqueue(WorkSize);
	
	// And multiplication kernel
	ComplexMultiply = Kernel( new clKernel(clState::context,clState::cldev,"clComplexMultiply",clState::clq));
	ComplexMultiply->loadProgSource("Multiply.cl");
	ComplexMultiply->BuildKernel();

	ComplexMultiply->SetArgT(3,resolution);
	ComplexMultiply->SetArgT(4,resolution);

	// And the imaging kernel
	ImagingKernel = Kernel( new clKernel(imagingKernelSource,clState::context,clState::cldev,"clImagingKernel",clState::clq));
	ImagingKernel->BuildKernelOld();


	clFinish(clState::clq->cmdQueue);
};

void TEMSimulation::InitialiseReSized(int resolution, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D)
{

	this->resolution = resolution;
	this->AtomicStructure = Structure;

	// Get size of input structure
	float RealSizeX = endx-startx;
	float RealSizeY = endy-starty;
	pixelscale = max(RealSizeX,RealSizeY)/(resolution);

	// Work out size of each binned block of atoms
	float BlockScaleX = (AtomicStructure->MaximumX-AtomicStructure->MinimumX)/AtomicStructure->xBlocks; 
	float BlockScaleY = (AtomicStructure->MaximumY-AtomicStructure->MinimumY)/AtomicStructure->yBlocks;

	// Work out area that is to be simulated
	float SimSizeX = pixelscale * resolution;
	float SimSizeY = SimSizeX;

	float	Pi		= 3.1415926f;	
	float	V		= TEMParams->kilovoltage;
	float	a0		= 52.9177e-012f;
	float	a0a		= a0*1e+010f;
	float	echarge	= 1.6e-019f;
	wavelength		= 6.63e-034f*3e+008f/sqrt((echarge*V*1000*(2*9.11e-031f*9e+016f + echarge*V*1000)))*1e+010f;
	float	sigma	= 2 * Pi * ((511.0f + V) / (2.0f*511.0f + V)) / (V * wavelength);
	float	sigma2	= (2*Pi/(wavelength * V * 1000)) * ((9.11e-031f*9e+016f + echarge*V*1000)/(2*9.11e-031f*9e+016f + echarge*V*1000));
	float	fix		= 300.8242834f/(4*Pi*Pi*a0a*echarge);
	float	V2		= V*1000;

	// Now we can set up frequencies and fourier transforms.

	int imidx = floor(resolution/2 + 0.5);
	int imidy = floor(resolution/2 + 0.5);

	std::vector<float> k0x;
	std::vector<float> k0y;

	float temp;

	for(int i=1 ; i <= resolution ; i++)
	{
		if ((i - 1) > imidx)
			temp = ((i - 1) - resolution)/SimSizeX;
		else temp = (i - 1)/SimSizeX;
		k0x.push_back (temp);
	}

	for(int i=1 ; i <= resolution ; i++)
	{
		if ((i - 1) > imidy)
			temp = ((i - 1) - resolution)/SimSizeY;
		else temp = (i - 1)/SimSizeY;
		k0y.push_back (temp);
	}

	// Find maximum frequency for bandwidth limiting rule....

	 bandwidthkmax=0;

	float	kmaxx = pow((k0x[imidx-1]*1/2),2);
	float	kmaxy = pow((k0y[imidy-1]*1/2),2);
	
	if(kmaxy <= kmaxx)
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
	FourierTrans->Setup(resolution,resolution);

	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction2 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction3 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction4 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));

	clTDSx.resize(resolution*resolution);
	clTDSk.resize(resolution*resolution);

	clImageWaveFunction = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPropagator = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPotential = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));

	// Set initial wavefunction to 1+0i
	Kernel InitialiseWavefunction = Kernel(new clKernel(InitialiseWavefunctionSource, clState::context, clState::cldev, "clInitialiseWavefunction", clState::clq));
	InitialiseWavefunction->BuildKernelOld();

	BandLimit = Kernel(new clKernel(BandLimitSource,clState::context,clState::cldev,"clBandLimit",clState::clq));
	BandLimit->BuildKernelOld();

	fftShift = Kernel( new clKernel(fftShiftSource,clState::context,clState::cldev,"clfftShift",clState::clq));
	fftShift->BuildKernelOld();

	fftShift->SetArgT(0,clWaveFunction2);
	fftShift->SetArgT(1,clWaveFunction3);
	fftShift->SetArgT(2,resolution);
	fftShift->SetArgT(3,resolution);

	float InitialValue = 1.0f;
	InitialiseWavefunction->SetArgT(0,clWaveFunction1);
	InitialiseWavefunction->SetArgT(1,resolution);
	InitialiseWavefunction->SetArgT(2,resolution);
	InitialiseWavefunction->SetArgT(3,InitialValue);

	BandLimit->SetArgT(0,clWaveFunction3);
	BandLimit->SetArgT(1,resolution);
	BandLimit->SetArgT(2,resolution);
	BandLimit->SetArgT(3,bandwidthkmax);
	BandLimit->SetArgT(4,clXFrequencies);
	BandLimit->SetArgT(5,clYFrequencies);

	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	InitialiseWavefunction->Enqueue(WorkSize);

	if (Full3D)
	{
		BinnedAtomicPotential = Kernel( new clKernel(clState::context,clState::cldev,"clBinnedAtomicPotentialOpt",clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialOpt2.cl");
	}
	else
	{
		BinnedAtomicPotential = Kernel( new clKernel(clState::context,clState::cldev,"clBinnedAtomicPotentialConventional",clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialConventional2.cl");		
	}
	BinnedAtomicPotential->BuildKernel();

	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f/BlockScaleX);
	int loadblocksy = ceil(3.0f/BlockScaleY);
	int loadblocksz = ceil(3.0f/AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential->SetArgT(0,clPotential);
	BinnedAtomicPotential->SetArgT(5,AtomicStructure->AtomicStructureParameterisation);
	BinnedAtomicPotential->SetArgT(7,resolution);
	BinnedAtomicPotential->SetArgT(8,resolution);
	BinnedAtomicPotential->SetArgT(12,AtomicStructure->dz);
	BinnedAtomicPotential->SetArgT(13,pixelscale);
	BinnedAtomicPotential->SetArgT(14,AtomicStructure->xBlocks);
	BinnedAtomicPotential->SetArgT(15,AtomicStructure->yBlocks);
	BinnedAtomicPotential->SetArgT(16,AtomicStructure->MaximumX);
	BinnedAtomicPotential->SetArgT(17,AtomicStructure->MinimumX);
	BinnedAtomicPotential->SetArgT(18,AtomicStructure->MaximumY);
	BinnedAtomicPotential->SetArgT(19,AtomicStructure->MinimumY);
	BinnedAtomicPotential->SetArgT(20,loadblocksx);
	BinnedAtomicPotential->SetArgT(21,loadblocksy);
	BinnedAtomicPotential->SetArgT(22,loadblocksz);
	BinnedAtomicPotential->SetArgT(23,sigma2); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential->SetArgT(24,startx); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential->SetArgT(25,starty); // Not sure why i am using sigma 2 and not sigma...
	
	// Also need to generate propagator.
	GeneratePropagator = Kernel( new clKernel(clState::context,clState::cldev,"clGeneratePropagator",clState::clq));
	GeneratePropagator->loadProgSource("GeneratePropagator.cl");
	GeneratePropagator->BuildKernel();

	GeneratePropagator->SetArgT(0,clPropagator);
	GeneratePropagator->SetArgT(1,clXFrequencies);
	GeneratePropagator->SetArgT(2,clYFrequencies);
	GeneratePropagator->SetArgT(3,resolution);
	GeneratePropagator->SetArgT(4,resolution);
	GeneratePropagator->SetArgT(5,AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	GeneratePropagator->SetArgT(6,wavelength);
	GeneratePropagator->SetArgT(7,bandwidthkmax);

	GeneratePropagator->Enqueue(WorkSize);
	
	// And multiplication kernel
	ComplexMultiply = Kernel( new clKernel(clState::context,clState::cldev,"clComplexMultiply",clState::clq));
	ComplexMultiply->loadProgSource("Multiply.cl");
	ComplexMultiply->BuildKernel();

	ComplexMultiply->SetArgT(3,resolution);
	ComplexMultiply->SetArgT(4,resolution);

	// And the imaging kernel
	ImagingKernel = Kernel( new clKernel(imagingKernelSource,clState::context,clState::cldev,"clImagingKernel",clState::clq));
	ImagingKernel->BuildKernelOld();


	clFinish(clState::clq->cmdQueue);
};

void TEMSimulation::InitialiseSTEM(int resolution, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D)
{
	this->resolution = resolution;
	this->AtomicStructure = Structure;

	// Get size of input structure
	float RealSizeX = endx-startx;
	float RealSizeY = endy-starty;
	pixelscale = max(RealSizeX,RealSizeY)/resolution;

	// Work out size of each binned block of atoms
	float BlockScaleX = (AtomicStructure->MaximumX-AtomicStructure->MinimumX)/AtomicStructure->xBlocks; 
	float BlockScaleY = (AtomicStructure->MaximumY-AtomicStructure->MinimumY)/AtomicStructure->yBlocks;

	// Work out area that is to be simulated
	float SimSizeX = pixelscale * resolution;
	float SimSizeY = SimSizeX;

	float	Pi		= 3.1415926f;	
	float	V		= STEMParams->kilovoltage;
	float	a0		= 52.9177e-012f;
	float	a0a		= a0*1e+010f;
	float	echarge	= 1.6e-019f;
	wavelength		= 6.63e-034f*3e+008f/sqrt((echarge*V*1000*(2*9.11e-031f*9e+016f + echarge*V*1000)))*1e+010f;
	float	sigma	= 2 * Pi * ((511.0f + V) / (2.0f*511.0f + V)) / (V * wavelength);
	float	sigma2	= (2*Pi/(wavelength * V * 1000)) * ((9.11e-031f*9e+016f + echarge*V*1000)/(2*9.11e-031f*9e+016f + echarge*V*1000));
	float	fix		= 300.8242834f/(4*Pi*Pi*a0a*echarge);
	float	V2		= V*1000;

	// Now we can set up frequencies and fourier transforms.
	int imidx = floor(resolution/2 + 0.5);
	int imidy = floor(resolution/2 + 0.5);

	std::vector<float> k0x;
	std::vector<float> k0y;

	float temp;

	for(int i=1 ; i <= resolution ; i++)
	{
		if ((i - 1) > imidx)
			temp = ((i - 1) - resolution)/SimSizeX;
		else temp = (i - 1)/SimSizeX;
		k0x.push_back (temp);
	}

	for(int i=1 ; i <= resolution ; i++)
	{
		if ((i - 1) > imidy)
			temp = ((i - 1) - resolution)/SimSizeY;
		else temp = (i - 1)/SimSizeY;
		k0y.push_back (temp);
	}

	// Find maximum frequency for bandwidth limiting rule....

	 bandwidthkmax=0;

	float	kmaxx = pow((k0x[imidx-1]*1/2),2);
	float	kmaxy = pow((k0y[imidy-1]*1/2),2);
	
	if(kmaxy <= kmaxx)
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
	FourierTrans->Setup(resolution,resolution);

	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction2 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction3 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));
	clWaveFunction4 = Buffer(new clMemory(resolution * resolution * sizeof( cl_float2 )));

	clTDSx.resize(resolution*resolution);
	clTDSk.resize(resolution*resolution);

	clImageWaveFunction = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));
	
	clTDSDiff = Buffer( new clMemory(resolution*resolution*sizeof(cl_float)));
	clTDSMaskDiff = Buffer( new clMemory(resolution*resolution*sizeof(cl_float)));
	
	clPropagator = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));
	clPotential = Buffer( new clMemory(resolution*resolution*sizeof(cl_float2)));

	// Set initial wavefunction to 1+0i
	InitialiseSTEMWavefunction = Kernel( new clKernel(InitialiseSTEMWavefunctionSource,clState::context,clState::cldev,"clInitialiseSTEMWavefunction",clState::clq));
	InitialiseSTEMWavefunction->BuildKernelOld();

	BandLimit = Kernel( new clKernel(BandLimitSource,clState::context,clState::cldev,"clBandLimit",clState::clq));
	BandLimit->BuildKernelOld();

	fftShift = Kernel( new clKernel(fftShiftSource,clState::context,clState::cldev,"clfftShift",clState::clq));
	fftShift->BuildKernelOld();

	fftShift->SetArgT(0,clWaveFunction2);
	fftShift->SetArgT(1,clWaveFunction3);
	fftShift->SetArgT(2,resolution);
	fftShift->SetArgT(3,resolution);

	MultiplyCL = Kernel( new clKernel(multiplySource,clState::context,clState::cldev,"clMultiply",clState::clq));
	MultiplyCL->BuildKernelOld();
	
	MaskingKernel = Kernel( new clKernel(bandPassSource,clState::context,clState::cldev,"clBandPass",clState::clq));
	MaskingKernel->BuildKernelOld();

	TDSMaskingKernel = Kernel( new clKernel(floatbandPassSource,clState::context,clState::cldev,"clFloatBandPass",clState::clq));
	TDSMaskingKernel->BuildKernelOld();

	BandLimit->SetArgT(0,clWaveFunction3);
	BandLimit->SetArgT(1,resolution);
	BandLimit->SetArgT(2,resolution);
	BandLimit->SetArgT(3,bandwidthkmax);
	BandLimit->SetArgT(4,clXFrequencies);
	BandLimit->SetArgT(5,clYFrequencies);

	size_t* WorkSize = new size_t[3];
	
	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	WFabsolute = Kernel( new clKernel(abssource2,clState::context,clState::cldev,"clAbs",clState::clq));
	WFabsolute->BuildKernelOld();

	if (Full3D)
	{
		BinnedAtomicPotential = Kernel( new clKernel(clState::context,clState::cldev,"clBinnedAtomicPotentialOpt",clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialOpt2.cl");
	}
	else
	{
		BinnedAtomicPotential = Kernel( new clKernel(clState::context,clState::cldev,"clBinnedAtomicPotentialConventional",clState::clq));
		BinnedAtomicPotential->loadProgSource("BinnedAtomicPotentialConventional2.cl");		
	}
	BinnedAtomicPotential->BuildKernel();
	//BinnedAtomicPotential->BuildKernelOld();

	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f/((AtomicStructure->MaximumX-AtomicStructure->MinimumX)/(AtomicStructure->xBlocks)));
	int loadblocksy = ceil(3.0f/((AtomicStructure->MaximumY-AtomicStructure->MinimumY)/(AtomicStructure->yBlocks)));
	int loadblocksz = ceil(3.0f/AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential->SetArgT(0,clPotential);
	BinnedAtomicPotential->SetArgT(5,AtomicStructure->AtomicStructureParameterisation);
	BinnedAtomicPotential->SetArgT(7,resolution);
	BinnedAtomicPotential->SetArgT(8,resolution);
	BinnedAtomicPotential->SetArgT(12,AtomicStructure->dz);
	BinnedAtomicPotential->SetArgT(13,pixelscale);
	BinnedAtomicPotential->SetArgT(14,AtomicStructure->xBlocks);
	BinnedAtomicPotential->SetArgT(15,AtomicStructure->yBlocks);
	BinnedAtomicPotential->SetArgT(16,AtomicStructure->MaximumX);
	BinnedAtomicPotential->SetArgT(17,AtomicStructure->MinimumX);
	BinnedAtomicPotential->SetArgT(18,AtomicStructure->MaximumY);
	BinnedAtomicPotential->SetArgT(19,AtomicStructure->MinimumY);
	BinnedAtomicPotential->SetArgT(20,loadblocksx);
	BinnedAtomicPotential->SetArgT(21,loadblocksy);
	BinnedAtomicPotential->SetArgT(22,loadblocksz);
	BinnedAtomicPotential->SetArgT(23,sigma2); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential->SetArgT(24,startx);
	BinnedAtomicPotential->SetArgT(25,starty);

	// Also need to generate propagator.
	GeneratePropagator = Kernel( new clKernel(clState::context,clState::cldev,"clGeneratePropagator",clState::clq));
	GeneratePropagator->loadProgSource("GeneratePropagator.cl");
	GeneratePropagator->BuildKernel();

	GeneratePropagator->SetArgT(0,clPropagator);
	GeneratePropagator->SetArgT(1,clXFrequencies);
	GeneratePropagator->SetArgT(2,clYFrequencies);
	GeneratePropagator->SetArgT(3,resolution);
	GeneratePropagator->SetArgT(4,resolution);
	GeneratePropagator->SetArgT(5,AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	GeneratePropagator->SetArgT(6,wavelength);
	GeneratePropagator->SetArgT(7,bandwidthkmax);

	GeneratePropagator->Enqueue(WorkSize);
	
	// And multiplication kernel
	ComplexMultiply = Kernel( new clKernel(clState::context,clState::cldev,"clComplexMultiply",clState::clq));
	ComplexMultiply->loadProgSource("Multiply.cl");
	ComplexMultiply->BuildKernel();

	ComplexMultiply->SetArgT(3,resolution);
	ComplexMultiply->SetArgT(4,resolution);

	// And the imaging kernel
	ImagingKernel = Kernel( new clKernel(imagingKernelSource,clState::context,clState::cldev,"clImagingKernel",clState::clq));
	ImagingKernel->BuildKernelOld();

	clFinish(clState::clq->cmdQueue);
};

void TEMSimulation::MakeSTEMWaveFunction(float posx, float posy)
{
	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	InitialiseSTEMWavefunction->SetArgS(clWaveFunction2, resolution, resolution, clXFrequencies, clYFrequencies, posx, posy, STEMParams->aperturesizemrad, pixelscale, STEMParams->defocus, STEMParams->spherical, wavelength);

	/**InitialiseSTEMWavefunction << clWaveFunction2 && resolution && resolution 
								&& clXFrequencies && clYFrequencies && posx && posy 
								&& STEMParams->aperturesizemrad && pixelscale 
								&& STEMParams->defocus && STEMParams->spherical 
								&& wavelength;*/

	InitialiseSTEMWavefunction->Enqueue(WorkSize);

	// IFFT
	FourierTrans->Enqueue(clWaveFunction2,clWaveFunction1,CLFFT_BACKWARD);

	// so both cl mem things have the wavefunction (gonna edit one in a sec)
	/*clEnqueueCopyBuffer(clState::clq->cmdQueue,clWaveFunction1, clWaveFunction2, 0, 0, resolution*resolution*sizeof(cl_float2), 0, 0, 0);

	*WFabsolute << clWaveFunction2 && resolution && resolution;

	WFabsolute->Enqueue(WorkSize);

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

	float sumRed = SumReduction(clWaveFunction2, globalSizeSum, localSizeSum, nGroups, totalSize);
	float inverseSum =1.0f/sumRed;

	*MultiplyCL << clWaveFunction1 && inverseSum && resolution && resolution;

	MultiplyCL->Enqueue(WorkSize);*/
}


float TEMSimulation::MeasureSTEMPixel(float inner, float outer, float xc, float yc)
{

	// NOTE FOR TDS SHOULD USE THE clTDSk vector and mask this to get results.... (can use TDS everytime its just set to 1 run??).


	// clWaveFunction3 should contain the diffraction pattern, shouldnt be needed elsewhere is STEM mode so should be safe to modify?

	size_t* WorkSize = new size_t[3];

	WorkSize[0] = resolution;
	WorkSize[1] = resolution;
	WorkSize[2] = 1;

	//fftShift->Enqueue(WorkSize); // looking at GetDiffImage, should put into clWaveFunction3 (can set manually)

	float pxFreq = (resolution * pixelscale);

	float innerFreq = inner/(1000 * wavelength);
	float innerPx = innerFreq*pxFreq;

	float outerFreq = outer/(1000 * wavelength);
	float outerPx = outerFreq*pxFreq;

	float xcFreq = xc/(1000 * wavelength);
	float xcPx = xcFreq*pxFreq;

	float ycFreq = yc/(1000 * wavelength);
	float ycPx = ycFreq*pxFreq;

	/**MaskingKernel << clWaveFunction4 && clWaveFunction3 && resolution && resolution && innerPx && outerPx;

	MaskingKernel->Enqueue(WorkSize);*/

	clTDSDiff->Write(clTDSk);
	
	*TDSMaskingKernel << clTDSMaskDiff && clTDSDiff && resolution && resolution && innerPx && outerPx && xcPx && ycPx;

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
}

void TEMSimulation::MultisliceStep(int stepno, int steps)
{
	// Work out current z position based on step size and current step
	// Should be one set of bins for each individual slice
	

	int slice = stepno - 1;
	int slices = steps;

	// Didn't have MinimumZ so it wasnt correctly rescaled z-axis from 0 to SizeZ...
	float currentz = AtomicStructure->MaximumZ - AtomicStructure->MinimumZ - slice * AtomicStructure->dz;
	
	int topz = slice - ceil(3.0f/AtomicStructure->dz);
	int bottomz = slice + ceil(3.0f/AtomicStructure->dz);

	if(topz < 0 )
		topz = 0;
	if(bottomz >= slices )
		bottomz = slices-1;

//	AtomicStructure->UploadConstantBlock(topz,bottomz);
	BinnedAtomicPotential->SetArgT(1,AtomicStructure->clAtomx);
	BinnedAtomicPotential->SetArgT(2,AtomicStructure->clAtomy);
	BinnedAtomicPotential->SetArgT(3,AtomicStructure->clAtomz);
	BinnedAtomicPotential->SetArgT(4,AtomicStructure->clAtomZ);
	BinnedAtomicPotential->SetArgT(6,AtomicStructure->clBlockStartPositions);
	BinnedAtomicPotential->SetArgT(9,slice);
	BinnedAtomicPotential->SetArgT(10,slices);
	BinnedAtomicPotential->SetArgT(11,currentz);

	size_t* Work = new size_t[3];

	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	size_t* LocalWork = new size_t[3];

	LocalWork[0]=16;
	LocalWork[1]=16;
	LocalWork[2]=1;

	BinnedAtomicPotential->Enqueue3D(Work,LocalWork);

	FourierTrans->Enqueue(clPotential,clWaveFunction3,CLFFT_FORWARD);
	BandLimit->Enqueue(Work);
	FourierTrans->Enqueue(clWaveFunction3,clPotential,CLFFT_BACKWARD);
	// Now for the rest of the multislice steps

	//Multiply with wavefunction
	ComplexMultiply->SetArgT(0,clPotential);
	ComplexMultiply->SetArgT(1,clWaveFunction1);
	ComplexMultiply->SetArgT(2,clWaveFunction2);
	ComplexMultiply->Enqueue(Work);

	// Propagate
	FourierTrans->Enqueue(clWaveFunction2,clWaveFunction3,CLFFT_FORWARD);

	// BandLimit OK here?
	ComplexMultiply->SetArgT(0,clWaveFunction3);
	ComplexMultiply->SetArgT(1,clPropagator);
	ComplexMultiply->SetArgT(2,clWaveFunction2);
	ComplexMultiply->Enqueue(Work);

		
	FourierTrans->Enqueue(clWaveFunction2,clWaveFunction1,CLFFT_BACKWARD);

	clFinish(clState::clq->cmdQueue);
	
};

void TEMSimulation::GetCTEMImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] = sqrt(compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
	
		// Find max,min for contrast limits
		if(data[i] > max)
			max = data[i];
		if(data[i] < min)
			min = data[i];	
	}

	imagemin = min;
	imagemax = max;
};

void TEMSimulation::GetCTEMImage(float* data, int resolution, float doseperpix, int binning, int detector)
{
	std::vector<float*> ntfs;

	ntfs.push_back(NULL);
	ntfs.push_back(oriusNTF);
	ntfs.push_back(k2NTF);


	Buffer Temp1 = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	Buffer ntfbuffer = Buffer(new clMemory(725*sizeof(cl_float)));

	size_t* Work = new size_t[3];

	Work[0] = resolution;
	Work[1] = resolution;
	Work[2] = 1;

	Kernel NTF = Kernel(new clKernel(NTFSource,clState::context,clState::cldev,"clNTF",clState::clq));
	NTF->BuildKernelOld();

	Kernel ABS =  Kernel(new clKernel(SqAbsSource,clState::context,clState::cldev,"clSqAbs",clState::clq));
	ABS->BuildKernelOld();


	float conversionfactor = 8; //CCD counts per electron.
	float Ntot = doseperpix*binning*binning; // Get this passed in, its dose per pixel i think.

	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1->Read(compdata);

	for(int i = 0; i < resolution * resolution; i++)
	{
		double random = ((double) rand() / (RAND_MAX+1));
		double random2 = ((double) rand() / (RAND_MAX+1));
		double rstdnormal = sqrt(-2.0f * +log(FLT_MIN+random))*(sin(2.0f * CL_M_PI * random2));

		float val = sqrt(compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
		// Get absolute value for display...	
		compdata[i].s[0] = floor(Ntot * val + sqrt(fabs(Ntot*val))*rstdnormal); // Was round not floor
		compdata[i].s[1] = 0;
	
	}

	clWaveFunction1->Write(compdata);

	FourierTrans->Enqueue(clWaveFunction1,Temp1,CLFFT_FORWARD);

	clEnqueueWriteBuffer(clState::clq->cmdQueue,ntfbuffer->buffer,CL_TRUE,0,725*sizeof(float),ntfs[detector],0,NULL,NULL);

	NTF->SetArgS(Temp1,ntfbuffer,resolution,resolution,binning);
	NTF->Enqueue(Work);

	FourierTrans->Enqueue(Temp1,clWaveFunction1,CLFFT_BACKWARD);


	clWaveFunction1->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{

		float val = sqrt(compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
		// Get absolute value for display...	
		data[i] = val; // Was round not floor
	
		// Find max,min for contrast limits
		if(data[i] > max)
			max = data[i];
		if(data[i] < min)
			min = data[i];	
	}

	imagemin = min;
	imagemax = max;
};

void TEMSimulation::AddTDS()
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		clTDSx[i] += (compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
	
		// Find max,min for contrast limits
		if(clTDSx[i] > max)
			max = clTDSx[i];
		if(clTDSx[i] < min)
			min = clTDSx[i];	
	}

	ewmin = min;
	ewmax = max;

	// Original data is complex so copy complex version down first

	size_t* Work = new size_t[3];

	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	fftShift->Enqueue(Work);

	clWaveFunction3->Read(compdata);

	max = CL_FLT_MIN;
	min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		clTDSk[i] += (compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
	
		// Find max,min for contrast limits
		if(clTDSk[i] > max)
			max = clTDSk[i];
		if(clTDSk[i] < min)
			min = clTDSk[i];	
	}

	tdsmin = min;
	tdsmax = max;
};

void TEMSimulation::ClearTDS()
{
	fill(clTDSk.begin(),clTDSk.end(),0);
	fill(clTDSx.begin(),clTDSx.end(),0);

};


void TEMSimulation::GetEWImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	clWaveFunction1->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] = sqrt(compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
	
		// Find max,min for contrast limits
		if(data[i] > max)
			max = data[i];
		if(data[i] < min)
			min = data[i];	
	}

	ewmin = min;
	ewmax = max;

	
};

void TEMSimulation::AddTDSDiffImage(float* data, int resolution)
{
	// Get Diff pattern and sum up into array...

	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	size_t* Work = new size_t[3];

	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	fftShift->Enqueue(Work);

	clWaveFunction3->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] +=(compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
	
		// Find max,min for contrast limits
		if(data[i] > max)
			max = data[i];
		if(data[i] < min)
			min = data[i];	
	}

	diffmin = min;
	diffmax = max;
};

void TEMSimulation::GetDiffImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	size_t* Work = new size_t[3];

	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	fftShift->Enqueue(Work);

	clWaveFunction3->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] += (compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1]);
	
		// Find max,min for contrast limits
		if(data[i] > max)
			max = data[i];
		if(data[i] < min)
			min = data[i];	
	}

	diffmin = min;
	diffmax = max;
};

void TEMSimulation::GetImDiffImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata;
	compdata.resize(resolution*resolution);

	size_t* Work = new size_t[3];

	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	fftShift->SetArgT(0,clWaveFunction4);
	fftShift->Enqueue(Work);

	// reset! probably unecessary
	fftShift->SetArgT(0,clWaveFunction2);

	clWaveFunction3->Read(compdata);

	float max = CL_FLT_MIN;
	float min = CL_MAXFLOAT;

	for(int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] = log(sqrt(compdata[i].s[0]*compdata[i].s[0] + compdata[i].s[1]*compdata[i].s[1])+0.0001f);
	
		// Find max,min for contrast limits
		if(data[i] > max)
			max = data[i];
		if(data[i] < min)
			min = data[i];	
	}

	diffmin = min;
	diffmax = max;
};

void TEMSimulation::SimulateCTEM()
{
	// Set up some temporary memory objects for the image simulation
	Buffer Temp1 = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	Buffer dqebuffer = Buffer(new clMemory(725*sizeof(cl_float)));

	Kernel DQE = Kernel(new clKernel(DQESource,clState::context,clState::cldev,"clDQE",clState::clq));
	DQE->BuildKernelOld();

	Kernel ABS =  Kernel(new clKernel(SqAbsSource,clState::context,clState::cldev,"clSqAbs",clState::clq));
	ABS->BuildKernelOld();

	// Set arguments for imaging kernel
	ImagingKernel->SetArgT(0,clWaveFunction2);
	ImagingKernel->SetArgT(1,clImageWaveFunction);
	ImagingKernel->SetArgT(2,resolution);
	ImagingKernel->SetArgT(3,resolution);
	ImagingKernel->SetArgT(4,TEMParams->spherical);
	ImagingKernel->SetArgT(5,TEMParams->defocus);
	ImagingKernel->SetArgT(6,TEMParams->astigmag);
	ImagingKernel->SetArgT(7,TEMParams->astigang);
	ImagingKernel->SetArgT(8,TEMParams->astig2mag);
	ImagingKernel->SetArgT(9,TEMParams->astig2ang);
	ImagingKernel->SetArgT(10,TEMParams->aperturesizemrad);
	ImagingKernel->SetArgT(11,wavelength);
	ImagingKernel->SetArgT(12,clXFrequencies);
	ImagingKernel->SetArgT(13,clYFrequencies);
	ImagingKernel->SetArgT(14,TEMParams->beta);
	ImagingKernel->SetArgT(15,TEMParams->delta);


	size_t* Work = new size_t[3];
	
	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	ImagingKernel->Enqueue(Work);


	// Now get and display absolute value
	FourierTrans->Enqueue(clImageWaveFunction,clWaveFunction1,CLFFT_BACKWARD);

	ABS->SetArgS(clWaveFunction1,Temp1,resolution,resolution);
	ABS->Enqueue(Work);

	FourierTrans->Enqueue(Temp1,clImageWaveFunction,CLFFT_FORWARD);
	int binning = 1;
	DQE->SetArgS(clImageWaveFunction,dqebuffer,resolution,resolution,binning);
	DQE->Enqueue(Work);

	FourierTrans->Enqueue(clImageWaveFunction,Temp1,CLFFT_BACKWARD);

	ABS->SetArgS(Temp1,clImageWaveFunction,resolution,resolution);
	ABS->Enqueue(Work);

	// Maybe update diffractogram image also...
	clEnqueueCopyBuffer(clState::clq->cmdQueue,clImageWaveFunction->buffer,clWaveFunction4->buffer,0,0,resolution*resolution*sizeof(cl_float2),0,0,0);

};

void TEMSimulation::SimulateCTEM(int detector, int binning)
{

	std::vector<float*> dqes;

	dqes.push_back(NULL);
	dqes.push_back(oriusDQE);
	dqes.push_back(k2DQE);

	// Set up some temporary memory objects for the image simulation
	Buffer Temp1 = Buffer(new clMemory(resolution*resolution*sizeof(cl_float2)));
	Buffer dqebuffer = Buffer(new clMemory(725*sizeof(cl_float)));

	Kernel DQE = Kernel(new clKernel(DQESource,clState::context,clState::cldev,"clDQE",clState::clq));
	DQE->BuildKernelOld();

	Kernel ABS =  Kernel(new clKernel(SqAbsSource,clState::context,clState::cldev,"clSqAbs",clState::clq));
	ABS->BuildKernelOld();

	// Set arguments for imaging kernel
	ImagingKernel->SetArgT(0,clWaveFunction2);
	ImagingKernel->SetArgT(1,clImageWaveFunction);
	ImagingKernel->SetArgT(2,resolution);
	ImagingKernel->SetArgT(3,resolution);
	ImagingKernel->SetArgT(4,TEMParams->spherical);
	ImagingKernel->SetArgT(5,TEMParams->defocus);
	ImagingKernel->SetArgT(6,TEMParams->astigmag);
	ImagingKernel->SetArgT(7,TEMParams->astigang);
	ImagingKernel->SetArgT(8,TEMParams->astig2mag);
	ImagingKernel->SetArgT(9,TEMParams->astig2ang);
	ImagingKernel->SetArgT(10,TEMParams->aperturesizemrad);
	ImagingKernel->SetArgT(11,wavelength);
	ImagingKernel->SetArgT(12,clXFrequencies);
	ImagingKernel->SetArgT(13,clYFrequencies);
	ImagingKernel->SetArgT(14,TEMParams->beta);
	ImagingKernel->SetArgT(15,TEMParams->delta);

	size_t* Work = new size_t[3];
	
	Work[0]=resolution;
	Work[1]=resolution;
	Work[2]=1;

	ImagingKernel->Enqueue(Work);


	// Now get and display absolute value
	FourierTrans->Enqueue(clImageWaveFunction,clWaveFunction1,CLFFT_BACKWARD);

	ABS->SetArgS(clWaveFunction1,Temp1,resolution,resolution);
	ABS->Enqueue(Work);

	FourierTrans->Enqueue(Temp1,clImageWaveFunction,CLFFT_FORWARD);

	clEnqueueWriteBuffer(clState::clq->cmdQueue,dqebuffer->buffer,CL_TRUE,0,725*sizeof(float),dqes[detector],0,NULL,NULL);
	DQE->SetArgS(clImageWaveFunction,dqebuffer,resolution,resolution,binning);
	DQE->Enqueue(Work);

	FourierTrans->Enqueue(clImageWaveFunction,Temp1,CLFFT_BACKWARD);

	ABS->SetArgS(Temp1,clImageWaveFunction,resolution,resolution);
	ABS->Enqueue(Work);



	// Maybe update diffractogram image also...
	clEnqueueCopyBuffer(clState::clq->cmdQueue,clImageWaveFunction->buffer,clWaveFunction4->buffer,0,0,resolution*resolution*sizeof(cl_float2),0,0,0);

};

float TEMSimulation::SumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize)
{
	Kernel SumReduction = Kernel(new clKernel(sumReductionsource2,clState::context,clState::cldev,"clSumReduction",clState::clq));
	SumReduction->BuildKernelOld();

	clMemory outArray;
	outArray.Create(nGroups*sizeof(cl_float2));

	// Create host array to store reduction results.
	std::vector< std::complex< float > > sums( nGroups );

	SumReduction->SetArgT(0,Array);

	// Only really need to do these 3 once...
	SumReduction->SetArgT(1,outArray);
	SumReduction->SetArgT(2,totalSize);
	SumReduction->SetArgLocalMemory(3,256,clFloat2);

	SumReduction->Enqueue3D(globalSizeSum,localSizeSum);

	// Now copy back 

	clEnqueueReadBuffer( clState::clq->cmdQueue, outArray.buffer, CL_TRUE, 0, nGroups*sizeof(cl_float2), &sums[0], 0, NULL, NULL );

	// Find out which numbers to read back
	float sum = 0;

	for(int i = 0 ; i < nGroups; i++)
	{
		sum += sums[i].real();
	}

	return sum;

}

float TEMSimulation::SumReduction(Buffer &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize)
{
	Kernel SumReduction = Kernel(new clKernel(sumReductionsource2,clState::context,clState::cldev,"clSumReduction",clState::clq));
	SumReduction->BuildKernelOld();

	clMemory outArray;
	outArray.Create(nGroups*sizeof(cl_float2));

	// Create host array to store reduction results.
	std::vector< std::complex< float > > sums( nGroups );

	SumReduction->SetArgT(0,Array);

	// Only really need to do these 3 once...
	SumReduction->SetArgT(1,outArray);
	SumReduction->SetArgT(2,totalSize);
	SumReduction->SetArgLocalMemory(3,256,clFloat2);

	SumReduction->Enqueue3D(globalSizeSum,localSizeSum);

	// Now copy back 

	clEnqueueReadBuffer( clState::clq->cmdQueue, outArray.buffer, CL_TRUE, 0, nGroups*sizeof(cl_float2), &sums[0], 0, NULL, NULL );

	// Find out which numbers to read back
	float sum = 0;

	for(int i = 0 ; i < nGroups; i++)
	{
		sum += sums[i].real();
	}

	return sum;

}

float TEMSimulation::FloatSumReduction(cl_mem &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize)
{
	Kernel SumReduction = Kernel(new clKernel(floatSumReductionsource2,clState::context,clState::cldev,"clFloatSumReduction",clState::clq));
	SumReduction->BuildKernelOld();

	clMemory outArray;
	outArray.Create(nGroups*sizeof(cl_float));

	// Create host array to store reduction results.
	std::vector< float> sums( nGroups );

	SumReduction->SetArgT(0,Array);

	// Only really need to do these 3 once...
	SumReduction->SetArgT(1,outArray);
	SumReduction->SetArgT(2,totalSize);
	SumReduction->SetArgLocalMemory(3,256,clFloat);

	SumReduction->Enqueue3D(globalSizeSum,localSizeSum);

	// Now copy back 
	clEnqueueReadBuffer( clState::clq->cmdQueue, outArray.buffer, CL_TRUE, 0, nGroups*sizeof(cl_float), &sums[0], 0, NULL, NULL );

	// Find out which numbers to read back
	float sum = 0;

	for(int i = 0 ; i < nGroups; i++)
	{
		sum += sums[i];
	}

	return sum;

}

float TEMSimulation::FloatSumReduction(Buffer &Array, size_t* globalSizeSum, size_t* localSizeSum, int nGroups, int totalSize)
{
	Kernel SumReduction = Kernel(new clKernel(floatSumReductionsource2,clState::context,clState::cldev,"clFloatSumReduction",clState::clq));
	SumReduction->BuildKernelOld();

	clMemory outArray;
	outArray.Create(nGroups*sizeof(cl_float));

	// Create host array to store reduction results.
	std::vector< float> sums( nGroups );

	SumReduction->SetArgT(0,Array);

	// Only really need to do these 3 once...
	SumReduction->SetArgT(1,outArray);
	SumReduction->SetArgT(2,totalSize);
	SumReduction->SetArgLocalMemory(3,256,clFloat);

	SumReduction->Enqueue3D(globalSizeSum,localSizeSum);

	// Now copy back 
	clEnqueueReadBuffer( clState::clq->cmdQueue, outArray.buffer, CL_TRUE, 0, nGroups*sizeof(cl_float), &sums[0], 0, NULL, NULL );

	// Find out which numbers to read back
	float sum = 0;

	for(int i = 0 ; i < nGroups; i++)
	{
		sum += sums[i];
	}

	return sum;

}

TEMSimulation::~TEMSimulation()
{
}