__kernel void clFiniteDifference(__global float2* restrict Potential, __global float2* restrict Grad, __global float2* restrict PsiMinus, __global float2* restrict Psi, __global float2* restrict PsiPlus, 
										float FDdz, float wavelength, float sigma, int width, int height)
{
	int xid = get_global_id(0);
	int yid = get_global_id(1);
	int Index = xid + width* yid;

	float2 cMinus = {1 , -2*3.14159f*FDdz/wavelength}; 
	float2 cPlus = {1 , 2*3.14159f*FDdz/wavelength};

	float2 reciprocalCPlus = {cMinus.x / (cMinus.x*cMinus.x + cMinus.y*cMinus.y),cMinus.y / (cMinus.x*cMinus.x + cMinus.y*cMinus.y)};
	float2 cMinusOvercPlus = {(cPlus.x*cPlus.x - cPlus.y*cPlus.y) / (cMinus.x*cMinus.x + cMinus.y*cMinus.y),-2*(cPlus.x*cPlus.y) / (cMinus.x*cMinus.x + cMinus.y*cMinus.y)};

	if(xid < width && yid < height)
	{
		float real = reciprocalCPlus.x*(2*Psi[Index].x-FDdz*FDdz*Grad[Index].x - FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].x/wavelength)
				-reciprocalCPlus.y*(2*Psi[Index].y-FDdz*FDdz*Grad[Index].y -  FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].y/wavelength)
				-cMinusOvercPlus.x*(PsiMinus[Index].x) + cMinusOvercPlus.y*(PsiMinus[Index].y);

		float imag = reciprocalCPlus.y*(2*Psi[Index].x-FDdz*FDdz*Grad[Index].x - FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].x/wavelength)
				+reciprocalCPlus.x*(2*Psi[Index].y-FDdz*FDdz*Grad[Index].y -  FDdz*FDdz*4*3.14159f*sigma*Potential[Index].x*Psi[Index].y/wavelength)
				-cMinusOvercPlus.y*(PsiMinus[Index].x) - cMinusOvercPlus.x*(PsiMinus[Index].y);

		PsiPlus[Index].x = real;
		PsiPlus[Index].y = imag;
	}
}
