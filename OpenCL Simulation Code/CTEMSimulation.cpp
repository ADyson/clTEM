#include "CTEMSimulation.h"

//TODO: make function to find maximum and minimum from arrays

void CTEMSimulation::initialiseCTEMSimulation(std::shared_ptr<MicroscopeParameters> params, std::shared_ptr<MultisliceStructure> Structure, int res, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints)
{
	resolution = res;

	isFD = FD;
	// Initialise Wavefunctions and Create other buffers...
	clWaveFunction1.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	clWaveFunction2.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	clWaveFunction3.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	clWaveFunction4.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));

	// might not need to be vectors, only if stem needs them ?
	if (isFD)
	{
		clWaveFunction1Minus.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
		clWaveFunction1Plus.push_back(OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution));
	}

	InitPlaneWavefunction = clKernel(OCL::ctx, InitialiseWavefunctionSource.c_str(), 4, "clInitialiseWavefunction");

	clWorkGroup WorkSize(resolution, resolution, 1);

	float InitialValue = 1.0f;
	InitPlaneWavefunction.SetArg(1, resolution);
	InitPlaneWavefunction.SetArg(2, resolution);
	InitPlaneWavefunction.SetArg(3, InitialValue);
	if (isFD) // TODO: in Adam's code, this get's reset later anyway?
	{
		InitPlaneWavefunction.SetArg(0, clWaveFunction1Minus[0], ArgumentType::Output);
		InitPlaneWavefunction(WorkSize);
	}
	InitPlaneWavefunction.SetArg(0, clWaveFunction1[0], ArgumentType::Output);

	InitPlaneWavefunction(WorkSize);

	ImagingKernel = clKernel(OCL::ctx, imagingKernelSource.c_str(), 24, "clImagingKernel");

	InitialiseSimulation(params, Structure, res, startx, starty, endx, endy, Full3D, FD, dz, full3dints, 1);

	OCL::ctx.WaitForQueueFinish();
}

void CTEMSimulation::getCTEMImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata = clImageWaveFunction->CreateLocalCopy();

	float maxF = -FLT_MIN;
	float minF = CL_MAXFLOAT;

	for (int i = 0; i < resolution * resolution; i++)
	{
		// Get absolute value for display...	
		data[i] = compdata[i].s[0]; // already abs in simulateCTEM fucntion

		// Find max,min for contrast limits
		if (data[i] > maxF)
			maxF = data[i];
		if (data[i] < minF)
			minF = data[i];
	}

	imageMin = minF;
	imageMax = maxF;
};

void CTEMSimulation::simulateCTEM()
{
	clKernel ABS = clKernel(OCL::ctx, SqAbsSource, 4, "clSqAbs");

	// Set arguments for imaging kernel
	ImagingKernel.SetArg(0, clWaveFunction2[0], ArgumentType::Input);
	ImagingKernel.SetArg(1, clImageWaveFunction, ArgumentType::Output);
	ImagingKernel.SetArg(2, resolution);
	ImagingKernel.SetArg(3, resolution);
	ImagingKernel.SetArg(4, clXFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(5, clYFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(6, wavelength);
	ImagingKernel.SetArg(7, mParams->C10);
	ImagingKernel.SetArg(8, mParams->C12);
	ImagingKernel.SetArg(9, mParams->C21);
	ImagingKernel.SetArg(10, mParams->C23);
	ImagingKernel.SetArg(11, mParams->C30);
	ImagingKernel.SetArg(12, mParams->C32);
	ImagingKernel.SetArg(13, mParams->C34);
	ImagingKernel.SetArg(14, mParams->C41);
	ImagingKernel.SetArg(15, mParams->C43);
	ImagingKernel.SetArg(16, mParams->C45);
	ImagingKernel.SetArg(17, mParams->C50);
	ImagingKernel.SetArg(18, mParams->C52);
	ImagingKernel.SetArg(19, mParams->C54);
	ImagingKernel.SetArg(20, mParams->C56);
	ImagingKernel.SetArg(21, mParams->Aperture);
	ImagingKernel.SetArg(22, mParams->Beta);
	ImagingKernel.SetArg(23, mParams->Delta);

	clWorkGroup Work(resolution, resolution, 1);

	ImagingKernel(Work);

	// Now get and display absolute value
	FourierTrans(clImageWaveFunction, clWaveFunction1[0], Direction::Inverse);

	ABS.SetArg(0, clWaveFunction1[0], ArgumentType::Input);
	ABS.SetArg(1, clImageWaveFunction, ArgumentType::Output);
	ABS.SetArg(2, resolution);
	ABS.SetArg(3, resolution);
	ABS(Work);
};

void CTEMSimulation::simulateCTEM(int detector, int binning, float doseperpix, float conversionfactor)
{
	// could be done in constructor?
	// populate lsit for DQEs
	std::vector<const float*> dqes;
	dqes.push_back(NULL);
	dqes.push_back(oriusDQE);
	dqes.push_back(k2DQE);

	// populate NTFs
	std::vector<const float*> ntfs;
	ntfs.push_back(NULL);
	ntfs.push_back(oriusNTF);
	ntfs.push_back(k2NTF);

	// Set up some temporary memory objects for the image simulation
	auto Temp1 = OCL::ctx.CreateBuffer<cl_float2, Manual>(resolution*resolution);
	auto dqentfbuffer = OCL::ctx.CreateBuffer<cl_float, Manual>(725);

	// build additional kernels required
	clKernel NTF = clKernel(OCL::ctx, NTFSource, 5, "clNTF");
	clKernel DQE = clKernel(OCL::ctx, DQESource, 5, "clDQE");
	clKernel ABS = clKernel(OCL::ctx, abssource2, 3, "clAbs");
	clKernel ABS2 = clKernel(OCL::ctx, SqAbsSource, 4, "clSqAbs");

	// simulate image
	ImagingKernel.SetArg(0, clWaveFunction2[0], ArgumentType::Input);
	ImagingKernel.SetArg(1, clImageWaveFunction, ArgumentType::Output);
	ImagingKernel.SetArg(2, resolution);
	ImagingKernel.SetArg(3, resolution);
	ImagingKernel.SetArg(4, clXFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(5, clYFrequencies, ArgumentType::Input);
	ImagingKernel.SetArg(6, wavelength);
	ImagingKernel.SetArg(7, mParams->C10);
	ImagingKernel.SetArg(8, mParams->C12);
	ImagingKernel.SetArg(9, mParams->C21);
	ImagingKernel.SetArg(10, mParams->C23);
	ImagingKernel.SetArg(11, mParams->C30);
	ImagingKernel.SetArg(12, mParams->C32);
	ImagingKernel.SetArg(13, mParams->C34);
	ImagingKernel.SetArg(14, mParams->C41);
	ImagingKernel.SetArg(15, mParams->C43);
	ImagingKernel.SetArg(16, mParams->C45);
	ImagingKernel.SetArg(17, mParams->C50);
	ImagingKernel.SetArg(18, mParams->C52);
	ImagingKernel.SetArg(19, mParams->C54);
	ImagingKernel.SetArg(20, mParams->C56);
	ImagingKernel.SetArg(21, mParams->Aperture);
	ImagingKernel.SetArg(22, mParams->Beta);
	ImagingKernel.SetArg(23, mParams->Delta);
	clWorkGroup Work(resolution, resolution, 1);
	ImagingKernel(Work);

	// ifft to real space
	FourierTrans(clImageWaveFunction, clWaveFunction1[0], Direction::Inverse);

	//abs for detected image
	ABS2.SetArg(0, clWaveFunction1[0], ArgumentType::InputOutput);
	ABS2.SetArg(1, Temp1, ArgumentType::Output);
	ABS2.SetArg(2, resolution);
	ABS2.SetArg(3, resolution);
	ABS2(Work);

	//
	// Dose stuff starts here!
	//

	// IFFT
	FourierTrans(Temp1, clImageWaveFunction, Direction::Forwards);
	// write DQE to opencl
	clEnqueueWriteBuffer(OCL::ctx.GetIOQueue(), dqentfbuffer->GetBuffer(), CL_TRUE, 0, 725 * sizeof(float), dqes[detector], 0, NULL, NULL);
	// apply DQE
	DQE.SetArg(0, clImageWaveFunction, ArgumentType::InputOutput);
	DQE.SetArg(1, dqentfbuffer, ArgumentType::Input);
	DQE.SetArg(2, resolution);
	DQE.SetArg(3, resolution);
	DQE.SetArg(4, binning);
	DQE(Work);
	// IFFT back
	FourierTrans(clImageWaveFunction, Temp1, Direction::Inverse);

	// what is this abs squared for?
	ABS.SetArg(0, Temp1, ArgumentType::Input);
	//ABS.SetArg(1,clImageWaveFunction,ArgumentType::Output);
	ABS.SetArg(1, resolution);
	ABS.SetArg(2, resolution);
	ABS(Work);

	float Ntot = doseperpix*binning*binning; // Get this passed in, its dose per pixel i think.

	std::vector<cl_float2> compdata = Temp1->CreateLocalCopy();

	for (int i = 0; i < resolution * resolution; i++)
	{
		double random = ((double)rand() / (RAND_MAX + 1));
		double random2 = ((double)rand() / (RAND_MAX + 1));
		double rstdnormal = sqrt(-2.0f * +log(FLT_MIN + random))*(sin(2.0f * CL_M_PI * random2));

		float val = compdata[i].s[0];
		// CAN CONVERSIOIN FACTOR BE APPLIED HERE?
		compdata[i].s[0] = conversionfactor * floor(Ntot * val + sqrt(fabs(Ntot*val))*rstdnormal); // Was round not floor
		compdata[i].s[1] = 0;
	}

	clImageWaveFunction->Write(compdata);
	FourierTrans(clImageWaveFunction, Temp1, Direction::Forwards);

	clEnqueueWriteBuffer(OCL::ctx.GetIOQueue(), dqentfbuffer->GetBuffer(), CL_TRUE, 0, 725 * sizeof(float), ntfs[detector], 0, NULL, NULL);

	NTF.SetArg(0, Temp1, ArgumentType::InputOutput);
	NTF.SetArg(1, dqentfbuffer, ArgumentType::Input);
	NTF.SetArg(2, resolution);
	NTF.SetArg(3, resolution);
	NTF.SetArg(4, binning);
	NTF(Work);

	FourierTrans(Temp1, clImageWaveFunction, Direction::Inverse);

	// might want to be sqrt (aka normal abs)
	ABS.SetArg(0, clImageWaveFunction, ArgumentType::Input);
	//ABS.SetArg(1, clImageWaveFunction, ArgumentType::Output);
	ABS.SetArg(1, resolution);
	ABS.SetArg(2, resolution);
	ABS(Work);
};

void CTEMSimulation::getEWAbsoluteImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata = clWaveFunction1[0]->CreateLocalCopy();

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

	ewAbsMin = min;
	ewAbsMax = max;
};

void CTEMSimulation::getEWPhaseImage(float* data, int resolution)
{
	// Original data is complex so copy complex version down first
	std::vector<cl_float2> compdata = clWaveFunction1[0]->CreateLocalCopy();

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

	ewPhaseMin = min;
	ewPhaseMax = max;
};