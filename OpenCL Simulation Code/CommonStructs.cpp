#include "CommonStructs.h"

clContext OCL::ctx = OpenCL::MakeContext(OpenCL::GetDeviceList());

const float Constants::Pi = 3.141592653589793238462643383279502884f;
const float Constants::eMass = 9.10938291e-031f;
const float Constants::eMassEnergy = 510.99906f;
const float Constants::eCharge = 1.6021773e-019f;
const float Constants::h = 6.6262e-034f;
const float Constants::c = 299792458.0f;
const float Constants::a0 = 52.9177e-012f;
const float Constants::a0A = 52.9177e-002f;