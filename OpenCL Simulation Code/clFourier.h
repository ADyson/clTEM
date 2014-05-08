#pragma once
#include <CL/opencl.h>
#include "clAmdFft.h"
#include <complex>
#include "clKernel.h"

class clFourier
{
public:
	clFourier(void);
	//clFourier(cl_context context, cl_command_queue cmdQueue);
	clFourier(cl_context &context, clQueue* cmdQueue);
	~clFourier(void);

	cl_context* context;
	//cl_command_queue cmdQueue;
	clQueue* clq;
	clAmdFftStatus fftStatus;
	clAmdFftSetupData fftSetupData;
	clAmdFftPlanHandle fftplan;

	//intermediate buffer	
	cl_mem clMedBuffer;
	cl_int medstatus;
	size_t buffersize;

	cl_event outEvent;

	
	void Setup(int width, int height);

	void Enqueue(cl_mem &input, cl_mem &output, clAmdFftDirection Dir);
};

