#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>
#include "CL\cl.h"
#include "clWrapper.h"

#pragma once

struct Atom
{
	int Z;
	float x;
	float y;
	float z;
};

struct AtomOcc
{
	int Z;
	float x;
	float y;
	float z;
	float occ;
};

using namespace std;

class MultisliceStructure
{

public:
	std::string filepath;
	bool GotDevice;

	clContext* ctx;

	cl_int status;

	clMemory<float,Manual>::Ptr clAtomx;
	clMemory<float,Manual>::Ptr clAtomy;
	clMemory<float,Manual>::Ptr clAtomz;
	clMemory<int,Manual>::Ptr clAtomZ;
	clMemory<int,Manual>::Ptr clBlockStartPositions;
	clMemory<int,Manual>::Ptr clConstantBlockStartPositions;
	clMemory<int,Manual>::Ptr clBlockIDs;
	clMemory<int,Manual>::Ptr clZIDs;
		
	// OpenCL Memory
	clMemory<float,Manual>::Ptr AtomicStructureParameterisation;

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

	// Check atoms for same position and remove one.
	void CheckOcc(AtomOcc a, AtomOcc b);

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