#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>
#include "CL\cl.h"
#include "clKernel.h"
#include "clState.h"

#pragma once

struct Atom
{
	int Z;
	float x;
	float y;
	float z;
};

using namespace std;

class MultisliceStructure
{

public:
	std::string filepath;
	bool GotDevice;

	cl_context context;
	clQueue* clq;
	clDevice* cldev;
	cl_int status;

	cl_mem clAtomx;
	cl_mem clAtomy;
	cl_mem clAtomz;
	cl_mem clAtomZ;
	cl_mem clBlockStartPositions;
	cl_mem clConstantBlockStartPositions;
	
	// OpenCL Memory
	cl_mem AtomicStructureParameterisation;

	std::vector<int> blockStartPositions;
	MultisliceStructure();
	bool sorted;

	// Import atoms from xyz filepath
	void ImportAtoms(std::string filepath);
	int SortAtoms(bool TDS);
	float TDSRand();
	void ClearStructure();
	void MultisliceStructure::UploadConstantBlock(int topz, int bottomz);

	// Convert atomic symbol i.e. Fe to Atomic Number e.g. 53
	static int GetZNum(std::string AtomSymbol);


	std::vector<Atom> Atoms;

	// Coordinates encompassing all atom positions
	float MaximumX;
	float MinimumX;
	float MaximumY;
	float MinimumY;
	float MaximumZ;
	float MinimumZ;

	int Length;

	int xBlocks;
	int yBlocks;
	float dz;
	int nSlices;

};