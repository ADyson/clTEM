#include "ManagedWrapper.h"
//#include <msclr\marshal_cppstd.h>


namespace ManagedOpenCLWrapper
{

	ManagedOpenCL::ManagedOpenCL() 
	{
		_UMOpenCL = new UnmanagedOpenCL();
	};

	ManagedOpenCL::~ManagedOpenCL() 
	{

	};

	void ManagedOpenCL::ImportStructure(String^ filepath) 
	{
		using namespace clix;

		std::string cfilename = marshalString<E_ANSI>(filepath);
		_UMOpenCL->SetupStructure(cfilename);
	};
	
	void ManagedOpenCL::GetStructureDetails(Int32% Len, float% MinX, float% MinY, float% MinZ, float% MaxX, float% MaxY, float% MaxZ )
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

	void ManagedOpenCL::GetNumberSlices(Int32% Slices)
	{
		int _NSlices= _UMOpenCL->Structure->nSlices;
		Slices = _NSlices;
	};

	void ManagedOpenCL::UploadParameterisation()
	{
		try
		{
			_UMOpenCL->UploadParameterisation();
		}
		catch(std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::SortStructure()
	{
		// This works, could handle other exception from C# with try catch also
		try
		{
			_UMOpenCL->Structure->SortAtoms();
		}
		catch(std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::SetTemParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture)
	{
		_UMOpenCL->SetParamsTEM(df, astigmag, astigang, kilovoltage, spherical, beta, delta, aperture);
	};

	void ManagedOpenCL::SetStemParams(float df, float astigmag, float astigang, float kilovoltage, float spherical, float beta, float delta, float aperture)
	{
		_UMOpenCL->SetParamsSTEM(df, astigmag, astigang, kilovoltage, spherical, beta, delta, aperture);
	};

	void ManagedOpenCL::InitialiseSimulation(int resolution)
	{
		try
		{
			_UMOpenCL->InitialiseSimulation(resolution);
		}
		catch(std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::MultisliceStep(int stepno, int steps)
	{
		try
		{
			_UMOpenCL->MultisliceStep(stepno,steps);
		}
		catch(std::exception ex)
		{
			// Get Message, pass onwards
			std::string message = ex.what();
			System::String^ sys_str = gcnew System::String(message.c_str());
			throw gcnew System::Exception(sys_str);
		}
	};

	void ManagedOpenCL::GetCTEMImage(array<float>^ data, int resolution)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->GetCTEMImage(pdata,resolution);
	};

	float ManagedOpenCL::GetIMMax()
	{	
		return _UMOpenCL->TS->imagemax;
	};

	float ManagedOpenCL::GetIMMin()
	{	
		return _UMOpenCL->TS->imagemin;
	};

	void ManagedOpenCL::GetDiffImage(array<float>^ data, int resolution)
	{
		pin_ptr<float> pdata = &data[0];
		_UMOpenCL->TS->GetDiffImage(pdata,resolution);
	};

	float ManagedOpenCL::GetDiffMax()
	{	
		return _UMOpenCL->TS->diffmax;
	};

	float ManagedOpenCL::GetDiffMin()
	{	
		return _UMOpenCL->TS->diffmin;
	};
}