#pragma once
#include <string>

// test using raw string literal for slightly cleaner code.
extern const std::string InitialiseWavefunctionSource;

// Slices have to start from 0
// Tried to fix so model doesnt have to have min at (0,0,0)
// Uses normal potential sliced many times not projected.
// Includes atoms onmultiple slices where they contribute
// Could be alot faster will try other methods like one kernel for each atom type with pre tabulated potentials.
// z is height of top of slice...
extern const char* BinnedAtomicPotentialSource;

extern const char* BinnedAtomicPotentialSource2;

extern const char* BandLimitSource;

extern const char* fftShiftSource;

extern const char* gradsource;

extern const char* fdsource;

// see Rolf Erni's book, Kirklands book and maybe the SuperSTEM book for details
// Need to test the behavious of float2 (i.e. addition etc.i)
extern const std::string imagingKernelSource;

extern const std::string InitialiseSTEMWavefunctionSourceTest;

extern const char* InitialiseSTEMWavefunctionSource;

extern const char* sumReductionsource2;

extern const char* floatSumReductionsource2;

extern const char* multisource;

extern const char* abssource2;

extern const char* multiplySource;

extern const char* bandPassSource;

extern const char* floatbandPassSource;

extern const char* floatabsbandPassSource;

extern const char* SqAbsSource;

extern const char* DQESource;

extern const char* NTFSource;

extern const char* opt2source;

extern const char* fd2source;

extern const char* conv2source;

extern const char* propsource;