#pragma once

const char* AtomSortSource = 
"__kernel void clAtomSort(__global const float* xInput, __global const float* yInput, __global const float* zInput, int length, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ, int xBlocks, int yBlocks, __global int* bids, __global int* zids, float dz, int nSlices) \n"
"{		\n"
"	int xid = get_global_id(0);	\n"
"	if(xid < length) \n"
"	{	\n"
"		int bidx = floor((xInput[xid] - MinX)/(MaxX-MinX)*xBlocks); \n"
"		int bidy = floor((yInput[xid] - MinY)/(MaxY-MinY)*yBlocks); \n"
"		int zid  = floor((MaxZ-zInput[xid])/dz); \n"
"		zid-=(zid==nSlices); \n"
"		bidx-=(bidx==xBlocks); \n"
"		bidy-=(bidy==yBlocks); \n"
"		int bid = bidx + xBlocks*bidy; \n"
"		bids[xid] = bid; \n"
"		zids[xid] = zid; \n"
"	}	\n"
"}		\n"
;
