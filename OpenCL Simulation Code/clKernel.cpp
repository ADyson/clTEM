#include "clKernel.h"
#include <string>
#include <sstream>
#include <iostream>
#include <fstream>
#include "clState.h"

template<class T> inline
std::string t_to_string(T i){
    std::stringstream ss;
    std::string s;
    ss << i;
    s = ss.str();

    return s;
}


clKernel::clKernel(void)
{
}


clKernel::~clKernel(void)
{
	clReleaseProgram(kernelprogram);
	clReleaseKernel(kernel);
}

// Constructor that sets all Command Queues and Contexts etc..
clKernel::clKernel(const char* codestring, cl_context &context, clDevice* cldev, std::string kernelname,clQueue* Queue)
{
	this->cldev = cldev;
	this->context = &context;
	//this->numDevices = numdevices;
	//this->devices = devices;
	this->kernelcode = codestring;
	this->kernelname = kernelname;
	this->clq = Queue;
}

// Constructor that sets all Command Queues and Contexts etc.. for using cl files..
clKernel::clKernel(cl_context &context, clDevice* cldev, std::string kernelname,clQueue* Queue)
{
	this->cldev = cldev;
	this->context = &context;
	//this->numDevices = numdevices;
	//this->devices = devices;
	this->kernelname = kernelname;
	this->clq = Queue;
}


void clKernel::BuildKernel()
{
	// denorms now flushed to zero, and no checks for NaNs or infs, should be faster...
	const char options[] = "-cl-finite-math-only -cl-strict-aliasing -cl-mad-enable -cl-denorms-are-zero";

	this->status = clBuildProgram(kernelprogram,cldev->numDevices,cldev->DevPtr(),options,NULL,NULL);

	this->status = clGetProgramBuildInfo(kernelprogram, cldev->devices, CL_PROGRAM_BUILD_LOG, 0, NULL, &log);

	char *buildlog = (char*)malloc(log*sizeof(char));
	this->status = clGetProgramBuildInfo(kernelprogram, cldev->devices, CL_PROGRAM_BUILD_LOG, log, buildlog, NULL);

	if(!status==0)
	{
		std::string error = t_to_string(status);
		std::string message = "Problem with Kernel Building" + error;
		throw std::exception (message.c_str());
	}

	free(buildlog);

	this->kernel = clCreateKernel(kernelprogram,kernelname.c_str(),&status);

	if(!status==0)
	{
		std::string message = "Problem with Kernel Creation";
		std::string error = message + t_to_string(status);
		throw std::exception (error.c_str());
	}

}

void clKernel::BuildKernelOld()
{
	this->kernelprogram = clCreateProgramWithSource(*context,1,&kernelcode,NULL,&status);

	if(!status==0)
	{
		throw std::exception ("Problem with Kernel Source");
	}

	this->status = clBuildProgram(kernelprogram,cldev->numDevices,cldev->DevPtr(),NULL,NULL,NULL);

	this->status = clGetProgramBuildInfo(kernelprogram, cldev->devices, CL_PROGRAM_BUILD_LOG, 0, NULL, &log);

	char *buildlog = (char*)malloc(log*sizeof(char));
	this->status = clGetProgramBuildInfo(kernelprogram, cldev->devices, CL_PROGRAM_BUILD_LOG, log, buildlog, NULL);

	if(!status==0)
	{
		std::string error = t_to_string(status);
		std::string message = this->kernelname + " Problem with Kernel Building" + error;
		throw std::exception (message.c_str());
	}

	//free(buildlog);

	this->kernel = clCreateKernel(kernelprogram,kernelname.c_str(),&status);

	if(!status==0)
	{
		std::string message = "Problem with Kernel Creation";
		std::string error = message + t_to_string(status);
		throw std::exception (error.c_str());
	}

}

// 0 is success
cl_int clKernel::StatusOK()
{
	if(status != CL_SUCCESS)
	{
		return status;
	}
	else return status;
}

void clKernel::Enqueue(size_t* globalWorkSize )
{
	status = clEnqueueNDRangeKernel(clq->cmdQueue,kernel,2,NULL,globalWorkSize,NULL,0,NULL,NULL);

	if(!status==0)
	{
		std::string message = "Problem with Kernel Enqueue";
		std::string error = message + t_to_string(status);
		throw std::exception (error.c_str());
	}
}

void clKernel::Enqueue3D(size_t* globalWorkSize, size_t* localWorkSize )
{
	status = clEnqueueNDRangeKernel(clq->cmdQueue,kernel,3,NULL,globalWorkSize,localWorkSize,0,NULL,NULL);

	if(!status==0)
	{
		std::string message = "Problem with Kernel Enqueue";
		std::string error = message + t_to_string(status);
		throw std::exception (error.c_str());
	}
}

void clKernel::Enqueue3D(size_t* globalWorkSize )
{
	status = clEnqueueNDRangeKernel(clq->cmdQueue,kernel,3,NULL,globalWorkSize,NULL,0,NULL,NULL);

	if(!status==0)
	{
		std::string message = "Problem with Kernel Enqueue";
		std::string error = message + t_to_string(status);
		throw std::exception (error.c_str());
	}
}

void clKernel::loadProgSource(const char* filename)
{
	

	std::ifstream file(filename);
	if(!file.is_open())
	{
		throw "Can't find cl file";
		return;
	}

	std::string prog(std::istreambuf_iterator<char>(file),(std::istreambuf_iterator<char>()));
	
	Sources source(1,std::make_pair(prog.c_str(), prog.length()+1));

	const ::size_t n = (::size_t)source.size();
    ::size_t* lengths = (::size_t*) alloca(n * sizeof(::size_t));
    const char** strings = (const char**) alloca(n * sizeof(const char*));
	for (::size_t i = 0; i < n; ++i) {
	        strings[i] = source[(int)i].first;
            lengths[i] = source[(int)i].second;
        }

	this->kernelprogram = clCreateProgramWithSource(*context,(cl_uint)n,strings,lengths,&status);

	if(!status==0)
	{
		throw std::exception ("Problem with Kernel Source");
	}

}

void clKernel::SetArgT(int position, Buffer &arg)
{
	status |= clSetKernelArg(kernel,position,sizeof(cl_mem),&arg->buffer);
}

void clKernel::SetArgT(int position, clMemory &arg)
{
	status |= clSetKernelArg(kernel,position,sizeof(cl_mem),&arg.buffer);
}




