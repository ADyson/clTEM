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
		void InitialiseSimulation(int resolution, bool Full3D);
		void InitialiseSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D);
		void InitialiseSTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D);
		void MakeSTEMWaveFunction(float posx, float posy);
		void MultisliceStep(int stepno, int steps);
		void GetCTEMImage(array<float>^ data, int resolution);
		void GetCTEMImage(array<float>^ data, int resolution, float dose, int binning, int detector);
		void GetEWImage(array<float>^ data, int resolution);
		void SimulateCTEMImage();
		void SimulateCTEMImage(int detector, int binning);
		float GetIMMax();
		float GetIMMin();
		void GetDiffImage(array<float>^ data, int resolution);
		void AddTDSDiffImage(array<float>^ data, int resolution);
		float GetDiffMax();
		float GetDiffMin();
		float GetEWMax();
		float GetEWMin();
		float GetSTEMPixel(float inner, float outer, float xc, float yc);
		void AddTDS();
		void ClearTDS();

		int getCLdevCount();
		String^ getCLdevString(int i, bool getShort);

		// For OpenCL Device Selector...
		void SetDevice(int index);
		int MemoryUsed();

		ManagedOpenCL();
		~ManagedOpenCL();

	};
}