#include "UnmanagedOpenCL.h"
#pragma once
using namespace System;
#pragma managed
#include "clix.h"


namespace ManagedOpenCLWrapper
{
	public ref class ManagedOpenCL
	{
	private:
		UnmanagedOpenCL* _UMOpenCL;

	public:
		ManagedOpenCL();
		~ManagedOpenCL();

		void setCLdev(int i);
		int getCLdevCount();
		String^ getCLdevString(int i, bool getShort);
		uint64_t getCLdevGlobalMemory();
		int getCLMemoryUsed();

		void importStructure(String^ filepath);
		void uploadParameterisation();
		void getStructureDetails(Int32% Len, float% MinX, float% MinY, float% MinZ, float% MaxX, float% MaxY, float% MaxZ);
		void getNumberSlices(Int32% Slices, bool FD);
		void sortStructure(bool tds);

		void doMultisliceStep(int stepno, int steps);
		void doMultisliceStep(int stepno, int steps, int waves);

		void setCTEMParams(
			float Voltage,
			float Beta,
			float Delta,
			float Aperture,
			float C10,
			float C12Mag, float C12Ang,
			float C21Mag, float C21Ang,
			float C23Mag, float C23Ang,
			float C30,
			float C32Mag, float C32Ang,
			float C34Mag, float C34Ang,
			float C41Mag, float C41Ang,
			float C43Mag, float C43Ang,
			float C45Mag, float C45Ang,
			float C50,
			float C52Mag, float C52Ang,
			float C54Mag, float C54Ang,
			float C56Mag, float C56Ang
			);

		void setSTEMParams(
			float Voltage,
			float Aperture,
			float C10,
			float C12Mag, float C12Ang,
			float C21Mag, float C21Ang,
			float C23Mag, float C23Ang,
			float C30,
			float C32Mag, float C32Ang,
			float C34Mag, float C34Ang,
			float C41Mag, float C41Ang,
			float C43Mag, float C43Ang,
			float C45Mag, float C45Ang,
			float C50,
			float C52Mag, float C52Ang,
			float C54Mag, float C54Ang,
			float C56Mag, float C56Ang
			);

		void initialiseCTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints);
		void initialiseSTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves);

		void initialiseSTEMWaveFunction(float posx, float posy);
		void initialiseSTEMWaveFunction(float posx, float posy, int wave);

		void simulateCTEM();
		void simulateCTEM(int detector, int binning, float doseperpix, float conversionfactor);

		void getCTEMImage(array<float>^ data, int resolution);

		void getEWImage(array<float>^ data, int resolution);
		void getEWImage(array<float>^ data, int resolution, int wave);
		void getEWImage2(array<float>^ data, int resolution);
		void getEWImage2(array<float>^ data, int resolution, int wave);
		void getDiffImage(array<float>^ data, int resolution);
		void getDiffImage(array<float>^ data, int resolution, int wave);

		float getCTEMMax();
		float getCTEMMin();
		float getEWMax();
		float getEWMin();
		float getEWMax2();
		float getEWMin2();
		float getDiffMax();
		float getDiffMax(int wave);
		float getDiffMin();
		float getDiffMin(int wave);

		float getSTEMPixel(float inner, float outer, float xc, float yc, int wave);
	};
}