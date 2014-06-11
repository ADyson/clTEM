#include "clMemory.h"
#include "clState.h"
#include "clKernel.h"

clMemory::clMemory()
{
	context = clState::context;
	clq = clState::clq;
	Created=false;
}

clMemory::clMemory(size_t size)
{
	context = clState::context;
	clq = clState::clq;

	buffer = clCreateBuffer(clState::context, CL_MEM_READ_WRITE, size, 0, &status);
	Created=true;

	MemoryIndex = clState::RegisterMemory(size);
	AllocatedSize = size;
}

clMemory::clMemory(size_t size, cl_mem_flags flags)
{
	context = clState::context;
	clq = clState::clq;

	buffer = clCreateBuffer(context, flags, size, 0, &status);
	Created=true;
	MemoryIndex = clState::RegisterMemory(size);
	AllocatedSize = size;
}


// Default flag is read/write
void clMemory::Create(size_t size)
{
	if(Created==true)
		throw "Tried to assign the same buffer twice";

	buffer = clCreateBuffer(context, CL_MEM_READ_WRITE, size, 0, &status);
	Created=true;
	MemoryIndex = clState::RegisterMemory(size);
	AllocatedSize = size;
}

void clMemory::Create(size_t size, cl_mem_flags flags)
{
	if(Created==true)
		throw "Tried to assign the same buffer twice";

	buffer = clCreateBuffer(context, flags, size, 0, &status);
	Created=true;
	MemoryIndex = clState::RegisterMemory(size);
	AllocatedSize = size;
}

clMemory::~clMemory()
{
	if(Created==true)
	{
		clState::DeRegisterMemory(MemoryIndex);
		clReleaseMemObject(buffer);
		Created=false;
		AllocatedSize=0;
	}
}

