using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Win32;
using ManagedOpenCLWrapper;
using BitMiracle.LibTiff.Classic;
using PanAndZoom;
using ColourGenerator;

namespace GPUTEMSTEMSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsResolutionSet = false;
        bool HaveStructure = false;
        bool IsSorted = false;
        bool TDS = false;
        bool isFull3D = true;
        bool DetectorVis = false;
        bool HaveMaxMrad = false;

        int Resolution;
        int CurrentResolution = 0;
        float CurrentPixelScale = 0;
        float CurrentWavelength = 0;
        float CurrentVoltage = 0;
        List<String> devicesShort;
        List<String> devicesLong;

        TEMParams ImagingParameters;
        TEMParams ProbeParameters;

	

        /// <summary>
        /// Cancel event to halt calculation.
        /// </summary>
        public event EventHandler Cancel = delegate { };

        /// <summary>
        /// Worker to perform calculations in Non UI Thread.
        /// </summary>
        ManagedOpenCL mCL;

        // TaskFactory stuff
        private CancellationTokenSource cancellationTokenSource;

        float[] TDSImage;

        public List<DetectorItem> Detectors = new List<DetectorItem>();
        List<DetectorItem> LockedDetectors = new List<DetectorItem>();

        public STEMArea STEMRegion = new STEMArea { xStart = 0, xFinish = 1, yStart = 0, yFinish = 1, xPixels = 1, yPixels = 1 };
        STEMArea LockedArea;

        public SimArea SimRegion = new SimArea { xStart = 0, xFinish = 10, yStart = 0, yFinish = 10};

        bool userSIMarea = false;
        bool userSTEMarea = false;

        float pixelScale;
        float wavelength;

		// Make the 3 default tabs...
        DisplayTab CTEMDisplay = new DisplayTab("CTEM");
        DisplayTab EWDisplay = new DisplayTab("EW");
        DisplayTab DiffDisplay = new DisplayTab("Diffraction");

        public MainWindow()
        {
            InitializeComponent();
			CancelButton.IsEnabled = false;
            
			// add constant tabs to UI
			LeftTab.Items.Add(CTEMDisplay.Tab);
			LeftTab.Items.Add(EWDisplay.Tab);
			RightTab.Items.Add(DiffDisplay.Tab);

            CTEMDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            EWDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            DiffDisplay.SetPositionReadoutElements(ref RightXCoord, ref RightYCoord);
            DiffDisplay.Reciprocal = true;

			// Start in TEM mode.
            TEMRadioButton.IsChecked = true;

            ImagingParameters = new TEMParams();
            ProbeParameters = new TEMParams();

            // Setup Managed Wrapper and Upload Atom Parameterisation ready for Multislice calculations.
            // Moved parameterisation, will be redone each time we get new structure now :(
            mCL = new ManagedOpenCL();

            // Must be set twice
            DeviceSelector.SelectedIndex = -1;
            DeviceSelector.SelectedIndex = -1;

            // Set Default Values
            ImagingAperture.Text = "30";
            ImagingCs.Text = "10000";
            ImagingkV.Text = "200";
            ImagingA1.Text = "0";
            ImagingA1theta.Text = "0";
            ImagingB2.Text = "0";
            ImagingB2Phi.Text = "0";
            Imagingbeta.Text = "5";
            Imagingdelta.Text = "3";
            ImagingDf.Text = "0";
            ImagingA2.Text = "0";
            ImagingA2Phi.Text = "0";

			BinningCombo.SelectedIndex = 0;
			CCDCombo.SelectedIndex = 0;

            // Add fake device names for now
            devicesShort = new List<String>();
            devicesLong = new List<String>();

            int numDev = mCL.getCLdevCount();

            for (int i = 0; i < numDev; i++)
            {
                devicesShort.Add(mCL.getCLdevString(i, true));
                devicesLong.Add(mCL.getCLdevString(i, false));
            }

            DeviceSelector.ItemsSource = devicesShort;
        }

        private void ImportStructureButton(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();

            // Set defaults for file dialog.
            openDialog.FileName = "file name";                  // Default file name
            openDialog.DefaultExt = ".xyz";                     // Default file extension
            openDialog.Filter = "XYZ Coordinates (.xyz)|*.xyz"; // Filter files by extension

            Nullable<bool> result = openDialog.ShowDialog();

            if (result == true)
            {
                string fName = openDialog.FileName;
                fileNameLabel.Content = System.IO.Path.GetFileName(fName);
                fileNameLabel.ToolTip = fName;

                // Now pass filename through to unmanaged where atoms can be imported inside structure class...
                mCL.ImportStructure(openDialog.FileName);
                mCL.UploadParameterisation();

                // Update some dialogs if everything went OK.
                Int32 Len = 0;
                float MinX = 0;
                float MinY = 0;
                float MinZ = 0;
                float MaxX = 0;
                float MaxY = 0;
                float MaxZ = 0;

                mCL.GetStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

                HaveStructure = true;

                WidthLabel.Content = (MaxX - MinX).ToString("f2") + " Å";
                HeightLabel.Content = (MaxY - MinY).ToString("f2") + " Å";
                DepthLabel.Content = (MaxZ - MinZ).ToString("f2") + " Å";
                AtomNoLabel.Content = Len.ToString();

                if (!userSTEMarea)
                {
                    STEMRegion.xFinish = Convert.ToSingle((MaxX - MinX).ToString("f2"));
					STEMRegion.yFinish = Convert.ToSingle((MaxY - MinY).ToString("f2"));
                }

                if (!userSIMarea)
                {
                    SimRegion.xFinish = Convert.ToSingle((MaxX - MinX).ToString("f2"));
                    SimRegion.yFinish = Convert.ToSingle((MaxY - MinY).ToString("f2"));
                }

                UpdatePx();

                // Now we want to sorting the atoms ready for the simulation process do this in a background worker...
                this.cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = this.cancellationTokenSource.Token;
                var progressReporter = new ProgressReporter();
                var task = Task.Factory.StartNew(() =>
                {
                    // This is where we start sorting the atoms in the background ready to be processed later...
                    mCL.SortStructure(TDS);
                    return 0;
                },cancellationToken);

                // This runs on UI Thread so can access UI, probably better way of doing image though.
                progressReporter.RegisterContinuation(task, () =>
                {
                    IsSorted = true;
                });

            }
        }
        
        private void ImportUnitCellButton(object sender, RoutedEventArgs e)
        {
            // No idea what to do here just yet, will just have to programatically make potentials based on unit cell and number of unit cells in each direction.
        }

        // Simulation Button
        private void SimulationButton(object sender, RoutedEventArgs e)
        {
            // Check We Have Structure
            if (HaveStructure == false)
            {
                var result = MessageBox.Show("No Structure Loaded", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Check parameters are set
            if (IsResolutionSet == false)
            {
                var result = MessageBox.Show("Resolution Not Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (DeviceSelector.SelectedIndex == -1)
            {
                var result = MessageBox.Show("OpenCL Device Not Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CurrentResolution = Resolution;
            CurrentPixelScale = pixelScale;



            CurrentWavelength = wavelength;
            CurrentVoltage = ImagingParameters.kilovoltage;

            // DiffDisplay.tCanvas.Width = CurrentResolution;
            // DiffDisplay.tCanvas.Height = CurrentResolution;

            SimulateEWButton.IsEnabled = false;
            SimulateImageButton.IsEnabled = false;

            bool select_TEM = TEMRadioButton.IsChecked == true;
            bool select_STEM = STEMRadioButton.IsChecked == true;
            bool select_CBED = CBEDRadioButton.IsChecked == true;

            int TDSruns = Convert.ToInt32(TDSCounts.Text);

            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            var progressReporter = new ProgressReporter();

			CancelButton.IsEnabled = false;
            var task = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                Stopwatch timer = new Stopwatch();

                // Upload Simulation Parameters to c++ class
                mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical, ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

                mCL.SetStemParams(ProbeParameters.df, ProbeParameters.astigmag, ProbeParameters.astigang, ProbeParameters.kilovoltage, ProbeParameters.spherical, ProbeParameters.beta, ProbeParameters.delta, ProbeParameters.aperturemrad);

				SimulationMethod(select_TEM, select_STEM, select_CBED, TDSruns, ref progressReporter, ref timer, ref cancellationToken);
                
            }, cancellationToken);

            // This runs on UI Thread so can access UI, probably better way of doing image though.
            //SimWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            progressReporter.RegisterContinuation(task, () =>
            {
				CancelButton.IsEnabled = false;
                progressBar1.Value = 100;
                progressBar2.Value = 100;

                if (select_STEM)
                {
                    if (LockedDetectors.Count == 0)
                    {
                        SimulateEWButton.IsEnabled = true;
                        return;
                    }

                    foreach (DetectorItem i in LockedDetectors)
                    {
						UpdateDetectorImage(i);
                    }

                    // just select the first tab for convenience
                    LockedDetectors[0].Tab.IsSelected = true;
                    SaveImageButton.IsEnabled = true;
                }
                else if (select_CBED)
                {
                    UpdateTDSImage();
                    SaveImageButton2.IsEnabled = true;

                }
                else
                {
					UpdateEWImage();
					EWDisplay.Tab.IsSelected = true;
					UpdateDiffractionImage();
                    SaveImageButton.IsEnabled = true;
                    SaveImageButton2.IsEnabled = true;
                    SimulateImageButton.IsEnabled = true;
                }

                SimulateEWButton.IsEnabled = true;
            });
        
        }

		private void UpdateEWImage()
		{
			EWDisplay.xDim = CurrentResolution;
			EWDisplay.yDim = CurrentResolution;

			EWDisplay._ImgBMP = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			EWDisplay.tImage.Source = EWDisplay._ImgBMP;

			// When its completed we want to get data to c# for displaying in an image...
			EWDisplay.ImageData = new float[CurrentResolution * CurrentResolution];
			mCL.GetEWImage(EWDisplay.ImageData, CurrentResolution);


			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel = (EWDisplay._ImgBMP.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride = EWDisplay._ImgBMP.PixelWidth * bytesPerPixel;

			// Create a byte array for a the entire size of bitmap.
			var arraySize = stride * EWDisplay._ImgBMP.PixelHeight;
			var pixelArray = new byte[arraySize];

			float min = mCL.GetEWMin();
			float max = mCL.GetEWMax();

			if (min == max)
				return;

			for (int row = 0; row < EWDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < EWDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			Int32Rect rect = new Int32Rect(0, 0, EWDisplay._ImgBMP.PixelWidth, EWDisplay._ImgBMP.PixelHeight);

			EWDisplay._ImgBMP.WritePixels(rect, pixelArray, stride, 0);
		}

		private void SimulationMethod(bool select_TEM, bool select_STEM, bool select_CBED, int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{

            // Add Pixelscale to image tabs and diffraction then run simulation
			if (select_TEM)
			{
                EWDisplay.PixelScaleX = pixelScale;
                DiffDisplay.PixelScaleX = pixelScale;

                EWDisplay.PixelScaleY = pixelScale;
                DiffDisplay.PixelScaleY = pixelScale;

				SimulateTEM(ref progressReporter,ref timer, ref ct);
			}
			else if (select_STEM)
			{
				int multistem = 50;
                DiffDisplay.PixelScaleX = pixelScale;
                DiffDisplay.PixelScaleY = pixelScale;
				SimulateSTEM(TDSruns, ref progressReporter, ref timer, ref ct, multistem);
			}
			else if (select_CBED)
			{
                DiffDisplay.PixelScaleX = pixelScale;
                DiffDisplay.PixelScaleY = pixelScale;
				SimulateCBED(TDSruns, ref progressReporter,ref timer, ref ct);
			}
		}	

		private void UI_UpdateSimulationProgress(float ms, int NumberOfSlices, int runs, int j, int i, int mem)
		{
			this.progressBar1.Value =
				Convert.ToInt32(100 * Convert.ToSingle(i) /
								Convert.ToSingle(NumberOfSlices));
			this.progressBar2.Value =
				Convert.ToInt32(100 * Convert.ToSingle(j) /
								Convert.ToSingle(runs));
			this.TimerMessage.Content = ms.ToString() + " ms";
			this.MemUsageLabel.Content = mem / (1024 * 1024) + " MB";
		}

		private void UpdateDiffractionImage()
		{

			DiffDisplay.xDim = CurrentResolution;
			DiffDisplay.yDim = CurrentResolution;
			
			DiffDisplay._ImgBMP = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			DiffDisplay.tImage.Source = DiffDisplay._ImgBMP;

			DiffDisplay.ImageData = new float[CurrentResolution * CurrentResolution];

			mCL.GetDiffImage(DiffDisplay.ImageData, CurrentResolution);
			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel2 = (DiffDisplay._ImgBMP.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride2 = DiffDisplay._ImgBMP.PixelWidth * bytesPerPixel2;

			// Create a byte array for a the entire size of bitmap.
			var arraySize2 = stride2 * DiffDisplay._ImgBMP.PixelHeight;
			var pixelArray2 = new byte[arraySize2];

			float min2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.GetDiffMin()+1.0f)));
			float max2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.GetDiffMax()+1.0f)));

			if (min2 == max2)
				return;

			for (int row = 0; row < DiffDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < DiffDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(DiffDisplay.ImageData[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(DiffDisplay.ImageData[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(DiffDisplay.ImageData[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
				}


			Int32Rect rect2 = new Int32Rect(0, 0, DiffDisplay._ImgBMP.PixelWidth, DiffDisplay._ImgBMP.PixelHeight);

			DiffDisplay._ImgBMP.WritePixels(rect2, pixelArray2, stride2, 0);

            // to update diffraction rings, show they have changed.
            foreach (DetectorItem det in Detectors)
                det.setEllipse(CurrentResolution, CurrentPixelScale, CurrentWavelength, DetectorVis);
		}

		private void UpdateTDSImage()
		{
			DiffDisplay._ImgBMP = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			DiffDisplay.tImage.Source = DiffDisplay._ImgBMP;

			DiffDisplay.xDim = CurrentResolution;
			DiffDisplay.yDim = CurrentResolution;
			DiffDisplay.ImageData = TDSImage;	
	
            DiffDisplay.tCanvas.Width = CurrentResolution;
            DiffDisplay.tCanvas.Height = CurrentResolution;

			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel2 = (DiffDisplay._ImgBMP.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride2 = DiffDisplay._ImgBMP.PixelWidth * bytesPerPixel2;

			// Create a byte array for a the entire size of bitmap.
			var arraySize2 = stride2 * DiffDisplay._ImgBMP.PixelHeight;
			var pixelArray2 = new byte[arraySize2];

			float min2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.GetDiffMin()+1.0f)));
            float max2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.GetDiffMax()+1.0f)));

            for (int row = 0; row < DiffDisplay._ImgBMP.PixelHeight; row++)
                for (int col = 0; col < DiffDisplay._ImgBMP.PixelWidth; col++)
                {
                    pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(TDSImage[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(TDSImage[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(TDSImage[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                }


			Int32Rect rect2 = new Int32Rect(0, 0, DiffDisplay._ImgBMP.PixelWidth, DiffDisplay._ImgBMP.PixelHeight);

			DiffDisplay._ImgBMP.WritePixels(rect2, pixelArray2, stride2, 0);

            // to update diffraction rings, show they have changed.
            foreach (DetectorItem det in Detectors)
                det.setEllipse(CurrentResolution, CurrentPixelScale, CurrentWavelength, DetectorVis);
		}

		private void SimulateTEM(ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
			mCL.InitialiseSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D);

			// Reset atoms incase TDS has been used
			mCL.SortStructure(false);

			// Use Background worker to progress through each step
			int NumberOfSlices = 0;
			mCL.GetNumberSlices(ref NumberOfSlices);
			// Seperate into setup, loop over slices and final steps to allow for progress reporting.

			

			for (int i = 1; i <= NumberOfSlices; i++)
			{
				if (ct.IsCancellationRequested == true)
					break;

				timer.Start();
				mCL.MultisliceStep(i, NumberOfSlices);
				timer.Stop();
				int mem = mCL.MemoryUsed();
				// Report progress of the work. 

				float ms = timer.ElapsedMilliseconds;
				progressReporter.ReportProgress((val) =>
				{
					CancelButton.IsEnabled = true;
					UI_UpdateSimulationProgress(ms, NumberOfSlices, 1, 1, i, mem);
				}, i);

			}

		}

		private void SimulateSTEM(int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
			LockedDetectors = Detectors;
			LockedArea = STEMRegion;

            foreach (DetectorItem dt in LockedDetectors)
            {
                dt.PixelScaleX = LockedArea.getxInterval;
                dt.PixelScaleY = LockedArea.getyInterval;
                dt.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            }

			if (LockedDetectors.Count == 0)
			{
				var result = MessageBox.Show("No Detectors Have Been Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			int numPix = LockedArea.xPixels * LockedArea.yPixels;
			int pix = 0;

			foreach (DetectorItem i in LockedDetectors)
			{
				i.ImageData = new float[numPix];
				i.Min = float.MaxValue;
				i.Max = float.MinValue;
			}

			int runs = 1;
			if (TDS)
			{
				runs = TDSruns;
			}

			numPix *= runs;

			mCL.InitialiseSTEMSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D);

			float xInterval = LockedArea.getxInterval;
			float yInterval = LockedArea.getyInterval;

			for (int posY = 0; posY < LockedArea.yPixels; posY++)
			{
				float fCoordy = (LockedArea.yStart + posY * yInterval) / pixelScale;

				for (int posX = 0; posX < LockedArea.xPixels; posX++)
				{
					TDSImage = new float[CurrentResolution * CurrentResolution];

					for (int j = 0; j < runs; j++)
					{
						// if TDS was used last atoms are in wrong place and need resetting via same function
						// if (TDS)
						mCL.SortStructure(TDS);

						float fCoordx = (LockedArea.xStart + posX * xInterval) / pixelScale;

						mCL.MakeSTEMWaveFunction(fCoordx - SimRegion.xStart, fCoordy - SimRegion.yStart);

						// Use Background worker to progress through each step
						int NumberOfSlices = 0;
						mCL.GetNumberSlices(ref NumberOfSlices);
						// Seperate into setup, loop over slices and final steps to allow for progress reporting.
						
						for (int i = 1; i <= NumberOfSlices; i++)
						{
							if (ct.IsCancellationRequested == true)
								break;

							timer.Start();
							mCL.MultisliceStep(i, NumberOfSlices);
							timer.Stop();
							int mem = mCL.MemoryUsed();
							float ms = timer.ElapsedMilliseconds;

							progressReporter.ReportProgress((val) =>
							{
								CancelButton.IsEnabled = true;
								// Note: code passed to "ReportProgress" can access UI elements freely. 
								UI_UpdateSimulationProgressSTEM(ms, numPix, pix, NumberOfSlices, i, mem);
							}, i);
						}
						pix++;

						if (ct.IsCancellationRequested == true)
							break;

						// After a complete run if TDS need to sum up the DIFF...
						mCL.AddTDSDiffImage(TDSImage, CurrentResolution);
						// Sum it in C++ also for the stem pixel measurement...
						mCL.AddTDS();

						progressReporter.ReportProgress((val) =>
						{
							CancelButton.IsEnabled = false;
							UpdateTDSImage();
						}, j);
					}

					if (ct.IsCancellationRequested == true)
						break;

					// loop through and get each STEM pixel for each detector at the same time
					foreach (DetectorItem i in LockedDetectors)
					{
						float pixelVal = mCL.GetSTEMPixel(i.Inner, i.Outer);

						i.ImageData[LockedArea.xPixels * posY + posX] = pixelVal;

						if (pixelVal < i.Min)
						{
							i.Min = pixelVal;
						}
						if (pixelVal > i.Max)
						{
							i.Max = pixelVal;
						}

					}

					progressReporter.ReportProgress((val) =>
					{

						foreach (DetectorItem i in LockedDetectors)
						{
							UpdateDetectorImage(i);
						}
					}, posX);

					// Reset TDS arrays after pixel values retrieved...
					mCL.ClearTDS();

				}
				if (ct.IsCancellationRequested == true)
					break;
			}
		}

		private void SimulateSTEM(int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct, int multistem)
		{
			LockedDetectors = Detectors;
			LockedArea = STEMRegion;

			foreach (DetectorItem dt in LockedDetectors)
			{
				dt.PixelScaleX = LockedArea.getxInterval;
				dt.PixelScaleY = LockedArea.getyInterval;
				dt.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
			}

			if (LockedDetectors.Count == 0)
			{
				var result = MessageBox.Show("No Detectors Have Been Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			int numPix = LockedArea.xPixels * LockedArea.yPixels;
			int pix = 0;

			foreach (DetectorItem i in LockedDetectors)
			{
				i.ImageData = new float[numPix];
				i.Min = float.MaxValue;
				i.Max = float.MinValue;
			}

			int runs = 1;
			if (TDS)
			{
				runs = TDSruns;
			}

			numPix *= runs;

			mCL.InitialiseSTEMSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D,multistem);

			float xInterval = LockedArea.getxInterval;
			float yInterval = LockedArea.getyInterval;

			//List<float[]> TDSImages = new List<float[]>();
			List<float> fCoordxs = new List<float>();


			List<Tuple<Int32, Int32>> Pixels = new List<Tuple<Int32, Int32>>();

			for (int posY = 0; posY < LockedArea.yPixels; posY++)
			{
				for (int posX = 0; posX < LockedArea.xPixels; posX++)
				{
					Pixels.Add(new Tuple<Int32,Int32>(posX, posY));
				}
			}

			Shuffler.Shuffle< Tuple<Int32, Int32>>(Pixels);
			for (int j = 0; j < runs; j++)
			{
				mCL.SortStructure(TDS);

				// Reset image contrast limits for every run....
				foreach (DetectorItem i in LockedDetectors)
				{
					i.Min = float.MaxValue;
					i.Max = float.MinValue;
				}



				for (int posY = 0; posY < LockedArea.yPixels* LockedArea.xPixels; posY+=multistem)
				{
				//float fCoordy = (LockedArea.yStart + posY * yInterval) / pixelScale;
							
					for (int i = 1; i <= multistem; i++)
					{
						//TDSImages.Add(new float[CurrentResolution * CurrentResolution]);
						//fCoordxs.Add((LockedArea.xStart + (i - 1 + posX) * xInterval) / pixelScale);
					}

				
						// if TDS was used last atoms are in wrong place and need resetting via same function
						// if (TDS)
						
						for (int i = 1; i <= multistem; i++)
						{

							mCL.MakeSTEMWaveFunction(((LockedArea.xStart + Pixels[(posY+ i-1)].Item1 * xInterval - SimRegion.xStart)/pixelScale),
								((LockedArea.yStart + Pixels[(posY + i - 1)].Item2 * yInterval - SimRegion.yStart) / pixelScale), i);
						}

						// Use Background worker to progress through each step
						int NumberOfSlices = 0;
						mCL.GetNumberSlices(ref NumberOfSlices);
						// Seperate into setup, loop over slices and final steps to allow for progress reporting.

						for (int i = 1; i <= NumberOfSlices; i++)
						{
							if (ct.IsCancellationRequested == true)
								break;

							timer.Start();
							mCL.MultisliceStep(i, NumberOfSlices,multistem);
							timer.Stop();
							int mem = mCL.MemoryUsed();
							float ms = timer.ElapsedMilliseconds;

							progressReporter.ReportProgress((val) =>
							{
								CancelButton.IsEnabled = true;
								// Note: code passed to "ReportProgress" can access UI elements freely. 
								UI_UpdateSimulationProgressSTEM(ms, numPix, pix, NumberOfSlices, i, mem);
							}, i);
						}
						pix+=multistem;

						if (ct.IsCancellationRequested == true)
							break;

						for (int i = 1; i <= multistem; i++)
						{
							// After a complete run if TDS need to sum up the DIFF...
							//mCL.AddTDSDiffImage(TDSImages[i-1], CurrentResolution,i);
							// Sum it in C++ also for the stem pixel measurement...
							//mCL.AddTDS(i);
							mCL.GetSTEMDiff(i);
						}

						progressReporter.ReportProgress((val) =>
						{
							CancelButton.IsEnabled = false;
							//UpdateTDSImage();
						}, j);

						for (int p = 1; p <= multistem; p++)
						{
							// loop through and get each STEM pixel for each detector at the same time
							foreach (DetectorItem i in LockedDetectors)
							{
								float pixelVal = mCL.GetSTEMPixel(i.Inner, i.Outer, p);
								float newVal = i.ImageData[LockedArea.xPixels * Pixels[posY + p - 1].Item2 + Pixels[posY + p - 1].Item1] + pixelVal;
								i.ImageData[LockedArea.xPixels * Pixels[posY + p-1 ].Item2 + Pixels[posY + p-1 ].Item1] = newVal;

								//if (j == runs-1) // Only use final values to set contrast limits
								//{
									if (newVal < i.Min)
									{
										i.Min = newVal;
									}
									if (newVal > i.Max)
									{
										i.Max = newVal;
									}
								//}

							}
						}

						// This will update display after each tds run...
						progressReporter.ReportProgress((val) =>
						{
							foreach (DetectorItem i in LockedDetectors)
							{
								UpdateDetectorImage(i);
							}
						}, posY);

					if (ct.IsCancellationRequested == true)
						break;

				}

				// Reset TDS arrays after pixel values retrieved
				//mCL.ClearTDS(multistem);

				//fCoordxs.Clear();

				if (ct.IsCancellationRequested == true)
					break;
			}
		}

		private void SimulateCBED(int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
			int numPix = 1;
			int pix = 0;

			mCL.InitialiseSTEMSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D);

			int posX = CurrentResolution / 2;
			int posY = CurrentResolution / 2;

			

			// Use Background worker to progress through each step
			int NumberOfSlices = 0;
			mCL.GetNumberSlices(ref NumberOfSlices);
			// Seperate into setup, loop over slices and final steps to allow for progress reporting.

			int runs = 1;
			if (TDS)
			{
				runs = TDSruns;
			}

			TDSImage = new float[CurrentResolution * CurrentResolution];

			for (int j = 0; j < runs; j++)
			{
				mCL.SortStructure(TDS);
				mCL.MakeSTEMWaveFunction(posX, posY);


				for (int i = 1; i <= NumberOfSlices; i++)
				{
					if (ct.IsCancellationRequested == true)
						break;

					timer.Start();
					mCL.MultisliceStep(i, NumberOfSlices);
					timer.Stop();
					int mem = mCL.MemoryUsed();
					float ms = timer.ElapsedMilliseconds;

					// Report progress of the work. 
					progressReporter.ReportProgress((val) =>
					{
						CancelButton.IsEnabled = true;
						// Note: code passed to "ReportProgress" can access UI elements freely. 
						UI_UpdateSimulationProgress(ms, NumberOfSlices, runs, j, i, mem);
					}, i);
				}

				// After a complete run if TDS need to sum up the DIFF...
				mCL.AddTDSDiffImage(TDSImage, CurrentResolution);

				// Sum it in C++ also for the stem pixel measurement...
				mCL.AddTDS();

				if (ct.IsCancellationRequested == true)
					break;
				progressReporter.ReportProgress((val) =>
				{
					CancelButton.IsEnabled = false;
					UpdateTDSImage();
				}, j);
			}
		}

		private void UpdateDetectorImage(DetectorItem i)
		{
			i.xDim = LockedArea.xPixels;
			i.yDim = LockedArea.yPixels;

			i._ImgBMP = new WriteableBitmap(LockedArea.xPixels, LockedArea.yPixels, 96, 96, PixelFormats.Bgr32, null);
			i.tImage.Source = i._ImgBMP;

			RenderOptions.SetBitmapScalingMode(i.tImage, BitmapScalingMode.NearestNeighbor);

			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel = (i._ImgBMP.Format.BitsPerPixel + 7) / 8;
			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride = i._ImgBMP.PixelWidth * bytesPerPixel;

			// Create a byte array for a the entire size of bitmap.
			var arraySize = stride * i._ImgBMP.PixelHeight;
			var pixelArray = new byte[arraySize];

			float min = i.Min;
			float max = i.Max;

			if (min == max)
				return;

			for (int row = 0; row < i._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < i._ImgBMP.PixelWidth; col++)
				{
					pixelArray[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - min) / (max - min)) * 254.0f));
					pixelArray[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - min) / (max - min)) * 254.0f));
					pixelArray[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - min) / (max - min)) * 254.0f));
					pixelArray[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			Int32Rect rect = new Int32Rect(0, 0, i._ImgBMP.PixelWidth, i._ImgBMP.PixelHeight);

			i._ImgBMP.WritePixels(rect, pixelArray, stride, 0);
		}

		private void UpdateCTEMImage(float dpp, int binning, int CCD)
		{
			// Get pixelscale from EW and apply to image.
			CTEMDisplay.PixelScaleY = EWDisplay.PixelScaleY;
			CTEMDisplay.PixelScaleX = EWDisplay.PixelScaleY;

			CTEMDisplay.xDim = CurrentResolution;
			CTEMDisplay.yDim = CurrentResolution;

			CTEMDisplay._ImgBMP = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			CTEMDisplay.tImage.Source = CTEMDisplay._ImgBMP;

			// When its completed we want to get data to c# for displaying in an image...
			CTEMDisplay.ImageData = new float[CurrentResolution * CurrentResolution];

			if (CCD != 0)
				mCL.GetCTEMImage(CTEMDisplay.ImageData, CurrentResolution, dpp, binning, CCD);
			else
				mCL.GetCTEMImage(CTEMDisplay.ImageData, CurrentResolution);

			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel = (CTEMDisplay._ImgBMP.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride = CTEMDisplay._ImgBMP.PixelWidth * bytesPerPixel;

			// Create a byte array for a the entire size of bitmap.
			var arraySize = stride * CTEMDisplay._ImgBMP.PixelHeight;
			var pixelArray = new byte[arraySize];

			float min = mCL.GetIMMin();
			float max = mCL.GetIMMax();

			if (min == max)
				return;

			for (int row = 0; row < CTEMDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < CTEMDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			Int32Rect rect = new Int32Rect(0, 0, CTEMDisplay._ImgBMP.PixelWidth, CTEMDisplay._ImgBMP.PixelHeight);

			CTEMDisplay._ImgBMP.WritePixels(rect, pixelArray, stride, 0);

			CTEMDisplay.Tab.IsSelected = true;
		}
		
		private void UI_UpdateSimulationProgressSTEM(float ms, int numPix, int pix, int NumberOfSlices, int i, int mem)
		{
			this.progressBar1.Value =
				Convert.ToInt32(100 * Convert.ToSingle(i) /
								Convert.ToSingle(NumberOfSlices));
			this.progressBar2.Value =
				Convert.ToInt32(100 * Convert.ToSingle(pix) /
								Convert.ToSingle(numPix));
			this.TimerMessage.Content = ms.ToString() + " ms";
			this.MemUsageLabel.Content = mem / (1024 * 1024) + " MB";
		}

		private void SaveImageButton_Click(object sender, RoutedEventArgs e)
		{
			List<DisplayTab> tabs = new List<DisplayTab>();
			tabs.Add(CTEMDisplay);
			tabs.Add(EWDisplay);
			tabs.AddRange(LockedDetectors);

			SaveImageFromTabs(tabs);
		}

        private void SaveImageButton2_Click(object sender, RoutedEventArgs e)
        {
			// Ideally want to check tab and use information to save either EW or CTEM....
			List<DisplayTab> tabs = new List<DisplayTab>();
			tabs.Add(DiffDisplay);
			SaveImageFromTabs(tabs);
        }

		private void SaveImageFromTabs(List<DisplayTab> tabs)
		{				
			foreach (DisplayTab dt in tabs)
			{
				if (dt.xDim != 0 || dt.yDim != 0)
				{
					if (dt.Tab.IsSelected == true)
					{
						// File saving dialog
						Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();

						saveDialog.Title = "Save Output Image";
						saveDialog.DefaultExt = ".tiff";                     // Default file extension
						saveDialog.Filter = "TIFF Image (.tiff)|*.tiff"; // Filter files by extension

						Nullable<bool> result = saveDialog.ShowDialog();
						string filename = saveDialog.FileName;

						if (result == false)
							return;

						using (Tiff output = Tiff.Open(filename, "w"))
						{

							output.SetField(TiffTag.IMAGEWIDTH, dt.xDim);
							output.SetField(TiffTag.IMAGELENGTH, dt.yDim);
							output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
							output.SetField(TiffTag.SAMPLEFORMAT, 3);
							output.SetField(TiffTag.BITSPERSAMPLE, 32);
							output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
							output.SetField(TiffTag.ROWSPERSTRIP, dt.yDim);
							output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
							output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
							output.SetField(TiffTag.COMPRESSION, Compression.NONE);
							output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

							for (int i = 0; i < dt.yDim; ++i)
							{
								float[] buf = new float[dt.xDim];
								byte[] buf2 = new byte[4 * dt.xDim];

								for (int j = 0; j < dt.yDim; ++j)
								{
									buf[j] = dt.ImageData[j + dt.xDim * i];
								}

								Buffer.BlockCopy(buf, 0, buf2, 0, buf2.Length);
								output.WriteScanline(buf2, i);
							}
						}
					}
				}
			}
		}

        private void Button_Click_SimImage(object sender, RoutedEventArgs e)
        {
			//Disable simulate EW button for the duration
			SimulateEWButton.IsEnabled = false;

            mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, CurrentVoltage, ImagingParameters.spherical,
                                   ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

			// Calculate Dose Per Pixel
			float dpp = Convert.ToSingle(DoseTextBox.Text) * (CurrentPixelScale * CurrentPixelScale);
			// Get CCD and Binning

			var bincombo = BinningCombo.SelectedItem as ComboBoxItem;

			int binning = Convert.ToInt32(bincombo.Content);
			int CCD = CCDCombo.SelectedIndex;

			if (CCD != 0)
				mCL.SimulateCTEMImage(CCD,binning);
			else
				mCL.SimulateCTEMImage();

            //Update the displays
			UpdateCTEMImage(dpp, binning, CCD);
			UpdateDiffractionImage();

			SimulateEWButton.IsEnabled = true;
        }


		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			cancellationTokenSource.Cancel();
		}
    }
}
