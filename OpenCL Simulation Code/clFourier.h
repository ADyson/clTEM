#pragma once
#include <CL/opencl.h>
#include "clFFT.h"
#include <complex>
#include "clKernel.h"
#include "clMemory.h"
#include <memory>

class clFourier;

typedef std::unique_ptr<clFourier> FourierKernel;

class clFourier
{
public:
	clFourier(void);
	clFourier(cl_context &context, clQueue* cmdQueue);
	~clFourier(void);

	cl_context* context;
	clQueue* clq;
	clfftStatus fftStatus;
	clfftSetupData fftSetupData;
	clfftPlanHandle fftplan;

	//intermediate buffer	
	Buffer clMedBuffer;
	cl_int medstatus;
	size_t buffersize;

	cl_event outEvent;

	void Setup(int width, int height);
	void Enqueue(cl_mem &input, cl_mem &output, clfftDirection Dir);
	void Enqueue(Buffer &input, Buffer &output, clfftDirection Dir);
};

