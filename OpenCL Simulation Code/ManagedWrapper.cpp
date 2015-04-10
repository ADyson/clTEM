#include "ManagedWrapper.h"
#include "clix.h"


namespace ManagedOpenCLWrapper
{
	ManagedOpenCL::ManagedOpenCL()
	{
		_UMOpenCL = new UnmanagedOpenCL();
	};

	ManagedOpenCL::~ManagedOpenCL()
	{
		// do nothing
	};

	void ManagedOpenCL::setCLdev(int i)
	{
		_UMOpenCL->setCLdev(i);
	};

	int ManagedOpenCL::getCLdevCount()
	{
		return _UMOpenCL->getCLdevCount();
	}

	String^ ManagedOpenCL::getCLdevString(int i, bool getShort)
	{
		std::string UMstring = _UMOpenCL->getCLdevString(i, getShort);
		return clix::marshalString<clix::E_ANSI>(UMstring);
	}

	uint64_t ManagedOpenCL::getCLdevGlobalMemory()
	{
		return _UMOpenCL->getCLdevGlobalMemory();
	}

	int ManagedOpenCL::getCLMemoryUsed()
	{
		return _UMOpenCL->getCLMemoryUsed();
	}

	void ManagedOpenCL::importStructure(String^ filepath)
	{
		using namespace clix;
		std::string cfilename = marshalString<E_ANSI>(filepath);
		_UMOpenCL->importStructure(cfilename);
	};

	void ManagedOpenCL::uploadParameterisation()
	{
		try
		{
			_UMOpenCL->uploadParameterisation();
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::getStructureDetails(Int32% Len, float% MinX, float% MinY, float% MinZ, float% MaxX, float% MaxY, float% MaxZ)
	{
		// Probably unnecessary to do this way...
		int _Len = _UMOpenCL->Structure->Length;
		float _MinX = _UMOpenCL->Structure->MinimumX;
		float _MinY = _UMOpenCL->Structure->MinimumY;
		float _MinZ = _UMOpenCL->Structure->MinimumZ;
		float _MaxX = _UMOpenCL->Structure->MaximumX;
		float _MaxY = _UMOpenCL->Structure->MaximumY;
		float _MaxZ = _UMOpenCL->Structure->MaximumZ;

		Len = _Len;
		MinX = _MinX;
		MinY = _MinY;
		MinZ = _MinZ;
		MaxX = _MaxX;
		MaxY = _MaxY;
		MaxZ = _MaxZ;
	}

	void ManagedOpenCL::getNumberSlices(Int32% Slices, bool FD)
	{
		if (FD)
		{
			int _NSlices = _UMOpenCL->TS->NumberOfFDSlices;
			Slices = _NSlices;
		}
		else
		{
			int _NSlices = _UMOpenCL->Structure->nSlices;
			Slices = _NSlices;
		}
	};

	void ManagedOpenCL::sortStructure(bool tds)
	{
		// This works, could handle other exception from C# with try catch also
		try
		{
			_UMOpenCL->Structure->SortAtoms(tds);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::doMultisliceStep(int stepno, int steps)
	{
		try
		{
			_UMOpenCL->doMultisliceStep(stepno, steps, 1);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::doMultisliceStep(int stepno, int steps, int waves)
	{
		try
		{
			_UMOpenCL->doMultisliceStep(stepno, steps, waves);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::setCTEMParams(
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
		)
	{
		_UMOpenCL->setCTEMParams(
				Voltage,
				Beta,
				Delta,
				Aperture,
				C10,
				C12Mag, C12Ang,
				C21Mag, C21Ang,
				C23Mag, C23Ang,
				C30,
				C32Mag, C32Ang,
				C34Mag, C34Ang,
				C41Mag, C41Ang,
				C43Mag, C43Ang,
				C45Mag, C45Ang,
				C50,
				C52Mag, C52Ang,
				C54Mag, C54Ang,
				C56Mag, C56Ang
				);
	};

	void ManagedOpenCL::setSTEMParams(
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
		)
	{
		_UMOpenCL->setSTEMParams(
			Voltage,
			Aperture,
			C10,
			C12Mag, C12Ang,
			C21Mag, C21Ang,
			C23Mag, C23Ang,
			C30,
			C32Mag, C32Ang,
			C34Mag, C34Ang,
			C41Mag, C41Ang,
			C43Mag, C43Ang,
			C45Mag, C45Ang,
			C50,
			C52Mag, C52Ang,
			C54Mag, C54Ang,
			C56Mag, C56Ang
			);
	};

	void ManagedOpenCL::initialiseCTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints)
	{
		try
		{
			_UMOpenCL->initialiseCTEMSimulation(resolution, startx, starty, endx, endy, Full3D, FD, dz, full3dints);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::initialiseSTEMSimulation(int resolution, float startx, float starty, float endx, float endy, bool Full3D, bool FD, float dz, int full3dints, int waves)
	{
		try
		{
			_UMOpenCL->initialiseSTEMSimulation(resolution, startx, starty, endx, endy, Full3D, FD, dz, full3dints, waves);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::initialiseSTEMWaveFunction(float posx, float posy)
	{
		try
		{
			_UMOpenCL->initialiseSTEMWaveFunction(posx, posy, 1);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};
	
	void ManagedOpenCL::initialiseSTEMWaveFunction(float posx, float posy, int wave)
	{
		try
		{
			_UMOpenCL->initialiseSTEMWaveFunction(posx, posy, wave);
		}
		catch (std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::simulateCTEM()
	{
		_UMOpenCL->TS->simulateCTEM();
	};

	void ManagedOpenCL::simulateCTEM(int detector, int binning)
	{
		_UMOpenCL->TS->simulateCTEM(detector, binning);
	};

	void ManagedOpenCL::getCTEMImage(array<float>^ data, int resolution)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getCTEMImage(pdata, resolution);
	};

	void ManagedOpenCL::getCTEMImage(array<float>^ data, int resolution, float dose, int binning, int detector)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getCTEMImage(pdata, resolution, dose, binning, detector);
	};

	// possibly phase
	void ManagedOpenCL::getEWImage(array<float>^ data, int resolution)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getEWImage(pdata, resolution, 1);
	};

	void ManagedOpenCL::getEWImage(array<float>^ data, int resolution, int wave)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getEWImage(pdata, resolution, wave);
	};

	// possibly amplitude
	void ManagedOpenCL::getEWImage2(array<float>^ data, int resolution)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getEWImage2(pdata, resolution, 1);
	};

	void ManagedOpenCL::getEWImage2(array<float>^ data, int resolution, int wave)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getEWImage2(pdata, resolution, wave);
	};

	void ManagedOpenCL::getDiffImage(array<float>^ data, int resolution)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getDiffImage(pdata, resolution, 1);
	};

	void ManagedOpenCL::getDiffImage(array<float>^ data, int resolution, int wave)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->getDiffImage(pdata, resolution, wave);
	};

	float ManagedOpenCL::getCTEMMax()
	{
		return _UMOpenCL->TS->imagemax;
	};

	float ManagedOpenCL::getCTEMMin()
	{
		return _UMOpenCL->TS->imagemin;
	};

	float ManagedOpenCL::getEWMax()
	{
		return _UMOpenCL->TS->ewmax[0];
	};

	float ManagedOpenCL::getEWMin()
	{
		return _UMOpenCL->TS->ewmin[0];
	};

	float ManagedOpenCL::getEWMax2()
	{
		return _UMOpenCL->TS->ewmax2[0];
	};

	float ManagedOpenCL::getEWMin2()
	{
		return _UMOpenCL->TS->ewmin2[0];
	};

	float ManagedOpenCL::getDiffMax()
	{
		return _UMOpenCL->TS->diffmax[0];
	};

	float ManagedOpenCL::getDiffMax(int wave)
	{
		return _UMOpenCL->TS->diffmax[wave - 1];
	};

	float ManagedOpenCL::getDiffMin()
	{
		return _UMOpenCL->TS->diffmin[0];
	};

	float ManagedOpenCL::getDiffMin(int wave)
	{
		return _UMOpenCL->TS->diffmin[wave - 1];
	};

	void ManagedOpenCL::getSTEMDiff(int wave)
	{
		_UMOpenCL->TS->getSTEMDiff(wave);
	};

	float ManagedOpenCL::getSTEMPixel(float inner, float outer, float xc, float yc, int wave)
	{
		return _UMOpenCL->TS->getSTEMPixel(inner, outer, xc, yc, wave);
	};

	//void ManagedOpenCL::addTDS()
	//{
	//	_UMOpenCL->TS->addTDS(1);
	//};

	//void ManagedOpenCL::addTDS(int wave)
	//{
	//	_UMOpenCL->TS->addTDS(wave);
	//};

	//void ManagedOpenCL::clearTDS()
	//{
	//	_UMOpenCL->TS->clearTDS(1);
	//};

	//void ManagedOpenCL::clearTDS(int wave)
	//{
	//	_UMOpenCL->TS->clearTDS(wave);
	//};
}