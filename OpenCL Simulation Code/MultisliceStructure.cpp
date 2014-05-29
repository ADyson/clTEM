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

void MultisliceStructure::ImportAtoms(std::string filepath) {

	std::ifstream inputFile(filepath,std::ifstream::in);
	//inputFile.open(filename,ios::in);

	Atom linebuffer;

	if (!inputFile) {
		// TODO: Do something if its gone really bad...
	}

	int numAtoms;
	std::string commentline;

	// First two lines of .xyz, dont do anytihng with comment though
	inputFile >> numAtoms;
	getline(inputFile,commentline);

	for(int i=1; i<= numAtoms; i++) {
		std::string atomSymbol;
		inputFile >> atomSymbol >> linebuffer.x >> linebuffer.y >> linebuffer.z;
		linebuffer.Z = GetZNum(atomSymbol);
		Atoms.push_back (linebuffer);
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

		//cl_mem clBlockIDs = clCreateBuffer ( clState::context, CL_MEM_READ_WRITE, Atoms.size() * sizeof( cl_int ), 0, &status);
		//cl_mem clZIDs = clCreateBuffer ( clState::context, CL_MEM_READ_WRITE, Atoms.size() * sizeof( cl_int ), 0, &status);
		
		//clMemory clBlockIDs;
		//clBlockIDs.Create(Atoms.size()*sizeof(int));
		//clMemory clZIDs;
		//clZIDs.Create(Atoms.size()*sizeof(int));

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
		xBlocks = 90;
		yBlocks = 90;
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

		//clEnqueueReadBuffer(clState::clq->cmdQueue,clBlockIDs,CL_FALSE,0,Atoms.size()*sizeof(int),&HostBlockIDs[0],0,NULL,NULL);
		//clEnqueueReadBuffer(clState::clq->cmdQueue,clZIDs,CL_TRUE,0,Atoms.size()*sizeof(int),&HostZIDs[0],0,NULL,NULL);

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
		//clEnqueueWriteBuffer( clState::clq->cmdQueue, clAtomx, CL_FALSE, 0, Atoms.size()*sizeof(cl_float), &AtomXPos[ 0 ], 0, NULL, NULL );
		//clEnqueueWriteBuffer( clState::clq->cmdQueue, clAtomy, CL_FALSE, 0, Atoms.size()*sizeof(cl_float), &AtomYPos[ 0 ], 0, NULL, NULL );
		//clEnqueueWriteBuffer( clState::clq->cmdQueue, clAtomz, CL_FALSE, 0, Atoms.size()*sizeof(cl_float), &AtomZPos[ 0 ], 0, NULL, NULL );
		//clEnqueueWriteBuffer( clState::clq->cmdQueue, clAtomZ, CL_FALSE, 0, Atoms.size()*sizeof(cl_int), &AtomZNum[ 0 ], 0, NULL, NULL );

		clAtomx->Write(AtomXPos);
		clAtomy->Write(AtomYPos);
		clAtomz->Write(AtomZPos);
		clAtomZ->Write(AtomZNum);

		clBlockStartPositions = Buffer( new clMemory((nSlices*xBlocks*yBlocks+1) * sizeof( cl_int )));
		//clCreateBuffer ( clState::context, CL_MEM_READ_WRITE, (nSlices*xBlocks*yBlocks+1) * sizeof( cl_int ), 0, &status);

		clBlockStartPositions->Write(blockStartPositions);
		//clEnqueueWriteBuffer( clState::clq->cmdQueue, clBlockStartPositions, CL_TRUE, 0,(nSlices*xBlocks*yBlocks+1) * sizeof( cl_int ) , &blockStartPositions[ 0 ], 0, NULL, NULL );
	
		// 7 is 2 * loadzslices + 1
		//clConstantBlockStartPositions = clCreateBuffer ( clState::context, CL_MEM_READ_ONLY, (7*xBlocks*yBlocks+1) * sizeof( cl_int ), 0, &status);

		// TODO some cleanup probably
		//clAtomSort->~clKernel();

		//clReleaseMemObject(clBlockIDs);
		//clReleaseMemObject(clZIDs);

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
	else if (atomSymbol == "Sr")
		return 38;
	else if (atomSymbol == "Ru")
		return 44;
	else if (atomSymbol == "Br")
		return 35;
	else if (atomSymbol == "Fe")
		return 26;
	else if (atomSymbol == "La")
		return 57;
	else if (atomSymbol == "Sm")
		return 62;
	else if (atomSymbol == "Ta")
		return 73;
	else if (atomSymbol == "W")
		return 74;

	// Should actually include messages for when I have an input i don't recognise
	else return 1;

}

void MultisliceStructure::ClearStructure() {

	// Clear all memory that was used to store and sort the atoms
	clAtomx.release();
	clAtomy.release();
	clAtomz.release();
	clAtomZ.release();
	clBlockStartPositions.release();
	clBlockIDs.release();
	clZIDs.release();
	//clReleaseMemObject(clAtomx);
	//clReleaseMemObject(clAtomy);
	//clReleaseMemObject(clAtomz);
	//clReleaseMemObject(clAtomZ);
	//clReleaseMemObject(clBlockStartPositions);

	sorted=false;
};

void MultisliceStructure::UploadConstantBlock(int topz, int bottomz)
{
	//clEnqueueWriteBuffer( clq->cmdQueue, clConstantBlockStartPositions, CL_TRUE, 0,((bottomz-topz)*xBlocks*yBlocks+1) * sizeof( cl_int ) , &blockStartPositions[ topz*xBlocks*yBlocks ], 0, NULL, NULL );
};