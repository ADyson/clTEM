#include "MainSimulation.h"

void MicroscopeSimulation::InitialiseSimulation(std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves = 1)
{
	//set some variables we need to keep
	mParams = params;
	isFD = FD; //already been set?
	resolution = res;
	AtomicStructure = Structure;
	AtomicStructure->dz = dz;

	//calculate the pixel scale for the simulation
	float RealSizeX = endx - startx;
	float RealSizeY = endy - starty;
	pixelscale = max(RealSizeX, RealSizeY) / (resolution); //TODO: check this should add on the padding

	// Work out size of each binned block of atoms
	float BlockScaleX = (AtomicStructure->MaximumX - AtomicStructure->MinimumX) / AtomicStructure->xBlocks;
	float BlockScaleY = (AtomicStructure->MaximumY - AtomicStructure->MinimumY) / AtomicStructure->yBlocks;

	// Work out area that is to be simulated (in real space)
	float SimSizeX = pixelscale * resolution;
	float SimSizeY = SimSizeX;

	// create local copy of Pi for convenience
	float Pi = Constants::Pi;

	float V = mParams->Voltage * 1000; // Microscope voltage (V)
	wavelength = mParams->Wavelength(); // Electron wavelength (Angstrom)
	//float sigma = 2 * Pi * ((Constants::eMassEnergy + V) / (2.0f * Constants::eMassEnergy + V)) / (V * wavelength); //?
	float sigma = (2 * Pi / (wavelength * V)) * (Constants::eMass*Constants::c*Constants::c + Constants::eCharge * V) / (2 * Constants::eMass*Constants::c*Constants::c + Constants::eCharge * V); //?
	//float fix = 300.8242834f / (4 * Pi*Pi*Constants::a0A*Constants::eCharge); //?
	//float V2 = V * 1000; // Microscope voltage (V)

	//TODO: why is this needed
	//FDsigma = sigma;


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
		else
			temp = (i - 1) / SimSizeX;
		k0x.push_back(temp);
	}

	for (int i = 1; i <= resolution; i++)
	{
		if ((i - 1) > imidy)
			temp = ((i - 1) - resolution) / SimSizeY;
		else
			temp = (i - 1) / SimSizeY;
		k0y.push_back(temp);
	}

	// Find maximum frequency for bandwidth limiting rule....

	float bandwidthkmax = 0.0f;

	float kmaxx = pow((k0x[imidx - 1] * 1 / 2), 2);
	float kmaxy = pow((k0y[imidy - 1] * 1 / 2), 2);

	if (kmaxy <= kmaxx)
		bandwidthkmax = kmaxy;
	else
		bandwidthkmax = kmaxx;

	// k not k^2.
	bandwidthkmax = sqrt(bandwidthkmax);

	// Bandlimit by FDdz size
	float fnkx = resolution;
	float fnky = resolution;

	float p1 = fnkx / (2 * SimSizeX);
	float p2 = fnky / (2 * SimSizeY);
	float p12 = p1*p1;
	float p22 = p2*p2;

	float ke2 = (0.666666f)*(p12 + p22);

	float quadraticA = (ke2*ke2 * 16 * Pi*Pi*Pi*Pi) - (32 * Pi*Pi*Pi*ke2*sigma*V / wavelength) + (16 * Pi*Pi*sigma*sigma*V*V / (wavelength*wavelength));
	float quadraticB = 16 * Pi*Pi*(ke2 - (sigma*V / (Pi*wavelength)) - (1 / (4 * wavelength*wavelength)));
	float quadraticC = 3;
	float quadraticB24AC = quadraticB * quadraticB - 4 * quadraticA*quadraticC;

	// Now use these to determine acceptable resolution or enforce extra band limiting beyond 2/3
	if (quadraticB24AC < 0)
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
	if (maxStableDz > 0.06)
		maxStableDz = 0.06;

	FDdz = maxStableDz;

	int nFDSlices = ceil((AtomicStructure->MaximumZ - AtomicStructure->MinimumZ) / maxStableDz);
	// Prevent 0 slices for perfectly flat sample
	nFDSlices += (nFDSlices == 0);

	// Set class variables
	NumberOfFDSlices = nFDSlices;

	clXFrequencies = OCL::ctx.CreateBuffer<float, Manual>(resolution);
	clYFrequencies = OCL::ctx.CreateBuffer<float, Manual>(resolution);

	clXFrequencies->Write(k0x);
	clYFrequencies->Write(k0y);

	// Setup Fourier Transforms
	FourierTrans = clFourier(OCL::ctx, resolution, resolution);

	clTDSk.resize(resolution*resolution); // STEM?
	clImageWaveFunction = OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution);
	clPropagator = OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution);
	clPotential = OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution);

	SumReduction = clKernel(OCL::ctx, floatSumReductionsource2, 4, "clFloatSumReduction"); // STEM?
	BandLimit = clKernel(OCL::ctx, BandLimitSource, 6, "clBandLimit");
	fftShift = clKernel(OCL::ctx, fftShiftSource, 4, "clfftShift");

	//TODO: might need split here

	fftShift.SetArg(0, clWaveFunction2[0], ArgumentType::Input);
	fftShift.SetArg(1, clWaveFunction3[0], ArgumentType::Output);
	fftShift.SetArg(2, resolution);
	fftShift.SetArg(3, resolution);

	BandLimit.SetArg(0, clWaveFunction3[0], ArgumentType::InputOutput);
	BandLimit.SetArg(1, resolution);
	BandLimit.SetArg(2, resolution);
	BandLimit.SetArg(3, bandwidthkmax);
	BandLimit.SetArg(4, clXFrequencies, ArgumentType::Input);
	BandLimit.SetArg(5, clYFrequencies, ArgumentType::Input);

	clWorkGroup WorkSize(resolution, resolution, 1);

	//TODO: might need split here

	if (Full3D)
	{
		BinnedAtomicPotential = clKernel(OCL::ctx, opt2source, 27, "clBinnedAtomicPotentialOpt");
	}
	else if (isFD)
	{
		BinnedAtomicPotential = clKernel(OCL::ctx, fd2source, 26, "clBinnedAtomicPotentialOptFD");
	}
	else
	{
		BinnedAtomicPotential = clKernel(OCL::ctx, conv2source, 26, "clBinnedAtomicPotentialConventional");
	}

	// Work out which blocks to load by ensuring we have the entire area around workgroup upto 5 angstroms away...
	int loadblocksx = ceil(3.0f / BlockScaleX);
	int loadblocksy = ceil(3.0f / BlockScaleY);
	int loadblocksz = ceil(3.0f / AtomicStructure->dz);

	// Set some of the arguments which dont change each iteration
	BinnedAtomicPotential.SetArg(0, clPotential, ArgumentType::Output);
	BinnedAtomicPotential.SetArg(5, AtomicStructure->AtomicStructureParameterisation, ArgumentType::Input);
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
	BinnedAtomicPotential.SetArg(23, sigma); // Not sure why I am using this sigma and not commented sigma...
	BinnedAtomicPotential.SetArg(24, startx);
	BinnedAtomicPotential.SetArg(25, starty);

	if (Full3D)
		BinnedAtomicPotential.SetArg(26, full3dints);

	// Also need to generate propagator.
	GeneratePropagator = clKernel(OCL::ctx, propsource, 8, "clGeneratePropagator");

	GeneratePropagator.SetArg(0, clPropagator, ArgumentType::Output);
	GeneratePropagator.SetArg(1, clXFrequencies, ArgumentType::Input);
	GeneratePropagator.SetArg(2, clYFrequencies, ArgumentType::Input);
	GeneratePropagator.SetArg(3, resolution);
	GeneratePropagator.SetArg(4, resolution);

	if (isFD)
		GeneratePropagator.SetArg(5, FDdz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)
	else
		GeneratePropagator.SetArg(5, AtomicStructure->dz); // Is this the right dz? (Propagator needs slice thickness not spacing between atom bins)


	GeneratePropagator.SetArg(6, wavelength);
	GeneratePropagator.SetArg(7, bandwidthkmax);

	GeneratePropagator(WorkSize);

	// And multiplication kernel
	ComplexMultiply = clKernel(OCL::ctx, multisource, 5, "clComplexMultiply");
	ComplexMultiply.SetArg(3, resolution);
	ComplexMultiply.SetArg(4, resolution);

	diffMin.resize(waves);
	diffMax.resize(waves);

	if (isFD)
	{
		// Need Grad Kernel and FiniteDifference also
		GradKernel = clKernel(OCL::ctx, gradsource, 5, "clGrad");
		FiniteDifference = clKernel(OCL::ctx, fdsource, 10, "clFiniteDifference");
	}
}

void MicroscopeSimulation::doMultisliceStep(int stepno, int steps, int waves)
{
	if (isFD)
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

	BinnedAtomicPotential.SetArg(1, AtomicStructure->clAtomx, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(2, AtomicStructure->clAtomy, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(3, AtomicStructure->clAtomz, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(4, AtomicStructure->clAtomZ, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(6, AtomicStructure->clBlockStartPositions, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(9, slice);
	BinnedAtomicPotential.SetArg(10, slices);
	BinnedAtomicPotential.SetArg(11, currentz);

	clWorkGroup Work(resolution, resolution, 1);

	clWorkGroup LocalWork(16, 16, 1);

	BinnedAtomicPotential(Work, LocalWork);

	FourierTrans(clPotential, clWaveFunction3[0], Direction::Forwards);
	BandLimit(Work);
	FourierTrans(clWaveFunction3[0], clPotential, Direction::Inverse);

	// Now for the rest of the multislice steps
	for (int i = 1; i <= waves; i++)
	{
		//Multiply with wavefunction
		ComplexMultiply.SetArg(0, clPotential, ArgumentType::Input);
		ComplexMultiply.SetArg(1, clWaveFunction1[i - 1], ArgumentType::Input);
		ComplexMultiply.SetArg(2, clWaveFunction2[i - 1], ArgumentType::Output);
		ComplexMultiply(Work);

		// Propagate
		FourierTrans(clWaveFunction2[i - 1], clWaveFunction3[0], Direction::Forwards);

		// BandLimit OK here?
		ComplexMultiply.SetArg(0, clWaveFunction3[0], ArgumentType::Input);
		ComplexMultiply.SetArg(1, clPropagator, ArgumentType::Input);
		ComplexMultiply.SetArg(2, clWaveFunction2[i - 1], ArgumentType::Output);
		ComplexMultiply(Work);


		FourierTrans(clWaveFunction2[i - 1], clWaveFunction1[i - 1], Direction::Inverse);
	}
	OCL::ctx.WaitForQueueFinish();
};

void MicroscopeSimulation::doMultisliceStepFD(int stepno, int waves)
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

	BinnedAtomicPotential.SetArg(1, AtomicStructure->clAtomx, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(2, AtomicStructure->clAtomy, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(3, AtomicStructure->clAtomz, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(4, AtomicStructure->clAtomZ, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(6, AtomicStructure->clBlockStartPositions, ArgumentType::Input);
	BinnedAtomicPotential.SetArg(9, atomslice);
	BinnedAtomicPotential.SetArg(10, slices);
	BinnedAtomicPotential.SetArg(11, currentz);

	clWorkGroup Work(resolution, resolution, 1);

	clWorkGroup LocalWork(16, 16, 1);

	BinnedAtomicPotential(Work, LocalWork);

	FourierTrans(clPotential, clWaveFunction3[0], Direction::Forwards);
	BandLimit(Work);
	FourierTrans(clWaveFunction3[0], clPotential, Direction::Inverse);

	// Now for the rest of the multislice steps
	for (int i = 1; i <= waves; i++)
	{

		// //FT Psi into Grad2.
		FourierTrans(clWaveFunction1[i - 1], clWaveFunction3[0], Direction::Forwards);

		// //Grad Kernel on Grad2.
		GradKernel.SetArg(0, clWaveFunction3[0], ArgumentType::Input);
		GradKernel.SetArg(1, clXFrequencies, ArgumentType::Input);
		GradKernel.SetArg(2, clYFrequencies, ArgumentType::Input);
		GradKernel.SetArg(3, resolution);
		GradKernel.SetArg(4, resolution);
		GradKernel(Work);

		// //IFT Grad2 into Grad.
		FourierTrans(clWaveFunction3[0], clWaveFunction4[i - 1], Direction::Inverse);

		// //FD Kernel
		FiniteDifference.SetArg(0, clPotential, ArgumentType::Input);
		FiniteDifference.SetArg(1, clWaveFunction4[i - 1], ArgumentType::Input);
		FiniteDifference.SetArg(2, clWaveFunction1Minus[i - 1], ArgumentType::Input);
		FiniteDifference.SetArg(3, clWaveFunction1[i - 1], ArgumentType::Input);
		FiniteDifference.SetArg(4, clWaveFunction1Plus[i - 1], ArgumentType::Output);
		FiniteDifference.SetArg(5, FDdz);
		FiniteDifference.SetArg(6, wavelength);
		FiniteDifference.SetArg(7, FDsigma);
		FiniteDifference.SetArg(8, resolution);
		FiniteDifference.SetArg(9, resolution);
		FiniteDifference(Work);


		// //Bandlimit PsiPlus
		FourierTrans(clWaveFunction1Plus[i - 1], clWaveFunction3[0], Direction::Forwards);
		BandLimit(Work);
		FourierTrans(clWaveFunction3[0], clWaveFunction1Plus[i - 1], Direction::Inverse);

		// // Psi becomes PsiMinus
		clEnqueueCopyBuffer(OCL::ctx.GetIOQueue(), clWaveFunction1[i - 1]->GetBuffer(), clWaveFunction1Minus[i - 1]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, nullptr, nullptr);

		// // PsiPlus becomes Psi.
		clEnqueueCopyBuffer(OCL::ctx.GetIOQueue(), clWaveFunction1Plus[i - 1]->GetBuffer(), clWaveFunction1[i - 1]->GetBuffer(), 0, 0, resolution*resolution*sizeof(cl_float2), 0, nullptr, nullptr);

		// // To maintain status with other versions resulting end arrays should still be as follows.
		// // Finished wavefunction in real spaaaaaace in clWaveFunction1.
		// // Finished wavefunction in reciprocal spaaaaaace in clWaveFunction2.
		// // 3 and 4 were previously temporary.

		FourierTrans(clWaveFunction1[i - 1], clWaveFunction2[i - 1], Direction::Forwards);

	}

	OCL::ctx.WaitForQueueFinish();
};

void MicroscopeSimulation::getDiffImage(float* data, int resolution, int wave)
{
	// Original data is complex so copy complex version down first

	clWorkGroup Work(resolution, resolution, 1);

	fftShift.SetArg(0, clWaveFunction2[wave - 1], ArgumentType::Input);
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

	diffMin[wave - 1] = minf;
	diffMax[wave - 1] = maxf;
};