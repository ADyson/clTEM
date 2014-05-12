#pragma once

const char* InitialiseWavefunctionSource = 
"__kernel void clInitialiseWavefunction(__global float2* InputWavefunction, int width, int height, float value) \n"
"{		\n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1);	\n"
"	if(xid < width && yid < height) \n"
"	{	\n"
"		int Index = xid + width*yid; \n"
"		InputWavefunction[Index].x = value; \n"
"		InputWavefunction[Index].y = 0; \n"
"	}	\n"
"}		\n"
;

// Slices have to start from 0
// Tried to fix so model doesnt have to have min at (0,0,0)
// Uses normal potential sliced many times not projected.
// Includes atoms onmultiple slices where they contribute
// Could be alot faster will try other methods like one kernel for each atom type with pre tabulated potentials.
// z is height of top of slice...
const char* BinnedAtomicPotentialSource = 
"__kernel void clBinnedAtomicPotential(__global float2* Potential, __global float* clAtomXPos, __global float* clAtomYPos, __global float* clAtomZPos, __global int* clAtomZNum, __constant float* clfParams, __global const int* clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma) \n"
"{		\n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1);	\n"
"	if(xid < width && yid < height) \n"
"	{	\n"
"		int Index = xid + width*yid; \n"
"		int topz = slice - loadSlicesZ; \n"
"		int bottomz = slice + loadSlicesZ; \n"
"		float sumz = 0.0f; \n"
"		if(topz < 0 ) \n"
"			topz = 0; \n"
"		if(bottomz >= slices ) \n"
"			bottomz = slices-1; \n"
"		for(int k = topz; k <= bottomz; k++) \n"
"		{ \n"
"			for (int j = floor((get_group_id(1) * get_local_size(1) * yBlocks * pixelscale/ ( MaxY-MinY )) - loadBlocksY ); j <= ceil(((get_group_id(1)+1) * get_local_size(1) * yBlocks * pixelscale/ ( MaxY - MinY )) + loadBlocksY); j++) \n"
"			{ \n"
"				for (int i = floor((get_group_id(0) * get_local_size(0) * xBlocks * pixelscale / (MaxX-MinX )) - loadBlocksX ); i <= ceil(((get_group_id(0)+1) * get_local_size(0) * xBlocks * pixelscale/ ( MaxX - MinX )) + loadBlocksX); i++) \n"
"				{ \n"
"				// Check bounds to avoid unneccessarily loading blocks when i am at edge of sample. \n"
"					if(0 <= j && j < yBlocks) \n"
"					{ \n"
"						// Check bounds to avoid unneccessarily loading blocks when i am at edge of sample. \n"
"						if (0 <= i && i < xBlocks ) \n"
"						{ \n"
"							// Check if there is an atom in bin, arrays are not overwritten when there are no extra atoms so if you don't check could add contribution more than once. \n"
"							for (int l = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + i]; l < clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + i+1]; l++) \n"
"							{ \n"
"								for (int h = 0; h < 10; h++) \n"
"								{ \n"
"									float rad = sqrt((xid*pixelscale-clAtomXPos[l] + MinX)*(xid*pixelscale-clAtomXPos[l] + MinX) + (yid*pixelscale-clAtomYPos[l] + MinY)*(yid*pixelscale-clAtomYPos[l] + MinY) + (z - (h*(dz/10.0f))-clAtomZPos[l])*(z - (h*(dz/10.0f))-clAtomZPos[l])); \n"
"									int ZNum = clAtomZNum[l]; \n"
"									if( rad < sqrt(5.0f)) // Should also make sure is not too small \n"
"									{ \n"
"										sumz += (150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+1]))); \n"
"										sumz += (150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12+2]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+2+1]))); \n"
"										sumz += (150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12+4]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+4+1]))); \n"
"										sumz += (266.5157269f * clfParams[(ZNum-1)*12+6] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+6+1]) * sqrt(clfParams[(ZNum-1)*12+6+1])/(clfParams[(ZNum-1)*12+6+1]*clfParams[(ZNum-1)*12+6+1]*clfParams[(ZNum-1)*12+6+1])); \n"
"										sumz += (266.5157269f * clfParams[(ZNum-1)*12+8] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+8+1]) * sqrt(clfParams[(ZNum-1)*12+8+1])/(clfParams[(ZNum-1)*12+8+1]*clfParams[(ZNum-1)*12+8+1]*clfParams[(ZNum-1)*12+8+1])); \n"
"										sumz += (266.5157269f * clfParams[(ZNum-1)*12+10] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+10+1]) * sqrt(clfParams[(ZNum-1)*12+10+1])/(clfParams[(ZNum-1)*12+10+1]*clfParams[(ZNum-1)*12+10+1]*clfParams[(ZNum-1)*12+10+1])); \n"
"									} \n"
"								} \n"
"							} \n"
"						} \n"
"					} \n"
"				} \n"
"			} \n"
"		} \n"
"		Potential[Index].x = cos(sigma*sumz*dz/10.0f); \n"
"		Potential[Index].y = sin(sigma*sumz*dz/10.0f); \n"
"	}	\n"
"}	\n"
;

const char* BinnedAtomicPotentialSource2 = 
"__kernel void clBinnedAtomicPotential(__global float2* Potential, __global float* clAtomXPos, __global float* clAtomYPos, __global float* clAtomZPos, __global int* clAtomZNum, __global float* clfParams, __local float* clfParamsl, __constant int* clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma) \n"
"{		\n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1);	\n"
"	if(xid < width && yid < height) \n"
"	{	\n"
"		int Index = xid + width*yid; \n"
"		int topz = slice - loadSlicesZ; \n"
"		int bottomz = slice + loadSlicesZ; \n"
"		float sumz = 0.0f; \n"
"		if(topz < 0 ) \n"
"			topz = 0; \n"
"		if(bottomz >= slices ) \n"
"			bottomz = slices-1; \n"
"		for(int k = topz; k <= bottomz; k++) \n"
"		{ \n"
"			for (int j = floor((get_group_id(1) * get_local_size(1) * yBlocks * pixelscale/ ( MaxY-MinY )) - loadBlocksY ); j <= ceil(((get_group_id(1)+1) * get_local_size(1) * yBlocks * pixelscale/ ( MaxY - MinY )) + loadBlocksY); j++) \n"
"			{ \n"
"				for (int i = floor((get_group_id(0) * get_local_size(0) * xBlocks * pixelscale / (MaxX-MinX )) - loadBlocksX ); i <= ceil(((get_group_id(0)+1) * get_local_size(0) * xBlocks * pixelscale/ ( MaxX - MinX )) + loadBlocksX); i++) \n"
"				{ \n"
"				// Check bounds to avoid unneccessarily loading blocks when i am at edge of sample. \n"
"					if(0 <= j && j < yBlocks) \n"
"					{ \n"
"						// Check bounds to avoid unneccessarily loading blocks when i am at edge of sample. \n"
"						if (0 <= i && i < xBlocks ) \n"
"						{ \n"
"							// Check if there is an atom in bin, arrays are not overwritten when there are no extra atoms so if you don't check could add contribution more than once. \n"
"							for (int l = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + i]; l < clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + i+1]; l++) \n"
"							{ \n"
"								for (int h = 0; h < 10; h++) \n"
"								{ \n"
"									float rad = sqrt((xid*pixelscale-clAtomXPos[l] + MinX)*(xid*pixelscale-clAtomXPos[l] + MinX) + (yid*pixelscale-clAtomYPos[l] + MinY)*(yid*pixelscale-clAtomYPos[l] + MinY) + (z - (h*(dz/10.0f))-clAtomZPos[l])*(z - (h*(dz/10.0f))-clAtomZPos[l])); \n"
"									int ZNum = clAtomZNum[l]; \n"
"									if( rad < sqrt(5.0f)) // Should also make sure is not too small \n"
"									{ \n"
"										sumz += (150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+1]))); \n"
"										sumz += (150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12+2]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+2+1]))); \n"
"										sumz += (150.4121417f * (1.0f/rad) * clfParams[(ZNum-1)*12+4]* exp( -2.0f*3.141592f*rad*sqrt(clfParams[(ZNum-1)*12+4+1]))); \n"
"										sumz += (266.5157269f * clfParams[(ZNum-1)*12+6] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+6+1]) * sqrt(clfParams[(ZNum-1)*12+6+1])/(clfParams[(ZNum-1)*12+6+1]*clfParams[(ZNum-1)*12+6+1]*clfParams[(ZNum-1)*12+6+1])); \n"
"										sumz += (266.5157269f * clfParams[(ZNum-1)*12+8] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+8+1]) * sqrt(clfParams[(ZNum-1)*12+8+1])/(clfParams[(ZNum-1)*12+8+1]*clfParams[(ZNum-1)*12+8+1]*clfParams[(ZNum-1)*12+8+1])); \n"
"										sumz += (266.5157269f * clfParams[(ZNum-1)*12+10] * exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+10+1]) * sqrt(clfParams[(ZNum-1)*12+10+1])/(clfParams[(ZNum-1)*12+10+1]*clfParams[(ZNum-1)*12+10+1]*clfParams[(ZNum-1)*12+10+1])); \n"
"									} \n"
"								} \n"
"							} \n"
"						} \n"
"					} \n"
"				} \n"
"			} \n"
"		} \n"
"		Potential[Index].x = cos(sigma*sumz*dz/10.0f); \n"
"		Potential[Index].y = sin(sigma*sumz*dz/10.0f); \n"
"	}	\n"
"}	\n"
;




const char* BandLimitSource = 
"__kernel void clBandLimit(__global float2* InputWavefunction, int width, int height, float kmax, __global float* kx, __global float* ky) \n"
"{		\n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1);	\n"
"	if(xid < width && yid < height) \n"
"	{	\n"
"		int Index = xid + width*yid; \n"
"		float k = hypot(kx[xid],ky[yid]); \n"
"		InputWavefunction[Index].x *= (k<=kmax); \n"
"		InputWavefunction[Index].y *= (k<=kmax); \n"
"	}	\n"
"}		\n"
;

const char* fftShiftSource = 
"__kernel void clfftShift(__global const float2* Input, __global float2* Output, int width, int height) \n"
"{        \n"
"    //Get the work items ID \n"
"    int xid = get_global_id(0);    \n"
"    int yid = get_global_id(1); \n"
"    if(xid < width && yid < height) \n"
"    {    \n"
"        int Index = xid + yid*width; \n"
"        int Yshift = width*height/2; \n"
"        int Xshift = width/2; \n"
"        int Xmid = width/2; \n"
"        int Ymid = height/2; \n"
"        if( xid < Xmid && yid < Ymid ) \n"
"        { \n"
"            Output[Index+Yshift+Xshift].x = Input[Index].x; \n"
"            Output[Index+Yshift+Xshift].y = Input[Index].y; \n"    
"        } \n"
"        else if( xid >= Xmid && yid < Ymid ) \n"
"        { \n"
"            Output[Index+Yshift-Xshift].x = Input[Index].x; \n"
"            Output[Index+Yshift-Xshift].y = Input[Index].y; \n"    
"        } \n"
"        else if( xid < Xmid && yid >= Ymid ) \n"
"        { \n"
"            Output[Index-Yshift+Xshift].x = Input[Index].x; \n"
"            Output[Index-Yshift+Xshift].y = Input[Index].y; \n"    
"        } \n"
"        else if( xid >= Xmid && yid >= Ymid ) \n"
"        { \n"
"            Output[Index-Yshift-Xshift].x = Input[Index].x; \n"
"            Output[Index-Yshift-Xshift].y = Input[Index].y; \n"    
"        } \n"    
"    }    \n"
"}    \n"
;

const char* imagingKernelSource = 
"__kernel void clImagingKernel(__global const float2* Input, __global float2* Output, int width, int height, float Cs, float df, float a2, float a2phi, float a3, float a3phi, float objap, float wavel, __global float* clXFrequencies, __global float* clYFrequencies, float beta, float delta) \n"
"{        \n"
"    //Get the work items ID \n"
"    int xid = get_global_id(0); \n"
"    int yid = get_global_id(1); \n"
"    if(xid < width && yid < height) \n"
"    {  \n"
"       int Index = xid + yid*width; \n"
"		float objap2 = (((objap * 0.001f) / wavel ) * (( objap * 0.001f ) / wavel )); \n"
"		float Chi1 = 3.14159f * wavel; \n"
"		float Chi2 = 0.5f * Cs * wavel * wavel; \n"
"		float k2 = (clXFrequencies[xid]*clXFrequencies[xid]) + (clYFrequencies[yid]*clYFrequencies[yid]); \n"
"		float k = sqrt(k2); \n"
"		float factor = 1.0f*beta*beta/(4*wavel*wavel); \n"
"		float ecohs = exp(-factor*pow(3.14159f*k*wavel*2*df + 2*3.14159f*wavel*wavel*wavel*Cs*k2*k,2)); \n"
"		float ecohd = exp(-0.25f*delta*delta*3.14159f*3.14159f*k2*k2*wavel*wavel); \n"
"		if ( k2 < objap2){ \n"
"			float Phi = atan2(clYFrequencies[yid],clXFrequencies[xid]); \n"
"			float Chi = Chi1 * k2 * ( Chi2 * k2 + df + a2 * sin ( ( 2.0f * ( Phi - a2phi ) ) ) + 2.0f * a3 * wavel * sqrt ( k2 ) * sin ( ( 3.0f * ( Phi - a3phi ) ) ) / 3.0f ); \n"
"			Output[Index].x = ecohs*ecohd*(Input[Index].x *  cos ( Chi )  + Input[Index].y * sin ( Chi )) ; \n"
"			Output[Index].y	= ecohs*ecohd*(Input[Index].x * -1 *  sin ( Chi ) + Input[Index].y * cos ( Chi )); \n"
"		} else { \n"
"		Output[Index].x = 0.0f; \n"
"		Output[Index].y = 0.0f; \n"
"		} \n"
"	} \n"
"} \n"
;


__global__ void STEMinitializingKernel(cuComplex * in1, float* xFrequencies, float* yFrequencies, int posx, int posy, float apert, float pixelscale, float df, float Cs, float wavel)
{

	int xIndex = threadIdx.x + blockIdx.x * blockDim.x;
	int yIndex = threadIdx.y + blockIdx.y * blockDim.y;
	
	if (xIndex >= res[0] || yIndex >= res[1])
		return ;
		
	int Index = (yIndex * res[0] + xIndex);

	float k0x = xFrequencies[xIndex];
	float k0y = yFrequencies[yIndex];
	float k = sqrtf(k0x*k0x + k0y*k0y) ;
	float Pi = 3.14159265f;
	
	if( k < apert)
	{
		//in1[Index].x 	= cosf(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*cosf(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale));
		//in1[Index].y 	= -sinf(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*sinf(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale));
		in1[Index].x 	= cosf(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*cosf(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale))  + sinf(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*sinf(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale)) ;
		in1[Index].y 	= -cosf(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale))*sinf(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df)) + cosf(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*sinf(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale));
	}
	else
	{
		in1[Index].x 	= 0.0f;
		in1[Index].y 	= 0.0f;
	}
	
  }