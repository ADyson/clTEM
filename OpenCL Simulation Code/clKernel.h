#pragma once
#include "CL/OpenCl.h"
#include <stdlib.h> 
#include <iostream>
#include <vector>
#include <memory>
#include "clMemory.h"
#include "clQueue.h"
#include "clDevice.h"

class clState;
class clKernel;

typedef std::unique_ptr<clKernel> Kernel;

// define dummy type to overload template
typedef bool dummy_CL;
const dummy_CL CL_SKIP = false;

enum clTypes
{
	clFloat = 0,
	clFloat2 = 1,
	clDouble = 2,
	clDouble2 = 3
}; 

class clKernel
{
public:
	clKernel(void);
	~clKernel(void);

	typedef std::vector<std::pair<const char*, ::size_t> > Sources;
	
	const char* kernelcode;
	cl_program kernelprogram;
	cl_int status;
	cl_context* context;
	cl_kernel kernel;
	std::string kernelname;
	size_t log;
	clDevice* cldev;
	clQueue* clq;

	size_t kernelsize;
	int iter;
	int viter;
	
	clKernel(const char* codestring, cl_context &context, clDevice* cldev, std::string kernelname,clQueue* Queue);
	clKernel(cl_context &context, clDevice* cldev, std::string kernelname,clQueue* Queue);

	void loadProgSource(const char* filename);
	void BuildKernel();
	void BuildKernelOld();
	cl_int StatusOK();
	void Enqueue(size_t* globalWorkSize);
	void Enqueue3D(size_t* globalWorkSize);
	void Enqueue3D(size_t* globalWorkSize, size_t* localWorkSize);

	// Dont really need these with badass template :D
	void SetArg(int position,cl_mem argument);
	void SetArg(int position,int argument);
	void SetArg(int position,float argument);

	// Function definition has to be in header for templates...
	// Sets arguments for clKernel
	template <class T>
	void SetArgT(int position, const T &arg) 
	{
		status |= clSetKernelArg(kernel,position,sizeof(T),&arg);
	}
	
	inline void _SetArgS(int i){}

	template<typename T, typename... Args> //variadic template
	inline void _SetArgS(int i, const T& first, const Args& ...args)
	{
		SetArgT(i, first);
		_SetArgS(i+1, args...);
	}

	template<typename T, typename... Args> //variadic template
	inline void _SetArgS(int i, const dummy_CL& first, const Args& ...args)
	{
		//this is used to skip setting argument
		_SetArgS(i+1, args...);
	}

	template<typename... Args> //variadic template
	inline void SetArgS(const Args& ...args)
	{
		_SetArgS(0, args...);
	}

	void SetArgT(int position, Buffer &arg);
	void SetArgT(int position, clMemory &arg);

	void SetArgLocalMemory(int position, int size, clTypes type) 
	{
		switch (type)
		{
		case clFloat:
			status |= clSetKernelArg(kernel,position,size*sizeof(cl_float),NULL);
			break;
		case clFloat2:
			status |= clSetKernelArg(kernel,position,size*sizeof(cl_float2),NULL);
			break;
		case clDouble:
			status |= clSetKernelArg(kernel,position,size*sizeof(cl_double),NULL);
			break;
		case clDouble2:
			status |= clSetKernelArg(kernel,position,size*sizeof(cl_double2),NULL);
			break;
		}
	}

	// Operater method chaining for slightly cleaner openCL argument setting.
	// MUST USE << FIRST to set iterator to 0, then use &&
	// Currently needs support for 'skipping' values when they do no need to be changed
	// http://msdn.microsoft.com/en-us/library/kwyxac18.aspx

	//mostly useful for non c++11 compilers (use SetArgS above if poss)

	template<typename T>
	clKernel& operator<<(T &value)
	{
		iter = 0;
		SetArgT(iter,value);
		iter++;
		return *this;
	}

	clKernel& operator&&(dummy_CL na)
	{
		iter++;
		return *this;
	}

	template<typename T>
	clKernel& operator&&(T &value)
	{
		SetArgT(iter,value);
		iter++;
		return *this;
	}
};
