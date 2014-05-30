#include "clFourier.h"


clFourier::clFourier(void)
{
}


clFourier::~clFourier(void)
{
	clAmdFftDestroyPlan(&fftplan);

	if(buffersize)
		clReleaseMemObject(clMedBuffer);
}



clFourier::clFourier(cl_context &context, clQueue* cmdQueue)
{
	this->context = &context;
	this->clq = cmdQueue;
}

void clFourier::Setup(int width, int height)
{
	// Perform setup for FFT's

	clAmdFftInitSetupData(&fftSetupData);
	fftStatus = clAmdFftSetup(&fftSetupData);


	//	Local Data
	size_t buffSizeBytesIn = 0;
	size_t buffSizeBytesOut = 0;
	size_t fftVectorSize= 0;
	size_t fftVectorSizePadded = 0;
	size_t fftBatchSize = 0;
	cl_uint nBuffersOut = 0;
	cl_uint profileCount = 0;

	
	clAmdFftDim fftdim = CLFFT_2D;
	clAmdFftResultLocation	place = CLFFT_OUTOFPLACE;
	clAmdFftLayout inLayout  = CLFFT_COMPLEX_INTERLEAVED;
	clAmdFftLayout outLayout = CLFFT_COMPLEX_INTERLEAVED;

	size_t clLengths[ 3 ];
	size_t clPadding[ 3 ] = {0, 0, 0 };  // *** TODO
	size_t clStrides[ 4 ];
	size_t batchSize = 1;


	clLengths[0]=width;
	clLengths[1]=height;
	clLengths[2]=1;

	clStrides[ 0 ] = 1;
	clStrides[ 1 ] = clStrides[ 0 ] * (clLengths[ 0 ] + clPadding[ 0 ]);
	clStrides[ 2 ] = clStrides[ 1 ] * (clLengths[ 1 ] + clPadding[ 1 ]);
	clStrides[ 3 ] = clStrides[ 2 ] * (clLengths[ 2 ] + clPadding[ 2 ]);

	fftVectorSize	= clLengths[ 0 ] * clLengths[ 1 ] * clLengths[ 2 ];
	fftVectorSizePadded = clStrides[ 3 ];
	fftBatchSize	= fftVectorSizePadded * batchSize;


	fftStatus = clAmdFftCreateDefaultPlan( &fftplan, *context, fftdim, clLengths );

	//	Default plan creates a plan that expects an inPlace transform with interleaved complex numbers
	fftStatus = clAmdFftSetResultLocation( fftplan, place );
	fftStatus = clAmdFftSetPlanPrecision(fftplan,CLFFT_SINGLE);
	fftStatus = clAmdFftSetLayout( fftplan, inLayout, outLayout );
	fftStatus = clAmdFftSetPlanBatchSize( fftplan, batchSize );
	fftStatus = clAmdFftSetPlanScale (fftplan, CLFFT_FORWARD, 1/sqrtf(width * height));
	fftStatus = clAmdFftSetPlanScale (fftplan, CLFFT_BACKWARD, 1/sqrtf(width * height));

	// Not using padding here yet
	if ((clPadding[ 0 ] | clPadding[ 1 ] | clPadding[ 2 ]) != 0) {
		clAmdFftSetPlanInStride  ( fftplan, fftdim, clStrides );
		clAmdFftSetPlanOutStride ( fftplan, fftdim, clStrides );
		clAmdFftSetPlanDistance  ( fftplan, clStrides[ fftdim ], clStrides[ fftdim ]);
	}

	fftStatus = clAmdFftBakePlan( fftplan, 1, &clq->cmdQueue, NULL, NULL );
	
	//get the buffersize
	
	fftStatus = clAmdFftGetTmpBufSize(fftplan, &buffersize );
		
	if (buffersize)
	{
		clMedBuffer = Buffer( new clMemory(buffersize));
		//clCreateBuffer ( *context, CL_MEM_READ_WRITE, buffersize, 0, &medstatus);
	}
}

void clFourier::Enqueue(cl_mem &input, cl_mem &output, clAmdFftDirection Dir)
{
	fftStatus = clAmdFftEnqueueTransform( fftplan, Dir, 1, &clq->cmdQueue, 0, NULL, NULL, 
			&input, &output, clMedBuffer->buffer );
}

void clFourier::Enqueue(Buffer &input, Buffer &output, clAmdFftDirection Dir)
{
	fftStatus = clAmdFftEnqueueTransform( fftplan, Dir, 1, &clq->cmdQueue, 0, NULL, NULL, 
			&input->buffer, &output->buffer, clMedBuffer->buffer );
}