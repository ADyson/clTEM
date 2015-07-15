#pragma once
#include<complex>

#include "clWrapper.h"

static struct OCL
{
	static clContext ctx;

	static void setContext(int i)
	{
		ctx = OpenCL::MakeTwoQueueContext(OpenCL::GetDeviceByIndex(OpenCL::GetDeviceList(), i));
	}
};

static struct Constants
{
	static const float Pi;
	// electron mass (kg)
	static const float eMass;
	// electron mass (keV)
	static const float eMassEnergy;
	// electron charge (C)
	static const float eCharge;
	// Planck's constant (Js)
	static const float h;
	// speed of light (m/s)
	static const float c;
	// Bohr radius (m)
	static const float a0;
	// Bohr radius (Anstrom)
	static const float a0A;
};

struct MicroscopeParameters
{
	// Defocus
	float C10;
	//Two-fold astigmatism
	std::complex<float> C12;
	//Coma
	std::complex<float> C21;
	//Three-fold astigmatism
	std::complex<float> C23;
	//Spherical
	float C30;

	std::complex<float> C32;

	std::complex<float> C34;

	std::complex<float> C41;

	std::complex<float> C43;

	std::complex<float> C45;
	//Fifth order spherical
	float C50;

	std::complex<float> C52;

	std::complex<float> C54;

	std::complex<float> C56;

	// Voltaage (kV)
	float Voltage;
	//Condenser aperture size (mrad)
	float Aperture;

	//Convergence angle (?)
	float Beta;
	//Defocus spread (?)
	float Delta;

	//Calculate wavelength (Angstrom)
	float Wavelength()
	{
		return Constants::h*Constants::c / sqrt((Constants::eCharge * (Voltage * 1000) * (2 * Constants::eMass*Constants::c*Constants::c + Constants::eCharge * (Voltage * 1000)))) * 1e+010f;
	}

};