#pragma once
#include <CL/opencl.h>
#include "clAmdFft.h"
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
	clAmdFftStatus fftStatus;
	clAmdFftSetupData fftSetupData;
	clAmdFftPlanHandle fftplan;

	//intermediate buffer	
	Buffer clMedBuffer;
	cl_int medstatus;
	size_t buffersize;

	cl_event outEvent;

	
	void Setup(int width, int height);

	void Enqueue(cl_mem &input, cl_mem &output, clAmdFftDirection Dir);
	void Enqueue(Buffer &input, Buffer &output, clAmdFftDirection Dir);
};

