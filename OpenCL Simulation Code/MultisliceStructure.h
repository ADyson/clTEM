#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>
#include "CL\cl.h"
#include "clKernel.h"
#include "clMemory.h"
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

	Buffer clAtomx;
	Buffer clAtomy;
	Buffer clAtomz;
	Buffer clAtomZ;
	Buffer clBlockStartPositions;
	Buffer clConstantBlockStartPositions;
	Buffer clBlockIDs;
	Buffer clZIDs;
		
	// OpenCL Memory
	Buffer AtomicStructureParameterisation;

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