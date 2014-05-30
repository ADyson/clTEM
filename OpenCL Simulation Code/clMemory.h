#pragma once
#include "CL/OpenCl.h"
#include <vector>
#include <stdlib.h> 
#include <memory>

class clMemory;

typedef std::unique_ptr<clMemory> Buffer;

class clQueue;

class clMemory
{
private:
	cl_context context;
	clQueue* clq;
	cl_int status;

public:
	size_t AllocatedSize;
	cl_mem buffer;
	int MemoryIndex;
	bool Created;
	void Create(size_t size);
	void Create(size_t size, cl_mem_flags flags);
	clMemory();
	clMemory(size_t size);
	clMemory(size_t size, cl_mem_flags flags);
	~clMemory();

	template<typename T>void Write(std::vector<T> &data){
			clEnqueueWriteBuffer(clq->cmdQueue,buffer,CL_FALSE,0,data.size()*sizeof(T),&data[0],0,NULL,NULL);
	};
	
	 template<typename T> void Read(std::vector<T> &data){
			clEnqueueReadBuffer(clq->cmdQueue,buffer,CL_TRUE,0,data.size()*sizeof(T),&data[0],0,NULL,NULL);
	};

};