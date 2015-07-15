#include "clKernelCodes2.h"

#pragma once
#include <string>

// test using raw string literal for slightly cleaner code.
const std::string InitialiseWavefunctionSource =
R"(__kernel void clInitialiseWavefunction(global float2* InputWavefunction, int width, int height, float value)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	if(xid < width && yid < height)
	{
		int Index = xid + width*yid;
		InputWavefunction[Index].x = value;
		InputWavefunction[Index].y = 0;
	}
}
)"
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
const char* gradsource =
"__kernel void clGrad( __global float2* Input, __global float* restrict clXFrequencies, \n"
"						__global float* restrict clYFrequencies,\n"
"						int width, int height)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	int Index = xid + width* yid;\n"
"\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		Input[Index].x *= -4.0f * 3.1415129f * 3.141592f * (clXFrequencies[xid]*clXFrequencies[xid] + clYFrequencies[yid]*clYFrequencies[yid]);\n"
"		Input[Index].y *= -4.0f * 3.1415129f * 3.141592f * (clXFrequencies[xid]*clXFrequencies[xid] + clYFrequencies[yid]*clYFrequencies[yid]);\n"
"	}\n"
"}\n";

const char* fdsource =
"__kernel void clFiniteDifference(__global float2* restrict Potential, __global float2* restrict Grad, __global float2* restrict PsiMinus, __global float2* restrict Psi, __global float2* restrict PsiPlus, \n"
"										float FDdz, float wavelength, float sigma, int width, int height)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	int Index = xid + width* yid;\n"
"\n"
"	float2 cMinus = {1 , -2*3.14159f*FDdz/wavelength}; \n"
"	float2 cPlus = {1 , 2*3.14159f*FDdz/wavelength};\n"
"\n"
"	float2 reciprocalCPlus = {cMinus.x / (cMinus.x*cMinus.x + cMinus.y*cMinus.y),cMinus.y / (cMinus.x*cMinus.x + cMinus.y*cMinus.y)};\n"
"	float2 cMinusOvercPlus = {(cPlus.x*cPlus.x - cPlus.y*cPlus.y) / (cMinus.x*cMinus.x + cMinus.y*cMinus.y),-2*(cPlus.x*cPlus.y) / (cMinus.x*cMinus.x + cMinus.y*cMinus.y)};\n"
"\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		float real = reciprocalCPlus.x*(2*Psi[Index].x-FDdz*FDdz*Grad[Index].x - FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].x/wavelength)\n"
"				-reciprocalCPlus.y*(2*Psi[Index].y-FDdz*FDdz*Grad[Index].y -  FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].y/wavelength)\n"
"				-cMinusOvercPlus.x*(PsiMinus[Index].x) + cMinusOvercPlus.y*(PsiMinus[Index].y);\n"
"\n"
"		float imag = reciprocalCPlus.y*(2*Psi[Index].x-FDdz*FDdz*Grad[Index].x - FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].x/wavelength)\n"
"				+reciprocalCPlus.x*(2*Psi[Index].y-FDdz*FDdz*Grad[Index].y -  FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].y/wavelength)\n"
"				-cMinusOvercPlus.y*(PsiMinus[Index].x) - cMinusOvercPlus.x*(PsiMinus[Index].y);\n"
"\n"
"		PsiPlus[Index].x = real;\n"
"		PsiPlus[Index].y = imag;\n"
"	}\n"
"}\n";

// see Rolf Erni's book, Kirklands book and maybe the SuperSTEM book for details
// Need to test the behavious of float2 (i.e. addition etc.i)
const std::string imagingKernelSource =
R"(float cModSq(float2 a)
{
	return (a.x*a.x + a.y*a.y);
}

float2 cMult(float2 a, float2 b)
{
	return (float2)(a.x*b.x - a.y*b.y, a.x*b.y + a.y*b.x);
}

float2 cConj(float2 a)
{
	return (float2)(a.x, -a.y);
}

float2 cPow(float2 a, int n)
{
	float2 temp = a;
	for (int j=1; j < n; j++)
	{
		temp = cMult(temp, a);
	}
	return temp;
}


__kernel void clImagingKernel(__global const float2* Input, __global float2* Output, int width, int height,
__global float* clXFrequencies, __global float* clYFrequencies,
float wavel,
float C10, float2 C12,
float2 C21, float2 C23,
float C30, float2 C32, float2 C34,
float2 C41, float2 C43, float2 C45,
float C50, float2 C52, float2 C54, float2 C56,
float objap, float beta, float delta)
{
	//Get the work items ID
    int xid = get_global_id(0);
    int yid = get_global_id(1);
	if(xid < width && yid < height)
	{
		int Index = xid + yid*width;
		float objap2 = (objap * 0.001f) / wavel;
		float k = sqrt((clXFrequencies[xid]*clXFrequencies[xid]) + (clYFrequencies[yid]*clYFrequencies[yid]));
		if (k < objap2)
		{
			float2 w = (float2)(wavel*clXFrequencies[xid], wavel*clYFrequencies[yid]);
			float2 wc = cConj(w);
			
			float temporalCoh = exp( -0.5f * M_PI_F*M_PI_F  * delta*delta * cModSq(w)*cModSq(w) / (wavel*wavel) );

			float spatialCoh = exp( -1.0f * M_PI_F*M_PI_F * beta*beta * cModSq(w) * pow((C10 + C30*cModSq(w) + C50*cModSq(w)*cModSq(w)), 2)  / (wavel*wavel) );

			float tC10 = 0.5f * C10 * cModSq(w);
			float2 tC12 = 0.5f * cMult(C12, cPow(wc, 2));

			float2 tC21 = cMult(C12, cMult(cPow(wc, 2), w)) / 3.0f;
			float2 tC23 = cMult(C23, cPow(wc, 3)) / 3.0f;

			float tC30 = 0.25f * C30 * cModSq(w)*cModSq(w);
			float2 tC32 = 0.25f * cMult(C32, cMult(cPow(wc, 3), w));
			float2 tC34 = 0.25f * cMult(C34, cPow(wc, 4));
		
			float2 tC41 = 0.2f * cMult(C41, cMult(cPow(wc, 3), cPow(w ,2)));
			float2 tC43 = 0.2f * cMult(C43, cMult(cPow(wc, 4), w));
			float2 tC45 = 0.2f * cMult(C45, cPow(wc, 5));

			float tC50 = C50 * cModSq(w)*cModSq(w)*cModSq(w) / 6.0f; 
			float2 tC52 = cMult(C52, cMult(cPow(wc, 4), cPow(w ,2))) / 6.0f;
			float2 tC54 = cMult(C54, cMult(cPow(wc, 5), w)) / 6.0f;
			float2 tC56 = cMult(C56, cPow(wc, 6)) / 6.0f;
		
			float cchi = tC10 + tC12.x + tC21.x + tC23.x + tC30 + tC32.x + tC34.x + tC41.x + tC43.x + tC45.x + tC50 + tC52.x + tC54.x + tC56.x;
			float chi = 2.0f * M_PI_F * cchi / wavel;

			Output[Index].x = temporalCoh * spatialCoh * ( Input[Index].x * cos(chi) + Input[Index].y * sin(chi) );
			Output[Index].y = temporalCoh * spatialCoh * ( Input[Index].y * cos(chi) - Input[Index].x * sin(chi) );
		}
		else
		{
			Output[Index].x = 0.0f;
			Output[Index].y = 0.0f;
		}
	}
}
)"
;



const std::string InitialiseSTEMWavefunctionSourceTest =
R"(float cModSq(float2 a)
{
	return (a.x*a.x + a.y*a.y);
}

float2 cMult(float2 a, float2 b)
{
	return (float2)(a.x*b.x - a.y*b.y, a.x*b.y + a.y*b.x);
}

float2 cConj(float2 a)
{
	return (float2)(a.x, -a.y);
}

float2 cPow(float2 a, int n)
{
	float2 temp = a;
	for (int j=1; j < n; j++)
	{
		temp = cMult(temp, a);
	}
	return temp;
}

__kernel void clInitialiseSTEMWavefunction(__global float2* Output, int width, int height,
__global const float* clXFrequencies, __global const float* clYFrequencies, 
float posx, float posy, float pixelscale,
float wavel,
float C10, float2 C12,
float2 C21, float2 C23,
float C30, float2 C32, float2 C34,
float2 C41, float2 C43, float2 C45,
float C50, float2 C52, float2 C54, float2 C56,
float condap)
{
	// Get the work items ID
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	if(xid < width && yid < height)
	{
		int Index = xid + yid*width;
		float condap2 = (condap * 0.001f) / wavel;
		float k = sqrt((clXFrequencies[xid]*clXFrequencies[xid]) + (clYFrequencies[yid]*clYFrequencies[yid]));
		if (k < condap2)
		{			
			// this term is easier to calcualte once before it is put into the exponential
			float posTerm = 2.0f * M_PI_F * (clXFrequencies[xid]*posx*pixelscale + clYFrequencies[yid]*posy*pixelscale);

			float2 w = (float2)(wavel*clXFrequencies[xid], wavel*clYFrequencies[yid]);
			float2 wc = cConj(w);

			// all the aberration terms, calculated in w (omega)
			float tC10 = 0.5f * C10 * cModSq(w);
			float2 tC12 = 0.5f * cMult(C12, cPow(wc, 2));

			float2 tC21 = cMult(C12, cMult(cPow(wc, 2), w)) / 3.0f;
			float2 tC23 = cMult(C23, cPow(wc, 3)) / 3.0f;

			float tC30 = 0.25f * C30 * cModSq(w)*cModSq(w);
			float2 tC32 = 0.25f * cMult(C32, cMult(cPow(wc, 3), w));
			float2 tC34 = 0.25f * cMult(C34, cPow(wc, 4));
		
			float2 tC41 = 0.2f * cMult(C41, cMult(cPow(wc, 3), cPow(w ,2)));
			float2 tC43 = 0.2f * cMult(C43, cMult(cPow(wc, 4), w));
			float2 tC45 = 0.2f * cMult(C45, cPow(wc, 5));

			float tC50 = C50 * cModSq(w)*cModSq(w)*cModSq(w) / 6.0f; 
			float2 tC52 = cMult(C52, cMult(cPow(wc, 4), cPow(w ,2))) / 6.0f;
			float2 tC54 = cMult(C54, cMult(cPow(wc, 5), w)) / 6.0f;
			float2 tC56 = cMult(C56, cPow(wc, 6)) / 6.0f;
		
			float cchi = tC10 + tC12.x + tC21.x + tC23.x + tC30 + tC32.x + tC34.x + tC41.x + tC43.x + tC45.x + tC50 + tC52.x + tC54.x + tC56.x;
			float chi = 2.0f * M_PI_F * cchi / wavel;
			
			Output[Index].x = cos(chi) * cos(posTerm) + sin(chi) * sin(posTerm);
			Output[Index].y = cos(chi) * sin(posTerm) - sin(chi) * cos(posTerm);
		}
		else
		{
			Output[Index].x = 0.0f;
			Output[Index].y = 0.0f;
		}
	}
}
)"
;

const char* InitialiseSTEMWavefunctionSource =
"__kernel void clInitialiseSTEMWavefunction(__global float2* Output, int width, int height, __global const float* clXFrequencies, __global const float* clYFrequencies, float posx, float posy, float apert, float pixelscale, float df, float Cs, float wavel) \n"
"{ \n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0); \n"
"	int yid = get_global_id(1); \n"
"	if(xid < width && yid < height) \n"
"	{ \n"
"		int Index = xid + yid*width; \n"
"		float apert2 = ((apert * 0.001f) / wavel ); \n"
"		float k0x = clXFrequencies[xid]; \n"
"		float k0y = clYFrequencies[yid]; \n"
"		float k = sqrt(k0x*k0x + k0y*k0y); \n"
"		float Pi = 3.14159265f; \n"
"		if( k < apert2) \n"
"		{ \n"
"			Output[Index].x = cos(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*cos(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale))  + sin(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*sin(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale)) ; \n"
"			Output[Index].y = -cos(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale))*sin(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df)) + cos(Pi*wavel*k*k*(Cs*wavel*wavel*k*k*0.5f + df))*sin(2*Pi*(k0x*posx*pixelscale + k0y*posy*pixelscale)); \n"
"		} \n"
"		else \n"
"		{ \n"
"			Output[Index].x 	= 0.0f; \n"
"			Output[Index].y 	= 0.0f; \n"
"		} \n"
"	} \n"
"} \n"
;

const char* sumReductionsource2 =
"__kernel void clSumReduction(__global const float2* input, __global float2* output, const unsigned int size, __local float2* buffer)	\n"
"{																																		\n"
"	//Get the work items ID																												\n"
"	size_t idx = get_local_id(0);																										\n"
"	size_t stride = get_global_size(0);																									\n"
"	buffer[idx] = 0;																													\n"
"																																		\n"
"	for(size_t pos = get_global_id(0); pos < size; pos += stride )																		\n"
"		buffer[idx] += input[pos];																										\n"
"																																		\n"
"	barrier(CLK_LOCAL_MEM_FENCE);																										\n"
"																																		\n"
"	float sum = 0;																														\n"
"	if(!idx) {																															\n"
"		for(size_t i = 1; i < get_local_size(0); ++i)																					\n"
"			sum += sqrt(buffer[i].x*buffer[i].x + buffer[i].y*buffer[i].y);																											\n"
"																																		\n"
"		output[get_group_id(0)].x = sum;																								\n"
"		output[get_group_id(0)].y = 0.0f;																								\n"
"	}																																	\n"
"}																																		\n"
;

const char* floatSumReductionsource2 =
"__kernel void clFloatSumReduction(__global const float* restrict input, __global float* restrict output, const unsigned int size, __local float* restrict buffer)	\n"
"{																																		\n"
"	//Get the work items ID																												\n"
"	size_t idx = get_local_id(0);																										\n"
"	size_t stride = get_global_size(0);																									\n"
"	buffer[idx] = 0;																													\n"
"																																		\n"
"	for(size_t pos = get_global_id(0); pos < size; pos += stride )																		\n"
"		buffer[idx] += input[pos];																										\n"
"																																		\n"
"	barrier(CLK_LOCAL_MEM_FENCE);																										\n"
"																																		\n"
"	float sum = 0;																														\n"
"	if(!idx) {																															\n"
"		for(size_t i = 1; i < get_local_size(0); ++i)																					\n"
"			sum += buffer[i];																											\n"
"																																		\n"
"		output[get_group_id(0)] = sum;																								\n"
"	}																																	\n"
"}																																		\n"
;
const char* multisource =
"__kernel void clComplexMultiply(__global float2* In1, __global float2* In2, __global float2* Out, int width, int height)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		int Index = xid + width*yid;\n"
"\n"
"		Out[Index].x = In1[Index].x * In2[Index].x - In1[Index].y * In2[Index].y;\n"
"		Out[Index].y = In1[Index].x * In2[Index].y + In1[Index].y * In2[Index].x;\n"
"		\n"
"	}\n"
"}\n";

const char* abssource2 =
"__kernel void clAbs(__global float2* clEW, int sizeX, int sizeY)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<sizeX&&yid<sizeY) \n"
"	{	\n"
"		int Index = xid + yid*sizeX; \n"
"		float real = clEW[Index].x;	\n"
"		float imag = clEW[Index].y;	\n"
"		clEW[Index].x = hypot(real,imag)*hypot(real,imag);	\n"
"		clEW[Index].y = 0;	\n"
"	}	\n"
"}	\n"
;

const char* multiplySource =
"__kernel void clMultiply(__global float2* Input, float factor, int sizeX, int sizeY)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<sizeX&&yid<sizeY) \n"
"	{	\n"
"		int Index = xid + yid*sizeX; \n"
"		Input[Index].x *= factor; \n"
"		Input[Index].y *= factor; \n"
"	}	\n"
"}	\n"
;

const char* bandPassSource =
"__kernel void clBandPass(__global float2* Output, __global const float2* Input, int width, int height, float inner, float outer)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<width && yid<height) \n"
"	{	\n"
"		int Index = xid + yid*width; \n"
"		float centX = width/2; \n"
"		float centY = height/2; \n"
"		float radius = sqrt((xid-centX)*(xid-centX)+(yid-centY)*(yid-centY)); \n" // hypot?
"		if(radius < outer && radius > inner) \n"
"		{	\n"
"			Output[Index].x = Input[Index].x; \n"
"			Output[Index].y = Input[Index].y; \n"
"		} \n"
"		else \n"
"		{	\n"
"			Output[Index].x = 0; \n"
"			Output[Index].y = 0; \n"
"		} \n"
"	}	\n"
"}	\n"
;

const char* floatbandPassSource =
"__kernel void clFloatBandPass(__global float* restrict Output, __global const float* restrict Input, int width, int height, float inner, float outer, float xc, float yc)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<width && yid<height) \n"
"	{	\n"
"		int Index = xid + yid*width; \n"
"       float centX = width/2 + xc; \n"
"       float centY = height/2 + yc; \n"
"       float radius = hypot(xid-centX,yid-centY); \n"
"		Output[Index] = (radius < outer && radius > inner) * Input[Index];\n"
"	}	\n"
"}	\n"
;

const char* floatabsbandPassSource =
"__kernel void clFloatAbsBandPass(__global float* restrict Output, __global const float2* restrict Input, int width, int height, float inner, float outer, float xc, float yc)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<width && yid<height) \n"
"	{	\n"
"		int Index = xid + yid*width; \n"
"       float centX = width/2 + xc; \n"
"       float centY = height/2 + yc; \n"
"       float radius = hypot(xid-centX,yid-centY); \n"
"		Output[Index] = (radius < outer && radius > inner) * hypot(Input[Index].x, Input[Index].y) * hypot(Input[Index].x, Input[Index].y);\n" // wants to be abs^2 ??
"	}	\n"
"}	\n"
;

const char* SqAbsSource =
"__kernel void clSqAbs(__global const float2* clIm, __global float2* clAbsSq, int sizeX, int sizeY)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<sizeX&&yid<sizeY) \n"
"	{	\n"
"		int Index = xid + yid*sizeX; \n"
"		float real = clIm[Index].x;	\n"
"		float imag = clIm[Index].y;	\n"
"		clAbsSq[Index].x = real*real + imag*imag;	\n"
"		clAbsSq[Index].y = 0;	\n"
"	}	\n"
"}	\n"
;

const char* DQESource =
"__kernel void clDQE(__global const float2* clIm, __global float* DQE, int sizeX, int sizeY, int binning)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<sizeX&&yid<sizeY) \n"
"	{	\n"
"		int Index = xid + yid*sizeX; \n"
"		int midx; \n"
"		int midy; \n"
"		if(xid < sizeX/2) \n"
"			midx=0; \n"
"		else \n"
"			midx=sizeX; \n"
"		if(yid < sizeY/2) \n"
"			midy=0; \n"
"		else \n"
"			midy=sizeY; \n"
"		float xp = xid - midx; \n"
"		float yp = yid - midy; \n"
"		float rad = hypot(xp,yp); \n"
"		int dqe = floor(rad/binning); \n"
"		int dqe2 = dqe+1; \n"
"		float dqeval = DQE[min(dqe,724)]; \n"
"		float dqeval2 = DQE[min(dqe2,724)]; \n"
"		float interp = rad/binning - floor(rad/binning); \n"
"		float finaldqe = interp * dqeval2 + (1-interp)*dqeval; \n"
"		float real = clIm[Index].x;	\n"
"		float imag = clIm[Index].y;	\n"
"		clIm[Index].x = real*sqrt(finaldqe);	\n"
"		clIm[Index].y = imag*sqrt(finaldqe);	\n"
"	}	\n"
"}	\n"
;

const char* NTFSource =
"__kernel void clNTF(__global const float2* clIm, __global float* NTF, int sizeX, int sizeY, int binning)	\n"
"{	\n"
"	//Get the work items ID \n"
"	int xid = get_global_id(0);	\n"
"	int yid = get_global_id(1); \n"
"	\n"
"	if(xid<sizeX&&yid<sizeY) \n"
"	{	\n"
"		int Index = xid + yid*sizeX; \n"
"		int midx; \n"
"		int midy; \n"
"		if(xid < sizeX/2) \n"
"			midx=0; \n"
"		else \n"
"			midx=sizeX; \n"
"		if(yid < sizeY/2) \n"
"			midy=0; \n"
"		else \n"
"			midy=sizeY; \n"
"		float xp = xid - midx; \n"
"		float yp = yid - midy; \n"
"		float rad = hypot(xp,yp); \n"
"		int ntf = floor(rad/binning); \n"
"		int ntf2 = ntf+1; \n"
"		float ntfval = NTF[min(ntf,724)]; \n"
"		float ntfval2 = NTF[min(ntf2,724)]; \n"
"		float interp = rad/binning - floor(rad/binning); \n"
"		float finalntf = interp * ntfval2 + (1-interp)*ntfval; \n"
"		float real = clIm[Index].x;	\n"
"		float imag = clIm[Index].y;	\n"
"		clIm[Index].x = real*finalntf;	\n"
"		clIm[Index].y = imag*finalntf;	\n"
"	}	\n"
"}	\n"
;
const char* opt2source =
"__kernel void clBinnedAtomicPotentialOpt(__global float2* Potential, \n"
"										  __global const float* restrict clAtomXPos, \n"
"										  __global const float* restrict clAtomYPos, \n"
"										  __global const float* restrict clAtomZPos, \n"
"										  __global const int* restrict clAtomZNum, \n"
"										  __constant float* clfParams, \n"
"										  __global const int* restrict clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma,\n"
"										  float startx, float starty, int full3dints)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	int lid = get_local_id(0) + get_local_size(0)*get_local_id(1);\n"
"	int Index = xid + width*yid;\n"
"	int topz = slice - loadSlicesZ;\n"
"	int bottomz = slice + loadSlicesZ;\n"
"	float sumz = 0.0f;\n"
"	int gx = get_group_id(0);\n"
"	int gy = get_group_id(1);\n"
"	if(topz < 0 )\n"
"		topz = 0;\n"
"	if(bottomz >= slices )\n"
"		bottomz = slices-1;\n"
"\n"
"	__local float atx[256];\n"
"	__local float aty[256];\n"
"	__local float atz[256];\n"
"	__local int atZ[256];\n"
"\n"
"	float integrals = full3dints;\n"
"\n"
"	int startj = fmax(floor(((starty + gy * get_local_size(1) * pixelscale) * yBlocks ) / (MaxY-MinY)) - loadBlocksY,0) ;\n"
"	int endj = fmin(ceil(((starty + (gy+1) * get_local_size(1) * pixelscale) * yBlocks) / (MaxY-MinY)) + loadBlocksY,yBlocks-1);\n"
"	int starti = fmax(floor(((startx + gx * get_local_size(0) * pixelscale) * xBlocks)  / (MaxX-MinX)) - loadBlocksX,0) ;\n"
"	int endi = fmin(ceil(((startx + (gx+1) * get_local_size(0) * pixelscale) * xBlocks) / (MaxX-MinX)) + loadBlocksX,xBlocks-1);\n"
"\n"
"	for(int k = topz; k <= bottomz; k++)\n"
"	{\n"
"		for (int j = startj ; j <= endj; j++)\n"
"		{\n"
"			//Need list of atoms to load, so we can load in sequence\n"
"			int start = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + starti];\n"
"			int end = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + endi + 1];\n"
"				\n"
"			int gid = start + lid;\n"
"\n"
"			if(lid < end-start)\n"
"			{\n"
"				atx[lid] = clAtomXPos[gid];\n"
"				aty[lid] = clAtomYPos[gid];\n"
"				atz[lid] = clAtomZPos[gid];\n"
"				atZ[lid] = clAtomZNum[gid];\n"
"			}\n"
"\n"
"			barrier(CLK_LOCAL_MEM_FENCE);\n"
"\n"
"			float p2=0;\n"
"			for (int l = 0; l < end-start; l++) \n"
"			{\n"
"				float xyrad2 = (startx + xid*pixelscale-atx[l])*(startx + xid*pixelscale-atx[l]) + (starty + yid*pixelscale-aty[l])*(starty + yid*pixelscale-aty[l]);\n"
"\n"
"				int ZNum = atZ[l];\n"
"				for (int h = 0; h <= full3dints; h++)\n"
"				{\n"
"					float rad = native_sqrt(xyrad2 + (z - h*dz/integrals -atz[l])*(z - h*dz/integrals-atz[l]));\n"
"\n"
"					if(rad < 0.25f * pixelscale)\n"
"						rad = 0.25f * pixelscale;\n"
"\n"
"					float p1 = 0;\n"
"\n"
"					if( rad < 3.0f) // Should also make sure is not too small\n"
"					{\n"
"						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12  ]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+1  ])));\n"
"						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+2+1])));\n"
"						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+4+1])));\n"
"						p1 += (266.5157269f * clfParams[(ZNum-1)*12+6] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+6+1]) * native_powr(clfParams[(ZNum-1)*12+6+1],-1.5f));\n"
"						p1 += (266.5157269f * clfParams[(ZNum-1)*12+8] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+8+1]) * native_powr(clfParams[(ZNum-1)*12+8+1],-1.5f));\n"
"						p1 += (266.5157269f * clfParams[(ZNum-1)*12+10] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+10+1]) * native_powr(clfParams[(ZNum-1)*12+10+1],-1.5f));\n"
"\n"
"						sumz += (h!=0) * (p1+p2)*0.5f;\n"
"						p2 = p1;\n"
"						//sumz +=p1;\n"
"					}\n"
"				}\n"
"			}\n"
"\n"
"			barrier(CLK_LOCAL_MEM_FENCE);\n"
"		}\n"
"	}\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		Potential[Index].x = native_cos((dz/integrals)*sigma*sumz);\n"
"		Potential[Index].y = native_sin((dz/integrals)*sigma*sumz);\n"
"	}\n"
"}\n"
;
const char* fd2source =
"__kernel void clBinnedAtomicPotentialOptFD(__global float2* Potential, \n"
"										  __global const float* restrict clAtomXPos, \n"
"										  __global const float* restrict clAtomYPos, \n"
"										  __global const float* restrict clAtomZPos, \n"
"										  __global const int* restrict clAtomZNum, \n"
"										  __constant float* clfParams, \n"
"										  __global const int* restrict clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma,\n"
"										  float startx, float starty)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	int lid = get_local_id(0) + get_local_size(0)*get_local_id(1);\n"
"	int Index = xid + width*yid;\n"
"	int topz = slice - loadSlicesZ;\n"
"	int bottomz = slice + loadSlicesZ;\n"
"	float sumz = 0.0f;\n"
"	int gx = get_group_id(0);\n"
"	int gy = get_group_id(1);\n"
"	if(topz < 0 )\n"
"		topz = 0;\n"
"	if(bottomz >= slices )\n"
"		bottomz = slices-1;\n"
"\n"
"	__local float atx[256];\n"
"	__local float aty[256];\n"
"	__local float atz[256];\n"
"	__local int atZ[256];\n"
"\n"
"	int startj = fmax(floor(((starty + gy * get_local_size(1) * pixelscale) * yBlocks ) / (MaxY-MinY)) - loadBlocksY,0) ;\n"
"	int endj = fmin(ceil(((starty + (gy+1) * get_local_size(1) * pixelscale) * yBlocks) / (MaxY-MinY)) + loadBlocksY,yBlocks-1);\n"
"	int starti = fmax(floor(((startx + gx * get_local_size(0) * pixelscale) * xBlocks)  / (MaxX-MinX)) - loadBlocksX,0) ;\n"
"	int endi = fmin(ceil(((startx + (gx+1) * get_local_size(0) * pixelscale) * xBlocks) / (MaxX-MinX)) + loadBlocksX,xBlocks-1);\n"
"\n"
"	for(int k = topz; k <= bottomz; k++)\n"
"	{\n"
"		for (int j = startj ; j <= endj; j++)\n"
"		{\n"
"			//Need list of atoms to load, so we can load in sequence\n"
"			int start = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + starti];\n"
"			int end = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + endi + 1];\n"
"				\n"
"			int gid = start + lid;\n"
"\n"
"			if(lid < end-start)\n"
"			{\n"
"				atx[lid] = clAtomXPos[gid];\n"
"				aty[lid] = clAtomYPos[gid];\n"
"				atz[lid] = clAtomZPos[gid];\n"
"				atZ[lid] = clAtomZNum[gid];\n"
"			}\n"
"\n"
"			barrier(CLK_LOCAL_MEM_FENCE);\n"
"\n"
"			for (int l = 0; l < end-start; l++) \n"
"			{\n"
"				float xyrad2 = (startx + xid*pixelscale-atx[l])*(startx + xid*pixelscale-atx[l]) + (starty + yid*pixelscale-aty[l])*(starty + yid*pixelscale-aty[l]);\n"
"\n"
"				int ZNum = atZ[l];\n"
"\n"
"					float rad = native_sqrt(xyrad2 + (z-atz[l])*(z-atz[l]));\n"
"\n"
"					if(rad < 0.25f * pixelscale)\n"
"						rad = 0.25f * pixelscale;\n"
"\n"
"					float p1 = 0;\n"
"\n"
"					if( rad < 3.0f) // Should also make sure is not too small\n"
"					{\n"
"						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12  ]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+1  ])));\n"
"						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12+2]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+2+1])));\n"
"						p1 += (150.4121417f * native_recip(rad) * clfParams[(ZNum-1)*12+4]* native_exp( -2.0f*3.141592f*rad*native_sqrt(clfParams[(ZNum-1)*12+4+1])));\n"
"						p1 += (266.5157269f * clfParams[(ZNum-1)*12+6] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+6+1]) * native_powr(clfParams[(ZNum-1)*12+6+1],-1.5f));\n"
"						p1 += (266.5157269f * clfParams[(ZNum-1)*12+8] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+8+1]) * native_powr(clfParams[(ZNum-1)*12+8+1],-1.5f));\n"
"						p1 += (266.5157269f * clfParams[(ZNum-1)*12+10] * native_exp (-3.141592f*rad*3.141592f*rad/clfParams[(ZNum-1)*12+10+1]) * native_powr(clfParams[(ZNum-1)*12+10+1],-1.5f));\n"
"\n"
"						sumz +=p1;\n"
"					}\n"
"\n"
"			}\n"
"\n"
"			barrier(CLK_LOCAL_MEM_FENCE);\n"
"		}\n"
"	}\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		Potential[Index].x = sumz;\n"
"		Potential[Index].y = 0;\n"
"	}\n"
"}\n"
;
const char* conv2source =
"float bessi0(float x)\n"
"{\n"
"	int i;\n"
"	float ax, sum, t;\n"
"\n"
"	float i0a[] = { 1.0f, 3.5156229f, 3.0899424f, 1.2067492f, 0.2659732f, 0.0360768f, 0.0045813f };\n"
"\n"
"	float i0b[] = { 0.39894228f, 0.01328592f, 0.00225319f, -0.00157565f, 0.00916281f, -0.02057706f, 0.02635537f, -0.01647633f, 0.00392377f};\n"
"\n"
"	ax = fabs( x );\n"
"\n"
"	if( ax <= 3.75f ) \n"
"	{\n"
"		t = x / 3.75f;\n"
"		t = t * t;\n"
"		sum = i0a[6];\n"
"\n"
"		for( i=5; i>=0; i--) sum = sum*t + i0a[i]; \n"
"\n"
"	} else\n"
"	{\n"
"		t = 3.75f / ax;\n"
"		sum = i0b[8];\n"
"		for( i=7; i>=0; i--) sum = sum*t + i0b[i];\n"
"		sum = native_exp( ax ) * sum / native_sqrt( ax );\n"
"	}\n"
"\n"
"	return sum;\n"
"}\n"
"\n"
"float bessk0(float x)\n"
"{\n"
"	int i;\n"
"	float ax, x2, sum;\n"
"	float k0a[] = { -0.57721566f, 0.42278420f, 0.23069756f, 0.03488590f, 0.00262698f, 0.00010750f, 0.00000740f };\n"
"	float k0b[] = { 1.25331414f, -0.07832358f, 0.02189568f, -0.01062446f, 0.00587872f, -0.00251540f, 0.00053208f };\n"
"\n"
"	ax = fabs( x );\n"
"\n"
"	if( (ax > 0.0f)  && ( ax <=  2.0f ) )\n"
"	{\n"
"		x2 = ax/2.0f;\n"
"		x2 = x2 * x2;\n"
"		sum = k0a[6];\n"
"		for( i=5; i>=0; i--) sum = sum*x2 + k0a[i];\n"
"		sum = -log(ax/2.0f) * bessi0(x) + sum;\n"
"\n"
"	} else if( ax > 2.0f ) \n"
"	{\n"
"		x2 = 2.0f/ax;\n"
"		sum = k0b[6];\n"
"\n"
"		for( i=5; i>=0; i--) sum = sum*x2 + k0b[i];\n"
"\n"
"		sum = native_exp( -ax ) * sum / native_sqrt( ax );\n"
"\n"
"	} else sum = 1.0e20;\n"
"\n"
"	return sum;\n"
"}\n"
"\n"
"__kernel void clBinnedAtomicPotentialConventional(__global float2* Potential, \n"
"										  __global const float* restrict clAtomXPos, \n"
"										  __global const float* restrict clAtomYPos, \n"
"										  __global const float* restrict clAtomZPos, \n"
"										  __global const int* restrict clAtomZNum, \n"
"										  __constant float* clfParams, \n"
"										  __global const int* restrict clBlockStartPositions, int width, int height, int slice, int slices, float z, float dz, float pixelscale, int xBlocks, int yBlocks, float MaxX, float MinX, float MaxY, float MinY, int loadBlocksX, int loadBlocksY, int loadSlicesZ, float sigma,\n"
"										  float startx, float starty)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	int lid = get_local_id(0) + get_local_size(0)*get_local_id(1);\n"
"	int Index = xid + width*yid;\n"
"	int topz = slice;\n"
"	int bottomz = slice;\n"
"	float sumz = 0.0f;\n"
"	int gx = get_group_id(0);\n"
"	int gy = get_group_id(1);\n"
"	if(topz < 0 )\n"
"		topz = 0;\n"
"	if(bottomz >= slices )\n"
"		bottomz = slices-1;\n"
"\n"
"	__local float atx[256];\n"
"	__local float aty[256];\n"
"	__local int atZ[256];\n"
"\n"
"	int startj = fmax(floor(((starty + gy * get_local_size(1) * pixelscale) * yBlocks ) / (MaxY-MinY)) - loadBlocksY,0) ;\n"
"	int endj = fmin(ceil(((starty + (gy+1) * get_local_size(1) * pixelscale) * yBlocks) / (MaxY-MinY)) + loadBlocksY,yBlocks-1);\n"
"	int starti = fmax(floor(((startx + gx * get_local_size(0) * pixelscale) * xBlocks)  / (MaxX-MinX)) - loadBlocksX,0) ;\n"
"	int endi = fmin(ceil(((startx + (gx+1) * get_local_size(0) * pixelscale) * xBlocks) / (MaxX-MinX)) + loadBlocksX,xBlocks-1);\n"
"\n"
"	for(int k = topz; k <= bottomz; k++)\n"
"	{\n"
"		for (int j = startj ; j <= endj; j++)\n"
"		{\n"
"			//Need list of atoms to load, so we can load in sequence\n"
"			int start = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + starti];\n"
"			int end = clBlockStartPositions[k*xBlocks*yBlocks + xBlocks*j + endi + 1];\n"
"				\n"
"			int gid = start + lid;\n"
"\n"
"			if(lid < end-start)\n"
"			{\n"
"				atx[lid] = clAtomXPos[gid];\n"
"				aty[lid] = clAtomYPos[gid];\n"
"				atZ[lid] = clAtomZNum[gid];\n"
"			}\n"
"\n"
"			barrier(CLK_LOCAL_MEM_FENCE);\n"
"\n"
"			for (int l = 0; l < end-start; l++) \n"
"			{\n"
"				int ZNum = atZ[l];\n"
"			\n"
"					float rad = native_sqrt((startx + xid*pixelscale-atx[l])*(startx + xid*pixelscale-atx[l]) + (starty + yid*pixelscale-aty[l])*(starty + yid*pixelscale-aty[l]));\n"
"\n"
"					if(rad < 0.25f * pixelscale)\n"
"						rad = 0.25f * pixelscale;\n"
"\n"
"					if( rad < 3.0f) // Should also make sure is not too small\n"
"					{\n"
"						int i;\n"
"						float suml, sumg, x;\n"
"\n"
"						/* avoid singularity at r=0 */\n"
"						suml = sumg = 0.0f;\n"
"\n"
"						/* Lorenztians */\n"
"						x = 2.0f*3.141592654f*rad;\n"
"\n"
"						for( i=0; i<2*3; i+=2 )\n"
"							suml += clfParams[(ZNum-1)*12+i]* bessk0( x*native_sqrt(clfParams[(ZNum-1)*12+i+1]) );\n"
"\n"
"						/* Gaussians */\n"
"						x = 3.141592654f*rad;\n"
"						x = x*x;\n"
"\n"
"						for( i=2*3; i<2*(3+3); i+=2 )\n"
"							sumg += clfParams[(ZNum-1)*12+i] * native_exp (-x/clfParams[(ZNum-1)*12+i+1]) / clfParams[(ZNum-1)*12+i+1];\n"
"\n"
"						sumz += 300.8242834f*suml + 150.4121417f*sumg;\n"
"					}\n"
"\n"
"			}\n"
"\n"
"			barrier(CLK_LOCAL_MEM_FENCE);\n"
"		}\n"
"	}\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		Potential[Index].x = native_cos(sigma*sumz);\n"
"		Potential[Index].y = native_sin(sigma*sumz);\n"
"	}\n"
"}\n"
;

const char* propsource =
"__kernel void clGeneratePropagator(__global float2* Propagator, __global float* clXFrequencies, __global float* clYFrequencies, int width, int height, float dz, float wavel, float kmax)\n"
"{\n"
"	int xid = get_global_id(0);\n"
"	int yid = get_global_id(1);\n"
"	if(xid < width && yid < height)\n"
"	{\n"
"		int Index = xid + width*yid;\n"
"		float k0x = clXFrequencies[xid];\n"
"		float k0y = clYFrequencies[yid];\n"
"		float Pi = 3.14159265f;\n"
"\n"
"		k0x*=k0x;\n"
"		k0y*=k0y;\n"
"\n"
"		if (sqrt(k0x+k0y) < kmax)\n"
"		{\n"
"			Propagator[Index].x = cos(Pi*dz*wavel*(k0x+k0y));\n"
"			Propagator[Index].y = -1*sin(Pi*dz*wavel*(k0x+k0y));\n"
"		} else \n"
"		{\n"
"			Propagator[Index].x = 0.0f;\n"
"			Propagator[Index].y = 0.0f;\n"
"		}		\n"
"	}\n"
"}\n";