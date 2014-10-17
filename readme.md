#clTEM
####OpenCL accelerated TEM/STEM/Diffraction/EW Image Simulation Software

1. Opens .xyz structure files only (.cif and others will be added later)
1. Conventional multislice and improved version with no slice thickness limitations, and accurate finite difference support
1. Supports TDS via frozen phonon simulations for CBED and STEM.
1. 256,512,1024,2048,4096 pixel images are supported for TEM, CBED
1. Any size image can be simulated in STEM.
1. Any number of detectors can be added for STEM.
1. Images can be simulated for different electron doses. 
1. Images can be simulated for different CCD types and include the effects of binning. 
1. Runs in parallel on CPU or GPU (AMD and nVidia) (no multi-gpu support yet)

![GUI](http://adyson.github.io/clTEM/cltemscreen.png)
