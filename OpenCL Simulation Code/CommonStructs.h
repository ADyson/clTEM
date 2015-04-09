#pragma once
#include<complex>

struct TEMParameters
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

	float Voltage;
	//Convergence angle
	float Beta;
	//Defocus spread
	float Delta;
	//Objective aperture size (mrad)
	float Aperture;
};

struct STEMParameters
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

	float Voltage;
	//Condenser aperture size (mrad)
	float Aperture;
};