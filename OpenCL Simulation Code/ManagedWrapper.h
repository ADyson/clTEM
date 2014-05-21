#include "UnmanagedOpenCL.h"



#pragma once
using namespace System;

#pragma managed

#include "clix.h"

namespace ManagedOpenCLWrapper {

	public ref class ManagedOpenCL {
	private:
		UnmanagedOpenCL* _UMOpenCL;

	public:
		void ImportStructure(String^ filepath);
		void GetStructureDetails(Int32% Len, float% MinX, float% MinY, float% MinZ, float% MaxX, float% MaxY, float% MaxZ );
		void GetNumberSlices(Int32% Slices);
		void UploadParameterisation();
		void SortStructure(bool tds);
		void SetTemParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture, float astig2mag, float astig2ang, float b2mag, float b2ang );
		void SetStemParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture);
		void InitialiseSimulation(int resolution);
		void InitialiseSTEMSimulation(int resolution);
		void MakeSTEMWaveFunction(int posx, int posy);
		void MultisliceStep(int stepno, int steps);
		void GetCTEMImage(array<float>^ data, int resolution);
		void GetEWImage(array<float>^ data, int resolution);
		void SimulateCTEMImage();
		float GetIMMax();
		float GetIMMin();
		void GetDiffImage(array<float>^ data, int resolution);
		void AddTDSDiffImage(array<float>^ data, int resolution);
		float GetDiffMax();
		float GetDiffMin();
		float GetEWMax();
		float GetEWMin();
		float GetSTEMPixel();
		ManagedOpenCL();
		~ManagedOpenCL();

	};
}