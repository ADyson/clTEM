#include "MultisliceStructure.h"
#include "clKernelCodes.h"
#include <ctime>

MultisliceStructure::MultisliceStructure()
{
	sorted = false;
	srand(time(NULL));
};

float MultisliceStructure::TDSRand()
{
	double random = ((double) rand() / (RAND_MAX+1));
	double random2 = ((double) rand() / (RAND_MAX+1));
	double rstdnormal = sqrt(-2.0f * +log(FLT_MIN+random))*(sin(2.0f * CL_M_PI * random2));
	float randNormal = 0.075f * rstdnormal; //random normal(mean,stdDev^2)

	return randNormal;
};

void MultisliceStructure::CheckOcc(AtomOcc a, AtomOcc b)
{

};

void MultisliceStructure::ImportAtoms(std::string filepath) {

	//std::ifstream inputFile(filepath,std::ifstream::in);
	std::ifstream inputFile;
	inputFile.open(filepath,ios::in);
	bool addatom = true;

	Atom linebuffer;
	AtomOcc thisAtom;
	AtomOcc prevAtom;

	if (!inputFile) {
		// TODO: Do something if its gone really bad...
	}

	int numAtoms;
	std::string commentline;
	std::string commentline2;

	// First two lines of .xyz, dont do anytihng with comment though
	inputFile >> numAtoms;
	//inputFile >> commentline; // Will break if comment more than one word...
	getline(inputFile, commentline);
	getline(inputFile, commentline2); // Not sure why it reads this with second line?


	// this branch is hardcoded to be nm for now
	if(commentline2=="occ")
	{
		for(int i=1; i<= numAtoms; i++) {
			std::string atomSymbol;
			inputFile >> atomSymbol >> thisAtom.x >> thisAtom.y >> thisAtom.z >> thisAtom.occ;
			thisAtom.Z = GetZNum(atomSymbol);

			// Check previous atom for same position.
			if(thisAtom.occ != 1)
			{
				if(i!=1)
				{
					if(thisAtom.x ==prevAtom.x && thisAtom.y == prevAtom.y && thisAtom.z == prevAtom.z)
					{
						double r = ((double) rand() / (RAND_MAX));

						if(r <= prevAtom.occ)
						{
							addatom=false;
							; // Don't remove any atoms, don't add next atom;
						}
						else
						{
							Atoms.pop_back();
						}
					}
				}
			}

			prevAtom = thisAtom;
			if(addatom)
			{
				linebuffer.x = thisAtom.x * 10;
				linebuffer.y = thisAtom.y * 10;
				linebuffer.z = thisAtom.z * 10;
				linebuffer.Z = thisAtom.Z;

				Atoms.push_back (linebuffer);
			}

			addatom=true;
		}
	}
	else
	{
		for(int i=1; i<= numAtoms; i++) {
			std::string atomSymbol;
			inputFile >> atomSymbol >> linebuffer.x >> linebuffer.y >> linebuffer.z;
			linebuffer.Z = GetZNum(atomSymbol);
			Atoms.push_back (linebuffer);
		}
	}

	inputFile.close();

	// Find Structure Range Also
	int maxX(0);
	int minX(0);
	int maxY(0);
	int minY(0);
	int maxZ(0);
	int minZ(0);

	for(int i = 0; i < Atoms.size(); i++) {
		if (Atoms[i].x > Atoms[maxX].x)
			maxX=i;
		if (Atoms[i].y > Atoms[maxY].y)
			maxY=i;
		if (Atoms[i].z > Atoms[maxZ].z)
			maxZ=i;
		if (Atoms[i].x < Atoms[minX].x)
			minX=i;
		if (Atoms[i].y < Atoms[minY].y)
			minY=i;
		if (Atoms[i].z < Atoms[minZ].z)
			minZ=i;
	};

	MaximumX = Atoms[maxX].x + 8;
	MinimumX = Atoms[minX].x - 8;
	MaximumY = Atoms[maxY].y + 8;
	MinimumY = Atoms[minY].y - 8;
	MaximumZ = Atoms[maxZ].z + 3;
	MinimumZ = Atoms[minZ].z - 3;

	Length = Atoms.size();
};

int MultisliceStructure::SortAtoms(bool TDS)
{
	if(GotDevice)
	{
		if(sorted)
			ClearStructure();

		std::vector<int>   AtomZNum(Length);
		std::vector<float> AtomXPos(Length);
		std::vector<float> AtomYPos(Length);
		std::vector<float> AtomZPos(Length);

		for(int i = 0; i < Atoms.size(); i++)
		{
			AtomZNum[i] = Atoms[i].Z;
			AtomXPos[i] = Atoms[i].x + TDS*TDSRand();
			AtomYPos[i] = Atoms[i].y + TDS*TDSRand();
			AtomZPos[i] = Atoms[i].z + TDS*TDSRand();
		}


		//Alloc Device Memory
		clAtomx = Buffer(new clMemory(Atoms.size()*sizeof(float)));
		clAtomy = Buffer(new clMemory(Atoms.size()*sizeof(float)));
		clAtomz = Buffer(new clMemory(Atoms.size()*sizeof(float)));
		clAtomZ = Buffer(new clMemory(Atoms.size()*sizeof(cl_int)));
		
		clBlockIDs = Buffer(new clMemory(Atoms.size()*sizeof(cl_int)));
		clZIDs = Buffer(new clMemory(Atoms.size()*sizeof(cl_int)));

		clAtomx->Write(AtomXPos);
		clAtomy->Write(AtomYPos);
		clAtomz->Write(AtomZPos);
		clAtomZ->Write(AtomZNum);

		// Make Kernel and set parameters
		Kernel clAtomSort = Kernel(new clKernel(AtomSortSource,clState::context,clState::cldev,"clAtomSort",clState::clq));
		clAtomSort->BuildKernelOld();

		// NOTE: DONT CHANGE UNLESS CHANGE ELSEWHERE ASWELL!
		// Or fix it so they are all referencing same variable.
		int NumberOfAtoms = Atoms.size();
		xBlocks = 80;
		yBlocks = 80;
		dz		= 1.0f;
		nSlices	= ceil((MaximumZ-MinimumZ)/dz);
		nSlices+=(nSlices==0);


		clAtomSort->SetArgT(0,clAtomx);
		clAtomSort->SetArgT(1,clAtomy);
		clAtomSort->SetArgT(2,clAtomz);
		clAtomSort->SetArgT(3,NumberOfAtoms);
		clAtomSort->SetArgT(4,MinimumX);
		clAtomSort->SetArgT(5,MaximumX);
		clAtomSort->SetArgT(6,MinimumY);
		clAtomSort->SetArgT(7,MaximumY);
		clAtomSort->SetArgT(8,MinimumZ);
		clAtomSort->SetArgT(9,MaximumZ);
		clAtomSort->SetArgT(10,xBlocks);
		clAtomSort->SetArgT(11,yBlocks);
		clAtomSort->SetArgT(12,clBlockIDs);
		clAtomSort->SetArgT(13,clZIDs);
		clAtomSort->SetArgT(14,dz);
		clAtomSort->SetArgT(15,nSlices);
	
		size_t* SortSize = new size_t[3];
		SortSize[0] = NumberOfAtoms;
		SortSize[1] = 1;
		SortSize[2] = 1;


		clAtomSort->Enqueue(SortSize);
	
		//Malloc HBlockStuff
		std::vector<int> HostBlockIDs (Atoms.size());
		std::vector<int> HostZIDs (Atoms.size());

		clBlockIDs->Read(HostBlockIDs);
		clZIDs->Read(HostZIDs);

		vector < vector < vector < float > > > Binnedx;
		Binnedx.resize(xBlocks*yBlocks);
		vector < vector < vector < float > > > Binnedy;
		Binnedy.resize(xBlocks*yBlocks);
		vector < vector < vector < float > > > Binnedz;
		Binnedz.resize(xBlocks*yBlocks);
		vector < vector < vector < int > > > BinnedZ;
		BinnedZ.resize(xBlocks*yBlocks);

	
		for(int i = 0 ; i < xBlocks*yBlocks ; i++){
			Binnedx[i].resize(nSlices);
			Binnedy[i].resize(nSlices);
			Binnedz[i].resize(nSlices);
			BinnedZ[i].resize(nSlices);
		}
	
	
		for(int i = 0; i < Atoms.size(); i++)
		{
			Binnedx[HostBlockIDs[i]][HostZIDs[i]].push_back(AtomXPos[i]-MinimumX);
			Binnedy[HostBlockIDs[i]][HostZIDs[i]].push_back(AtomYPos[i]-MinimumY);
			Binnedz[HostBlockIDs[i]][HostZIDs[i]].push_back(AtomZPos[i]-MinimumZ);
			BinnedZ[HostBlockIDs[i]][HostZIDs[i]].push_back(AtomZNum[i]);
		}
		
		int atomIterator(0);

		std::vector<int> blockStartPositions;
		blockStartPositions.resize(nSlices*xBlocks*yBlocks+1);

		// Put all bins into a linear block of memory ordered by z then y then x and record start positions for every block.
	
		for(int slicei = 0; slicei < nSlices; slicei++)
		{
			for(int j = 0; j < yBlocks; j++)
			{
				for(int k = 0; k < xBlocks; k++)
				{
					blockStartPositions[slicei*xBlocks*yBlocks+ j*xBlocks + k] = atomIterator;

					if(Binnedx[j*xBlocks+k][slicei].size() > 0)
					{
						for(int l = 0; l < Binnedx[j*xBlocks+k][slicei].size(); l++)
						{
							// cout <<"Block " << j <<" , " << k << endl;
							AtomXPos[atomIterator] = Binnedx[j*xBlocks+k][slicei][l];
							AtomYPos[atomIterator] = Binnedy[j*xBlocks+k][slicei][l];
							AtomZPos[atomIterator] = Binnedz[j*xBlocks+k][slicei][l];
							AtomZNum[atomIterator] = BinnedZ[j*xBlocks+k][slicei][l];
							atomIterator++;
						}
					}
				}
			}
		}

		// Trying to store rows of yBlocks consecutively for faster coalesced loading in GPU
		// Atoms still consecutive in terms of xBlock (not sure this is possible)
		//for(int slicei = 0; slicei < nSlices; slicei++)
		//{
		//	for(int j = 0; j < yBlocks; j++)
		//	{
		//		for(int k = 0; k < xBlocks; k++)
		//		{
		//			blockStartPositions[slicei*xBlocks*yBlocks+ k*yBlocks + j] = atomIterator;

		//			if(Binnedx[j*xBlocks+k][slicei].size() > 0)
		//			{
		//				for(int l = 0; l < Binnedx[j*xBlocks+k][slicei].size(); l++)
		//				{
		//					// cout <<"Block " << j <<" , " << k << endl;
		//					AtomXPos[atomIterator] = Binnedx[j*xBlocks+k][slicei][l];
		//					AtomYPos[atomIterator] = Binnedy[j*xBlocks+k][slicei][l];
		//					AtomZPos[atomIterator] = Binnedz[j*xBlocks+k][slicei][l];
		//					AtomZNum[atomIterator] = BinnedZ[j*xBlocks+k][slicei][l];
		//					atomIterator++;
		//				}
		//			}
		//		}
		//	}
		//}


		// Last element indicates end of last block as total number of atoms.
		blockStartPositions[nSlices*xBlocks*yBlocks] = Atoms.size();

		// Now upload the sorted atoms onto the device..
		clAtomx->Write(AtomXPos);
		clAtomy->Write(AtomYPos);
		clAtomz->Write(AtomZPos);
		clAtomZ->Write(AtomZNum);

		clBlockStartPositions = Buffer( new clMemory((nSlices*xBlocks*yBlocks+1) * sizeof( cl_int )));

		clBlockStartPositions->Write(blockStartPositions);
	
		// 7 is 2 * loadzslices + 1
		//clConstantBlockStartPositions = clCreateBuffer ( clState::context, CL_MEM_READ_ONLY, (7*xBlocks*yBlocks+1) * sizeof( cl_int ), 0, &status);

		clFinish(clState::clq->cmdQueue);
		sorted = true;
	}
	return 1;
};

int MultisliceStructure::GetZNum(std::string atomSymbol) {
		 
	if (atomSymbol == "H")
		return 1;
	else if (atomSymbol == "He")
		return 2;
	else if (atomSymbol == "Li")
		return 3;
	else if (atomSymbol == "Be")
		return 4;
	else if (atomSymbol == "B")
		return 5;
	else if (atomSymbol == "C")
		return 6;
	else if (atomSymbol == "N")
		return 7;
	else if (atomSymbol == "O")
		return 8;
	else if (atomSymbol == "F")
		return 9;
	else if (atomSymbol == "Na")
		return 11;
	else if (atomSymbol == "Mg")
		return 12;
	else if (atomSymbol == "Al")
		return 13;
	else if (atomSymbol == "Si")
		return 14;
	else if (atomSymbol == "P")
		return 15;
	else if (atomSymbol == "S")
		return 16;
	else if (atomSymbol == "Cl")
		return 17;
	else if (atomSymbol == "Ar")
		return 18;
	else if (atomSymbol == "K")
		return 19;
	else if (atomSymbol == "Ca")
		return 20;
	else if (atomSymbol == "Sc")
		return 21;
	else if (atomSymbol == "Ti")
		return 22;
	else if (atomSymbol == "V")
		return 23;
	else if (atomSymbol == "Cr")
		return 24;
	else if (atomSymbol == "Mn")
		return 25;
	else if (atomSymbol == "Fe")
		return 26;
	else if (atomSymbol == "Co")
		return 27;
	else if (atomSymbol == "Ni")
		return 28;
	else if (atomSymbol == "Cu")
		return 29;
	else if (atomSymbol == "Zn")
		return 30;
	else if (atomSymbol == "Ga")
		return 31;
	else if (atomSymbol == "Ge")
		return 32;
	else if (atomSymbol == "As")
		return 33;
	else if (atomSymbol == "Se")
		return 34;
	else if (atomSymbol == "Br")
		return 35;
	else if (atomSymbol == "Kr")
		return 36;
	else if (atomSymbol == "Rb")
		return 37;
	else if (atomSymbol == "Sr")
		return 38;
	else if (atomSymbol == "Y")
		return 39;
	else if (atomSymbol == "Zr")
		return 40;
	else if (atomSymbol == "Nb")
		return 41;
	else if (atomSymbol == "Mo")
		return 42;
	else if (atomSymbol == "Tc")
		return 43;
	else if (atomSymbol == "Ru")
		return 44;
	else if (atomSymbol == "Rh")
		return 45;
	else if (atomSymbol == "Pd")
		return 46;
	else if (atomSymbol == "Ag")
		return 47;
	else if (atomSymbol == "Cd")
		return 48;
	else if (atomSymbol == "In")
		return 49;
	else if (atomSymbol == "Sn")
		return 50;
	else if (atomSymbol == "Sb")
		return 51;
	else if (atomSymbol == "Te")
		return 52;
	else if (atomSymbol == "I")
		return 53;
	else if (atomSymbol == "Xe")
		return 54;
	else if (atomSymbol == "Cs")
		return 55;
	else if (atomSymbol == "Ba")
		return 56;
	else if (atomSymbol == "La")
		return 57;
	else if (atomSymbol == "Ce")
		return 58;
	else if (atomSymbol == "Pr")
		return 59;
	else if (atomSymbol == "Nd")
		return 60;
	else if (atomSymbol == "Pm")
		return 61;
	else if (atomSymbol == "Sm")
		return 62;
	else if (atomSymbol == "Eu")
		return 63;
	else if (atomSymbol == "Gd")
		return 64;
	else if (atomSymbol == "Tb")
		return 65;
	else if (atomSymbol == "Dy")
		return 66;
	else if (atomSymbol == "Ho")
		return 67;
	else if (atomSymbol == "Er")
		return 68;
	else if (atomSymbol == "Tm")
		return 69;
	else if (atomSymbol == "Yb")
		return 70;
	else if (atomSymbol == "Lu")
		return 71;
	else if (atomSymbol == "Hf")
		return 72;
	else if (atomSymbol == "Ta")
		return 73;
	else if (atomSymbol == "W")
		return 74;
	else if (atomSymbol == "Re")
		return 75;
	else if (atomSymbol == "Os")
		return 76;
	else if (atomSymbol == "Ir")
		return 77;
	else if (atomSymbol == "Pt")
		return 78;
	else if (atomSymbol == "Au")
		return 79;
	else if (atomSymbol == "Hg")
		return 80;
	else if (atomSymbol == "Tl")
		return 81;
	else if (atomSymbol == "Pb")
		return 82;
	else if (atomSymbol == "Bi")
		return 83;
	else if (atomSymbol == "Po")
		return 84;
	else if (atomSymbol == "At")
		return 85;
	else if (atomSymbol == "Rn")
		return 86;
	else if (atomSymbol == "Fr")
		return 87;
	else if (atomSymbol == "Ra")
		return 88;
	else if (atomSymbol == "Ac")
		return 89;
	else if (atomSymbol == "Th")
		return 90;
	else if (atomSymbol == "Pa")
		return 91;
	else if (atomSymbol == "U")
		return 92;
	else if (atomSymbol == "Np")
		return 93;
	else if (atomSymbol == "Pu")
		return 94;
	else if (atomSymbol == "Am")
		return 95;
	else if (atomSymbol == "Cm")
		return 96;
	else if (atomSymbol == "Bk")
		return 97;
	else if (atomSymbol == "Cf")
		return 98;
	else if (atomSymbol == "Es")
		return 99;
	else if (atomSymbol == "Fm")
		return 100;
	else if (atomSymbol == "Md")
		return 101;
	else if (atomSymbol == "No")
		return 102;
	else if (atomSymbol == "Lr")
		return 103;

	// Should actually include messages for when I have an input i don't recognise
	else return 1;

}

void MultisliceStructure::ClearStructure() {

	// Clear all memory that was used to store and sort the atoms
	// This would not be necessary if we just created a new structure object for each file.

	sorted=false;
};

void MultisliceStructure::UploadConstantBlock(int topz, int bottomz)
{
	//clEnqueueWriteBuffer( clq->cmdQueue, clConstantBlockStartPositions, CL_TRUE, 0,((bottomz-topz)*xBlocks*yBlocks+1) * sizeof( cl_int ) , &blockStartPositions[ topz*xBlocks*yBlocks ], 0, NULL, NULL );
};