#include "TEMSimulation.h"
#include "clKernelCodes2.h"
#include <minmax.h>
#include <complex>
#include "mtf.h"
#include "UnmanagedOpenCL.h"

//#include <boost/lexical_cast.hpp>
#include <Windows.h>

#include <time.h>
#include <numeric>

TEMSimulation::TEMSimulation(TEMParameters* temparams, STEMParameters* stemparams): FourierTrans(UnmanagedOpenCL::ctx,1024,1024)
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
	float	V = TEMParams->Voltage;
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

	clXFrequencies = UnmanagedOpenCL::ctx.CreateBuffer<float,Manual>(resolution);
	clYFrequencies = UnmanagedOpenCL::ctx.CreateBuffer<float,Manual>(resolution);

	clXFrequencies->Write(k0x);
	clYFrequencies->Write(k0y);

	// Setup Fourier Transforms
	FourierTrans = clFourier(UnmanagedOpenCL::ctx,resolution,resolution);

	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
	clWaveFunction2.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
	clWaveFunction3.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
	clWaveFunction4.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));

	// might not need to be vectors, only if stem needs them
	if (FD)
	{
		clWaveFunction1Minus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
		clWaveFunction1Plus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
	}

	//clTDSk.resize(1);
	//clTDSx.resize(1);

	clTDSk.resize(resolution*resolution);
	//clTDSk[0].resize(resolution*resolution);

	clImageWaveFunction = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);
	clPropagator = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);
	clPotential = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);

	// Set initial wavefunction to 1+0i
	clKernel InitialiseWavefunction(UnmanagedOpenCL::ctx,InitialiseWavefunctionSource.c_str(),4, "clInitialiseWavefunction");
	
	SumReduction = clKernel(UnmanagedOpenCL::ctx,floatSumReductionsource2,4, "clFloatSumReduction");

	BandLimit = clKernel(UnmanagedOpenCL::ctx,BandLimitSource, 6, "clBandLimit");

	fftShift = clKernel(UnmanagedOpenCL::ctx,fftShiftSource,4, "clfftShift");

	fftShift.SetArg(0,clWaveFunction2[0],ArgumentType::Input);
	fftShift.SetArg(1,clWaveFunction3[0]),ArgumentType::Output;
	fftShift.SetArg(2,resolution);
	fftShift.SetArg(3,resolution);

	float InitialValue = 1.0f;
	InitialiseWavefunction.SetArg(0,clWaveFunction1[0],ArgumentType::Output);
	InitialiseWavefunction.SetArg(1,resolution);
	InitialiseWavefunction.SetArg(2,resolution);
	InitialiseWavefunction.SetArg(3,InitialValue);

	BandLimit.SetArg(0,clWaveFunction3[0],ArgumentType::InputOutput);
	BandLimit.SetArg(1,resolution);
	BandLimit.SetArg(2,resolution);
	BandLimit.SetArg(3,bandwidthkmax);
	BandLimit.SetArg(4,clXFrequencies,ArgumentType::Input);
	BandLimit.SetArg(5,clYFrequencies,ArgumentType::Input);

	clWorkGroup WorkSize(resolution,resolution,1);

	if (FD)
	{
		InitialiseWavefunction.SetArg(0, clWaveFunction1Minus[0],ArgumentType::Output);
	}

	InitialiseWavefunction(WorkSize);

	if (Full3D)
	{
		BinnedAtomicPotential = clKernel(UnmanagedOpenCL::ctx,opt2source,27, "clBinnedAtomicPotentialOpt");
	}
	else if (FD)
	{
		BinnedAtomicPotential = clKernel(UnmanagedOpenCL::ctx,fd2source,26, "clBinnedAtomicPotentialOptFD");
	}
	else
	{
		BinnedAtomicPotential = clKernel(UnmanagedOpenCL::ctx,conv2source,26, "clBinnedAtomicPotentialConventional");
	}


	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f / BlockScaleX);
	int loadblocksy = ceil(3.0f / BlockScaleY);
	int loadblocksz = ceil(3.0f / AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential.SetArg(0, clPotential,ArgumentType::Output);
	BinnedAtomicPotential.SetArg(5, AtomicStructure->AtomicStructureParameterisation,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(7, resolution);
	BinnedAtomicPotential.SetArg(8, resolution);
	BinnedAtomicPotential.SetArg(12, AtomicStructure->dz);
	BinnedAtomicPotential.SetArg(13, pixelscale);
	BinnedAtomicPotential.SetArg(14, AtomicStructure->xBlocks);
	BinnedAtomicPotential.SetArg(15, AtomicStructure->yBlocks);
	BinnedAtomicPotential.SetArg(16, AtomicStructure->MaximumX);
	BinnedAtomicPotential.SetArg(17, AtomicStructure->MinimumX);
	BinnedAtomicPotential.SetArg(18, AtomicStructure->MaximumY);
	BinnedAtomicPotential.SetArg(19, AtomicStructure->MinimumY);
	BinnedAtomicPotential.SetArg(20, loadblocksx);
	BinnedAtomicPotential.SetArg(21, loadblocksy);
	BinnedAtomicPotential.SetArg(22, loadblocksz);
	BinnedAtomicPotential.SetArg(23, sigma2); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential.SetArg(24, startx); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential.SetArg(25, starty); // Not sure why i am using sigma 2 and not sigma...

	if (Full3D)
		BinnedAtomicPotential.SetArg(26, full3dints);

	// Also need to generate propagator.
	GeneratePropagator = clKernel(UnmanagedOpenCL::ctx,propsource,8, "clGeneratePropagator");

	GeneratePropagator.SetArg(0, clPropagator,ArgumentType::Output);
	GeneratePropagator.SetArg(1, clXFrequencies,ArgumentType::Input);
	GeneratePropagator.SetArg(2, clYFrequencies,ArgumentType::Input);
	GeneratePropagator.SetArg(3, resolution);
	GeneratePropagator.SetArg(4, resolution);

	if (FD)
	{
		GeneratePropagator.SetArg(5, FDdz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	}
	else
	{
		GeneratePropagator.SetArg(5, AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	}


	GeneratePropagator.SetArg(6, wavelength);
	GeneratePropagator.SetArg(7, bandwidthkmax);

	GeneratePropagator(WorkSize);

	// And multiplication kernel
	ComplexMultiply = clKernel(UnmanagedOpenCL::ctx,multisource,5 ,"clComplexMultiply");

	ComplexMultiply.SetArg(3, resolution);
	ComplexMultiply.SetArg(4, resolution);

	// And the imaging kernel
	ImagingKernel = clKernel(UnmanagedOpenCL::ctx, imagingKernelSource.c_str(), 24, "clImagingKernel");

	int waves = 1;
	ewmin.resize(waves);
	ewmax.resize(waves);
	ewmin2.resize(waves);
	ewmax2.resize(waves);
	diffmin.resize(waves);
	diffmax.resize(waves);
	//tdsmin.resize(waves);
	//tdsmax.resize(waves);

	if (FD)
	{
		// Need Grad Kernel and FiniteDifference also
		GradKernel = clKernel(UnmanagedOpenCL::ctx,gradsource,5, "clGrad");
		FiniteDifference = clKernel(UnmanagedOpenCL::ctx,fdsource,10, "clFiniteDifference");
		InitialiseWavefunction.SetArg(0, clWaveFunction1[0],ArgumentType::Output);
		InitialiseWavefunction(WorkSize);
	}

	UnmanagedOpenCL::ctx.WaitForQueueFinish();
};

void TEMSimulation::initialiseSTEMSimulation(int res, MultisliceStructure* Structure, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves)
{
	FDMode = FD;

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
	float	V = STEMParams->Voltage;
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

	clXFrequencies = UnmanagedOpenCL::ctx.CreateBuffer<float,Manual>(resolution);
	clYFrequencies = UnmanagedOpenCL::ctx.CreateBuffer<float,Manual>(resolution);

	clXFrequencies->Write(k0x);
	clYFrequencies->Write(k0y);

	// Setup Fourier Transforms
	FourierTrans = clFourier(UnmanagedOpenCL::ctx,resolution,resolution);

	//clTDSk.resize(waves);
	//clTDSx.resize(waves
	clTDSk.resize(resolution*resolution);

	clTDSDiff = UnmanagedOpenCL::ctx.CreateBuffer<cl_float, Manual>(resolution*resolution);

	// Initialise Wavefunctions and Create other buffers...
	for (int i = 1; i <= waves; i++)
	{
		clWaveFunction1.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
		clWaveFunction2.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
		clWaveFunction4.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));

		//clTDSx[i - 1].resize(resolution*resolution);
		//clTDSk[i - 1].resize(resolution*resolution);

		if (FD)
		{
			clWaveFunction1Minus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
			clWaveFunction1Plus.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));
		}
	}

	clWaveFunction3.push_back(UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution));

	clImageWaveFunction = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);

	clTDSMaskDiff = UnmanagedOpenCL::ctx.CreateBuffer<cl_float,Manual>(resolution*resolution);

	clPropagator = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);
	clPotential = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);

	// Set initial wavefunction to 1+0i
	InitialiseSTEMWavefunction = clKernel(UnmanagedOpenCL::ctx,InitialiseSTEMWavefunctionSourceTest.c_str(), 24, "clInitialiseSTEMWavefunction");

	SumReduction = clKernel(UnmanagedOpenCL::ctx,floatSumReductionsource2, 4, "clFloatSumReduction");

	BandLimit = clKernel(UnmanagedOpenCL::ctx,BandLimitSource, 6, "clBandLimit");

	fftShift = clKernel(UnmanagedOpenCL::ctx,fftShiftSource, 4, "clfftShift");

	fftShift.SetArg(0, clWaveFunction2[0]);
	fftShift.SetArg(1, clWaveFunction3[0]);
	fftShift.SetArg(2, resolution);
	fftShift.SetArg(3, resolution);

	MultiplyCL = clKernel(UnmanagedOpenCL::ctx,multiplySource,4 , "clMultiply");
	MaskingKernel = clKernel(UnmanagedOpenCL::ctx,bandPassSource, 6, "clBandPass");
	TDSMaskingKernel = clKernel(UnmanagedOpenCL::ctx,floatbandPassSource,8, "clFloatBandPass");
	TDSMaskingAbsKernel = clKernel(UnmanagedOpenCL::ctx, floatabsbandPassSource, 8, "clFloatAbsBandPass");


	BandLimit.SetArg(0, clWaveFunction3[0]);
	BandLimit.SetArg(1, resolution);
	BandLimit.SetArg(2, resolution);
	BandLimit.SetArg(3, bandwidthkmax);
	BandLimit.SetArg(4, clXFrequencies);
	BandLimit.SetArg(5, clYFrequencies);

	clWorkGroup WorkSize(resolution,resolution,1);

	WFabsolute = clKernel(UnmanagedOpenCL::ctx,abssource2,3, "clAbs");

	if (Full3D)
	{
		BinnedAtomicPotential = clKernel(UnmanagedOpenCL::ctx,opt2source,26, "clBinnedAtomicPotentialOpt");
	}
	else if (FD)
	{
		BinnedAtomicPotential = clKernel(UnmanagedOpenCL::ctx,fd2source,26, "clBinnedAtomicPotentialOptFD");
	}
	else
	{
		BinnedAtomicPotential = clKernel(UnmanagedOpenCL::ctx,conv2source,26, "clBinnedAtomicPotentialConventional");
	}


	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f / ((AtomicStructure->MaximumX - AtomicStructure->MinimumX) / (AtomicStructure->xBlocks)));
	int loadblocksy = ceil(3.0f / ((AtomicStructure->MaximumY - AtomicStructure->MinimumY) / (AtomicStructure->yBlocks)));
	int loadblocksz = ceil(3.0f / AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential.SetArg(0, clPotential);
	BinnedAtomicPotential.SetArg(5, AtomicStructure->AtomicStructureParameterisation);
	BinnedAtomicPotential.SetArg(7, resolution);
	BinnedAtomicPotential.SetArg(8, resolution);
	BinnedAtomicPotential.SetArg(12, AtomicStructure->dz);
	BinnedAtomicPotential.SetArg(13, pixelscale);
	BinnedAtomicPotential.SetArg(14, AtomicStructure->xBlocks);
	BinnedAtomicPotential.SetArg(15, AtomicStructure->yBlocks);
	BinnedAtomicPotential.SetArg(16, AtomicStructure->MaximumX);
	BinnedAtomicPotential.SetArg(17, AtomicStructure->MinimumX);
	BinnedAtomicPotential.SetArg(18, AtomicStructure->MaximumY);
	BinnedAtomicPotential.SetArg(19, AtomicStructure->MinimumY);
	BinnedAtomicPotential.SetArg(20, loadblocksx);
	BinnedAtomicPotential.SetArg(21, loadblocksy);
	BinnedAtomicPotential.SetArg(22, loadblocksz);
	BinnedAtomicPotential.SetArg(23, sigma2); // Not sure why i am using sigma 2 and not sigma...
	BinnedAtomicPotential.SetArg(24, startx);
	BinnedAtomicPotential.SetArg(25, starty);

	if (Full3D)
		BinnedAtomicPotential.SetArg(26, full3dints);

	// Also need to generate propagator.
	GeneratePropagator = clKernel(UnmanagedOpenCL::ctx,propsource,8, "clGeneratePropagator");

	GeneratePropagator.SetArg(0, clPropagator);
	GeneratePropagator.SetArg(1, clXFrequencies);
	GeneratePropagator.SetArg(2, clYFrequencies);
	GeneratePropagator.SetArg(3, resolution);
	GeneratePropagator.SetArg(4, resolution);
	
	if (FD)
	{
		GeneratePropagator.SetArg(5, FDdz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	}
	else
	{
		GeneratePropagator.SetArg(5, AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	}
	GeneratePropagator.SetArg(6, wavelength);
	GeneratePropagator.SetArg(7, bandwidthkmax);

	GeneratePropagator(WorkSize);

	// And multiplication kernel
	ComplexMultiply = clKernel(UnmanagedOpenCL::ctx,multisource,5 ,"clComplexMultiply");

	ComplexMultiply.SetArg(3, resolution);
	ComplexMultiply.SetArg(4, resolution);

	// And the imaging kernel
	ImagingKernel = clKernel(UnmanagedOpenCL::ctx,imagingKernelSource.c_str(),16, "clImagingKernel");

	ewmin.resize(waves);
	ewmax.resize(waves);
	diffmin.resize(waves);
	diffmax.resize(waves);
	//tdsmin.resize(waves);
	//tdsmax.resize(waves);
	
	if (FD)
	{
		// Need Grad Kernel and FiniteDifference also
		GradKernel = clKernel(UnmanagedOpenCL::ctx,gradsource,5, "clGrad");

		FiniteDifference = clKernel(UnmanagedOpenCL::ctx,fdsource,10, "clFiniteDifference");

	}

	UnmanagedOpenCL::ctx.WaitForQueueFinish();

};

void TEMSimulation::initialiseSTEMWaveFunction(float posx, float posy, int wave)
{
	clWorkGroup WorkSize(resolution,resolution,1);

	// Fix inverted images
	posx = resolution - 1 - posx;
	posy = resolution - 1 - posy;

	InitialiseSTEMWavefunction.SetArg(0, clWaveFunction2[wave - 1]);
	InitialiseSTEMWavefunction.SetArg(1, resolution);
	InitialiseSTEMWavefunction.SetArg(2, resolution);
	InitialiseSTEMWavefunction.SetArg(3, clXFrequencies);
	InitialiseSTEMWavefunction.SetArg(4, clYFrequencies);
	InitialiseSTEMWavefunction.SetArg(5, posx);
	InitialiseSTEMWavefunction.SetArg(6, posy);
	InitialiseSTEMWavefunction.SetArg(7, pixelscale);
	InitialiseSTEMWavefunction.SetArg(8, wavelength);
	InitialiseSTEMWavefunction.SetArg(9, STEMParams->C10);
	InitialiseSTEMWavefunction.SetArg(10, STEMParams->C12);
	InitialiseSTEMWavefunction.SetArg(11, STEMParams->C21);
	InitialiseSTEMWavefunction.SetArg(12, STEMParams->C23);
	InitialiseSTEMWavefunction.SetArg(13, STEMParams->C30);
	InitialiseSTEMWavefunction.SetArg(14, STEMParams->C32);
	InitialiseSTEMWavefunction.SetArg(15, STEMParams->C34);
	InitialiseSTEMWavefunction.SetArg(16, STEMParams->C41);
	InitialiseSTEMWavefunction.SetArg(17, STEMParams->C43);
	InitialiseSTEMWavefunction.SetArg(18, STEMParams->C45);
	InitialiseSTEMWavefunction.SetArg(19, STEMParams->C50);
	InitialiseSTEMWavefunction.SetArg(20, STEMParams->C52);
	InitialiseSTEMWavefunction.SetArg(21, STEMParams->C54);
	InitialiseSTEMWavefunction.SetArg(22, STEMParams->C56);
	InitialiseSTEMWavefunction.SetArg(23, STEMParams->Aperture);

	InitialiseSTEMWavefunction(WorkSize);

	// IFFT
	FourierTrans(clWaveFunction2[wave - 1], clWaveFunction1[wave - 1], Direction::Inverse);

	if(FDMode)
	{
		// Copy into both initialwavefunctions
		clEnqueueCopyBuffer(UnmanagedOpenCL::ctx.GetIOQueue(),clWaveFunction1[wave-1]->GetBuffer(),clWaveFunction1Minus[wave-1]->GetBuffer(),0,0,resolution*resolution*sizeof(cl_float2),0,0,0);
	}
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

	BinnedAtomicPotential.SetArg(1, AtomicStructure->clAtomx,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(2, AtomicStructure->clAtomy,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(3, AtomicStructure->clAtomz,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(4, AtomicStructure->clAtomZ,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(6, AtomicStructure->clBlockStartPositions,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(9, slice);
	BinnedAtomicPotential.SetArg(10, slices);
	BinnedAtomicPotential.SetArg(11, currentz);

	clWorkGroup Work(resolution,resolution,1);

	clWorkGroup LocalWork(16,16,1);

	BinnedAtomicPotential(Work, LocalWork);

	FourierTrans(clPotential, clWaveFunction3[0], Direction::Forwards);
	BandLimit(Work);
	FourierTrans(clWaveFunction3[0], clPotential, Direction::Inverse);

	// Now for the rest of the multislice steps
	for (int i = 1; i <= waves; i++)
	{
		//Multiply with wavefunction
		ComplexMultiply.SetArg(0, clPotential,ArgumentType::Input);
		ComplexMultiply.SetArg(1, clWaveFunction1[i - 1],ArgumentType::Input);
		ComplexMultiply.SetArg(2, clWaveFunction2[i - 1],ArgumentType::Output);
		ComplexMultiply(Work);

		// Propagate
		FourierTrans(clWaveFunction2[i - 1], clWaveFunction3[0], Direction::Forwards);

		// BandLimit OK here?
		ComplexMultiply.SetArg(0, clWaveFunction3[0] , ArgumentType::Input);
		ComplexMultiply.SetArg(1, clPropagator , ArgumentType::Input);
		ComplexMultiply.SetArg(2, clWaveFunction2[i - 1] , ArgumentType::Output);
		ComplexMultiply(Work);


		FourierTrans(clWaveFunction2[i - 1], clWaveFunction1[i - 1], Direction::Inverse);
	}
	UnmanagedOpenCL::ctx.WaitForQueueFinish();
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

	BinnedAtomicPotential.SetArg(1, AtomicStructure->clAtomx,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(2, AtomicStructure->clAtomy,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(3, AtomicStructure->clAtomz,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(4, AtomicStructure->clAtomZ,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(6, AtomicStructure->clBlockStartPositions,ArgumentType::Input);
	BinnedAtomicPotential.SetArg(9, atomslice);
	BinnedAtomicPotential.SetArg(10, slices);
	BinnedAtomicPotential.SetArg(11, currentz);

	clWorkGroup Work(resolution,resolution,1);

	clWorkGroup LocalWork(16,16,1);

	BinnedAtomicPotential(Work, LocalWork);

	FourierTrans(clPotential, clWaveFunction3[0], Direction::Forwards);
	BandLimit(Work);
	FourierTrans(clWaveFunction3[0], clPotential, Direction::Inverse);

	// Now for the rest of the multislice steps
	for (int i = 1; i <= waves; i++)
	{

	// //FT Psi into Grad2.
		FourierTrans(clWaveFunction1[i-1], clWaveFunction3[0], Direction::Forwards);

	// //Grad Kernel on Grad2.
		GradKernel.SetArg(0,clWaveFunction3[0],ArgumentType::Input);
		GradKernel.SetArg(1,clXFrequencies,ArgumentType::Input);
		GradKernel.SetArg(2,clYFrequencies,ArgumentType::Input);
		GradKernel.SetArg(3,resolution);
		GradKernel.SetArg(4,resolution);
		GradKernel(Work);

	// //IFT Grad2 into Grad.
		FourierTrans(clWaveFunction3[0], clWaveFunction4[i-1], Direction::Inverse);

	// //FD Kernel
		FiniteDifference.SetArg(0,clPotential,ArgumentType::Input);
		FiniteDifference.SetArg(1,clWaveFunction4[i-1],ArgumentType::Input);
		FiniteDifference.SetArg(2,clWaveFunction1Minus[i-1],ArgumentType::Input);
		FiniteDifference.SetArg(3,clWaveFunction1[i-1],ArgumentType::Input);
		FiniteDifference.SetArg(4,clWaveFunction1Plus[i-1],ArgumentType::Output);
		FiniteDifference.SetArg(5,FDdz);
		FiniteDifference.SetArg(6,wavelength);
		FiniteDifference.SetArg(7,FDsigma);
		FiniteDifference.SetArg(8,resolution);
		FiniteDifference.SetArg(9,resolution);
		FiniteDifference(Work);


	// //Bandlimit PsiPlus
		FourierTrans(clWaveFunction1Plus[i-1], clWaveFunction3[0], Direction::Forwards);
		BandLimit(Work);
		FourierTrans(clWaveFunction3[0], clWaveFunction1Plus[i-1], Direction::Inverse);

	// // Psi becomes PsiMinus
		clEnqueueCopyBuffer(UnmanagedOpenCL::ctx.GetIOQueue(), clWaveFunction1[i-1]->GetBuffer(), clWaveFunction1Minus[i-1]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, nullptr, nullptr);

	// // PsiPlus becomes Psi.
		clEnqueueCopyBuffer(UnmanagedOpenCL::ctx.GetIOQueue(), clWaveFunction1Plus[i-1]->GetBuffer(), clWaveFunction1[i-1]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, nullptr, nullptr);



	// // To maintain status with other versions resulting end arrays should still be as follows.
	// // Finished wavefunction in real spaaaaaace in clWaveFunction1.
	// // Finished wavefunction in reciprocal spaaaaaace in clWaveFunction2.
	// // 3 and 4 were previously temporary.

		FourierTrans(clWaveFunction1[i-1], clWaveFunction2[i-1],Direction::Forwards);

	}

	UnmanagedOpenCL::ctx.WaitForQueueFinish();
};

float TEMSimulation::getSTEMPixel(float inner, float outer, float xc, float yc, int wave)
{
	clWorkGroup WorkSize(resolution, resolution, 1);

	fftShift.SetArg(0, clWaveFunction2[wave - 1], ArgumentType::Input);
	fftShift(WorkSize);

	float pxFreq = (resolution * pixelscale);

	float innerFreq = inner / (1000 * wavelength);
	float innerPx = innerFreq*pxFreq;

	float outerFreq = outer / (1000 * wavelength);
	float outerPx = outerFreq*pxFreq;

	float xcFreq = xc / (1000 * wavelength);
	float xcPx = xcFreq*pxFreq;

	float ycFreq = yc / (1000 * wavelength);
	float ycPx = ycFreq*pxFreq;

	TDSMaskingAbsKernel.SetArg(0, clTDSMaskDiff, ArgumentType::Output);
	TDSMaskingAbsKernel.SetArg(1, clWaveFunction3[0], ArgumentType::Input);
	TDSMaskingAbsKernel.SetArg(2, resolution);
	TDSMaskingAbsKernel.SetArg(3, resolution);
	TDSMaskingAbsKernel.SetArg(4, innerPx);
	TDSMaskingAbsKernel.SetArg(5, outerPx);
	TDSMaskingAbsKernel.SetArg(6, xcPx);
	TDSMaskingAbsKernel.SetArg(7, ycPx);

	TDSMaskingAbsKernel(WorkSize);

	int totalSize = resolution*resolution;
	int nGroups = totalSize / 256;

	clWorkGroup globalSizeSum(totalSize,1,1);
	clWorkGroup localSizeSum(256,1,1);

	return FloatSumReduction(clTDSMaskDiff, globalSizeSum, localSizeSum, nGroups, totalSize);
};

void TEMSimulation::getCTEMImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata = clWaveFunction4[0]->CreateLocalCopy();

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

	auto Temp1 = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);
	auto ntfbuffer = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(725);

	clWorkGroup Work(resolution,resolution,1);

	clKernel NTF = clKernel(UnmanagedOpenCL::ctx,NTFSource,5, "clNTF");
	clKernel ABS = clKernel(UnmanagedOpenCL::ctx,SqAbsSource, 4, "clSqAbs");

	float conversionfactor = 8; //CCD counts per electron.
	float Ntot = doseperpix*binning*binning; // Get this passed in, its dose per pixel i think.

	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata = clWaveFunction4[0]->CreateLocalCopy();

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

	FourierTrans(clWaveFunction1[0], Temp1, Direction::Forwards);

	clEnqueueWriteBuffer(UnmanagedOpenCL::ctx.GetIOQueue(), ntfbuffer->GetBuffer(), CL_TRUE, 0, 725 * sizeof(float), ntfs[detector], 0, NULL, NULL);

	NTF.SetArg(0,Temp1,ArgumentType::InputOutput);
	NTF.SetArg(1,ntfbuffer,ArgumentType::Input);
	NTF.SetArg(2,resolution);
	NTF.SetArg(3,resolution);
	NTF.SetArg(4,binning);
	NTF(Work);

	FourierTrans(Temp1, clWaveFunction1[0], Direction::Inverse);

	compdata = clWaveFunction1[0]->CreateLocalCopy();

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
	auto Temp1 = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);
	auto dqebuffer = UnmanagedOpenCL::ctx.CreateBuffer<cl_float,Manual>(725);

	clKernel DQE = clKernel(UnmanagedOpenCL::ctx,DQESource, 5, "clDQE");
	clKernel ABS = clKernel(UnmanagedOpenCL::ctx,SqAbsSource, 4, "clSqAbs");

	// Set arguments for imaging kernel
	ImagingKernel.SetArg(0, clWaveFunction2[0], ArgumentType::Input);
	ImagingKernel.SetArg(1, clImageWaveFunction, ArgumentType::Output);
	ImagingKernel.SetArg(2, resolution);
	ImagingKernel.SetArg(3, resolution);
	ImagingKernel.SetArg(4, clXFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(5, clYFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(6, wavelength);
	ImagingKernel.SetArg(7, TEMParams->C10);
	ImagingKernel.SetArg(8, TEMParams->C12);
	ImagingKernel.SetArg(9, TEMParams->C21);
	ImagingKernel.SetArg(10, TEMParams->C23);
	ImagingKernel.SetArg(11, TEMParams->C30);
	ImagingKernel.SetArg(12, TEMParams->C32);
	ImagingKernel.SetArg(13, TEMParams->C34);
	ImagingKernel.SetArg(14, TEMParams->C41);
	ImagingKernel.SetArg(15, TEMParams->C43);
	ImagingKernel.SetArg(16, TEMParams->C45);
	ImagingKernel.SetArg(17, TEMParams->C50);
	ImagingKernel.SetArg(18, TEMParams->C52);
	ImagingKernel.SetArg(19, TEMParams->C54);
	ImagingKernel.SetArg(20, TEMParams->C56);
	ImagingKernel.SetArg(21, TEMParams->Aperture);
	ImagingKernel.SetArg(22, TEMParams->Beta);
	ImagingKernel.SetArg(23, TEMParams->Delta);

	clWorkGroup Work(resolution,resolution,1);

	ImagingKernel(Work);

	// Now get and display absolute value
	FourierTrans(clImageWaveFunction, clWaveFunction1[0], Direction::Inverse);

	ABS.SetArg(0,clWaveFunction1[0],ArgumentType::Input);
	ABS.SetArg(1,Temp1,ArgumentType::Output);
	ABS.SetArg(2,resolution);
	ABS.SetArg(3,resolution);
	ABS(Work);

	FourierTrans(Temp1, clImageWaveFunction, Direction::Forwards);
	int binning = 1;
	DQE.SetArg(0,clImageWaveFunction,ArgumentType::InputOutput);
	DQE.SetArg(1,dqebuffer,ArgumentType::Input);
	DQE.SetArg(2,resolution);
	DQE.SetArg(3,resolution);
	DQE.SetArg(4,binning);
	DQE(Work);

	FourierTrans(clImageWaveFunction, Temp1, Direction::Inverse);

	ABS.SetArg(0,Temp1,ArgumentType::Input);
	ABS.SetArg(1,clImageWaveFunction,ArgumentType::Output);
	ABS.SetArg(2,resolution);
	ABS.SetArg(3,resolution);
	ABS(Work);

	// Maybe update diffractogram image also...
	clEnqueueCopyBuffer(UnmanagedOpenCL::ctx.GetIOQueue(), clImageWaveFunction->GetBuffer(), clWaveFunction4[0]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, 0, 0);
};

void TEMSimulation::simulateCTEM(int detector, int binning)
{
	std::vector<float*> dqes;

	dqes.push_back(NULL);
	dqes.push_back(oriusDQE);
	dqes.push_back(k2DQE);

	// Set up some temporary memory objects for the image simulation
	auto Temp1 = UnmanagedOpenCL::ctx.CreateBuffer<cl_float2,Manual>(resolution*resolution);
	auto dqebuffer = UnmanagedOpenCL::ctx.CreateBuffer<cl_float,Manual>(725);

	clKernel DQE = clKernel(UnmanagedOpenCL::ctx,DQESource, 5, "clDQE");
	clKernel ABS = clKernel(UnmanagedOpenCL::ctx,SqAbsSource, 4, "clSqAbs");

	// Set arguments for imaging kernel
	ImagingKernel.SetArg(0, clWaveFunction2[0], ArgumentType::Input);
	ImagingKernel.SetArg(1, clImageWaveFunction, ArgumentType::Output);
	ImagingKernel.SetArg(2, resolution);
	ImagingKernel.SetArg(3, resolution);
	ImagingKernel.SetArg(4, clXFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(5, clYFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(6, wavelength);
	ImagingKernel.SetArg(7, TEMParams->C10);
	ImagingKernel.SetArg(8, TEMParams->C12);
	ImagingKernel.SetArg(9, TEMParams->C21);
	ImagingKernel.SetArg(10, TEMParams->C23);
	ImagingKernel.SetArg(11, TEMParams->C30);
	ImagingKernel.SetArg(12, TEMParams->C32);
	ImagingKernel.SetArg(13, TEMParams->C34);
	ImagingKernel.SetArg(14, TEMParams->C41);
	ImagingKernel.SetArg(15, TEMParams->C43);
	ImagingKernel.SetArg(16, TEMParams->C45);
	ImagingKernel.SetArg(17, TEMParams->C50);
	ImagingKernel.SetArg(18, TEMParams->C52);
	ImagingKernel.SetArg(19, TEMParams->C54);
	ImagingKernel.SetArg(20, TEMParams->C56);
	ImagingKernel.SetArg(21, TEMParams->Aperture);
	ImagingKernel.SetArg(22, TEMParams->Beta);
	ImagingKernel.SetArg(23, TEMParams->Delta);

	clWorkGroup Work(resolution,resolution,1);

	ImagingKernel(Work);

	// Now get and display absolute value
	FourierTrans(clImageWaveFunction, clWaveFunction1[0], Direction::Inverse);

	ABS.SetArg(0, clWaveFunction1[0], ArgumentType::InputOutput);
	ABS.SetArg(1,Temp1,ArgumentType::Output);
	ABS.SetArg(2,resolution);
	ABS.SetArg(3,resolution);
	ABS(Work);

	FourierTrans(Temp1, clImageWaveFunction, Direction::Forwards);
	binning = 1;
	clEnqueueWriteBuffer(UnmanagedOpenCL::ctx.GetIOQueue(), dqebuffer->GetBuffer(), CL_TRUE, 0, 725 * sizeof(float), dqes[detector], 0, NULL, NULL);

	DQE.SetArg(0,clImageWaveFunction,ArgumentType::InputOutput);
	DQE.SetArg(1,dqebuffer,ArgumentType::Input);
	DQE.SetArg(2,resolution);
	DQE.SetArg(3,resolution);
	DQE.SetArg(4,binning);
	DQE(Work);

	FourierTrans(clImageWaveFunction, Temp1, Direction::Inverse);

	ABS.SetArg(0,Temp1,ArgumentType::Input);
	ABS.SetArg(1,clImageWaveFunction,ArgumentType::Output);
	ABS.SetArg(2,resolution);
	ABS.SetArg(3,resolution);
	ABS(Work);

	// Maybe update diffractogram image also...
	clEnqueueCopyBuffer(UnmanagedOpenCL::ctx.GetIOQueue(), clImageWaveFunction->GetBuffer(), clWaveFunction4[0]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, 0, 0);
};

void TEMSimulation::getDiffImage(float* data, int resolution, int wave)
{
	// Original data is complex so copy complex version down first

	clWorkGroup Work(resolution,resolution,1);

	fftShift.SetArg(0, clWaveFunction2[wave - 1],ArgumentType::Input);
	fftShift(Work);

	std::vector<cl_float2> compdata = clWaveFunction3[0]->CreateLocalCopy();

	float maxf = -CL_MAXFLOAT;
	float minf = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...
		data[i] += (compdata[i].s[0] * compdata[i].s[0] + compdata[i].s[1] * compdata[i].s[1]);

		// Find max,min for contrast limits
		if (data[i] > maxf)
			maxf = data[i];
		if (data[i] < minf)
			minf = data[i];
	}

	diffmin[wave - 1] = minf;
	diffmax[wave - 1] = maxf;
};

void TEMSimulation::getEWImage(float* data, int resolution, int wave)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata = clWaveFunction1[wave - 1]->CreateLocalCopy();

	float max = -CL_MAXFLOAT;
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
	std::vector<cl_float2> compdata = clWaveFunction1[wave - 1]->CreateLocalCopy();

	float max = -CL_MAXFLOAT;
	float min = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{

		// Get sqrt abs value for display...
		data[i] = atan2(compdata[i].s[1], compdata[i].s[0]);
		//data[i] = compdata[i].s[1];

		// Find max,min for contrast limits
		if (data[i] > max)
			max = data[i];
		if (data[i] < min)
			min = data[i];
	}

	ewmin2[wave - 1] = min;
	ewmax2[wave - 1] = max;
};

float TEMSimulation::FloatSumReduction(clMemory<float, Manual>::Ptr Array, clWorkGroup globalSizeSum, clWorkGroup localSizeSum, int nGroups, int totalSize)
{
	clMemory<float,Manual>::Ptr outArray = UnmanagedOpenCL::ctx.CreateBuffer<float,Manual>(nGroups);
	SumReduction.SetArg(0, Array,ArgumentType::Input);

	// Only really need to do these 3 once...
	SumReduction.SetArg(1, outArray,ArgumentType::Output);
	SumReduction.SetArg(2, totalSize);
	SumReduction.SetLocalMemoryArg<float>(3, 256);
	SumReduction(globalSizeSum, localSizeSum);

	// Now copy back 
	std::vector< float> sums = outArray->CreateLocalCopy();

	// Find out which numbers to read back
	float sum = 0;
	for (int i = 0; i < nGroups; i++)
	{
		sum += sums[i];
	}
	return sum;
};