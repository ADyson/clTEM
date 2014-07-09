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
        bool IsResolutionSet;
        bool HaveStructure;
        bool IsSorted;
        bool TDS;
        int Resolution;

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

        DisplayTab CTEMDisplay = new DisplayTab("CTEM");
        DisplayTab EWDisplay = new DisplayTab("EW");
        DisplayTab DiffDisplay = new DisplayTab("Diffraction");

        public MainWindow()
        {
            InitializeComponent();

			LeftTab.Items.Add(CTEMDisplay.Tab);
			LeftTab.Items.Add(EWDisplay.Tab);
			RightTab.Items.Add(DiffDisplay.Tab);

            // Start in TEM mode.
            TEMRadioButton.IsChecked = true;

            ImagingParameters = new TEMParams();
            ProbeParameters = new TEMParams();

            // Setup Managed Wrapper and Upload Atom Parameterisation ready for Multislice calculations.
            // Moved parameterisation, will be redone each time we get new structure now :(
            mCL = new ManagedOpenCL();
            
            IsResolutionSet = false;
            HaveStructure = false;
            IsSorted = false;
            TDS = false;

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

            //DataContext = this;

            // Must be set twice
            DeviceSelector.SelectedIndex = -1;
            DeviceSelector.SelectedIndex = -1;

			BinningCombo.SelectedIndex = 0;
			CCDCombo.SelectedIndex = 0;

            // Add fake device names for now
            devicesShort = new List<String>();
            devicesLong = new List<String>();
            //devices.Add("CPU");
            //devices.Add("GPU");
            int numDev = mCL.getCLdevCount();

            for (int i = 0; i < numDev; i++)
            {
                devicesShort.Add(mCL.getCLdevString(i, true));
                devicesLong.Add(mCL.getCLdevString(i, false));
            }

            DeviceSelector.ItemsSource = devicesShort;

        }

        private void UpdateMaxMrad()
        {

            if (!HaveStructure)
                return;

            float MinX = SimRegion.xStart;
            float MinY = SimRegion.yStart;

            float MaxX = SimRegion.xFinish;
            float MaxY = SimRegion.yFinish;

            float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
            // Determine max mrads for reciprocal space, (need wavelength)...
            float MaxFreq = 1 / (2 * BiggestSize / Resolution);

            if (ImagingParameters.kilovoltage != 0 && IsResolutionSet)
            {
                float echarge = 1.6e-19f;
                wavelength = Convert.ToSingle(6.63e-034 * 3e+008 / Math.Sqrt((echarge * ImagingParameters.kilovoltage * 1000 * 
                    (2 * 9.11e-031 * 9e+016 + echarge * ImagingParameters.kilovoltage * 1000))) * 1e+010);

                float mrads = (1000 * MaxFreq * wavelength) / 2;

                MaxMradsLabel.Content = mrads.ToString("f2")+" mrad";
            }
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

                if (IsResolutionSet)
                {
                    //float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
                    float BiggestSize = Math.Max(SimRegion.xFinish - SimRegion.xStart, SimRegion.yFinish - SimRegion.yStart);
                    pixelScale = BiggestSize / Resolution;
                    PixelScaleLabel.Content = pixelScale.ToString("f2") + " Å";

                    UpdateMaxMrad();
                }

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

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            Resolution = Convert.ToInt32(ResolutionCombo.SelectedValue.ToString());

            IsResolutionSet = true;

            if (!userSTEMarea)
            {
                STEMRegion.xPixels = Resolution;
                STEMRegion.yPixels = Resolution;
            }

            if (HaveStructure)
            {
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

                float BiggestSize = Math.Max(SimRegion.xFinish - SimRegion.xStart, SimRegion.yFinish - SimRegion.yStart);
                pixelScale = BiggestSize / Resolution;
                PixelScaleLabel.Content = pixelScale.ToString("f2") + " Å";

                UpdateMaxMrad();
            }
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

            SimulateEWButton.IsEnabled = false;
            SimulateImageButton.IsEnabled = false;

            bool select_TEM = TEMRadioButton.IsChecked == true;
            bool select_STEM = STEMRadioButton.IsChecked == true;
            bool select_CBED = CBEDRadioButton.IsChecked == true;

            int TDSruns = Convert.ToInt32(TDSCounts.Text);

            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            var progressReporter = new ProgressReporter();
            var task = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                Stopwatch timer = new Stopwatch();

                // Upload Simulation Parameters to c++ class
                mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical, ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

                mCL.SetStemParams(ProbeParameters.df, ProbeParameters.astigmag, ProbeParameters.astigang, ProbeParameters.kilovoltage, ProbeParameters.spherical, ProbeParameters.beta, ProbeParameters.delta, ProbeParameters.aperturemrad);

				SimulationMethod(select_TEM, select_STEM, select_CBED, TDSruns, ref progressReporter, ref timer);
                
            }, cancellationToken);

            // This runs on UI Thread so can access UI, probably better way of doing image though.
            //SimWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            progressReporter.RegisterContinuation(task, () =>
            {
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
                    DiffDisplay.ImageData = new float[Resolution * Resolution];
                    TDSImage.CopyTo(DiffDisplay.ImageData, 0);
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
			EWDisplay.xDim = Resolution;
			EWDisplay.yDim = Resolution;

			EWDisplay._ImgBMP = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
			EWDisplay.tImage.Source = EWDisplay._ImgBMP;

			// When its completed we want to get data to c# for displaying in an image...
			EWDisplay.ImageData = new float[Resolution * Resolution];
			mCL.GetEWImage(EWDisplay.ImageData, Resolution);


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

			for (int row = 0; row < EWDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < EWDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * Resolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * Resolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * Resolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			Int32Rect rect = new Int32Rect(0, 0, EWDisplay._ImgBMP.PixelWidth, EWDisplay._ImgBMP.PixelHeight);

			EWDisplay._ImgBMP.WritePixels(rect, pixelArray, stride, 0);
		}

		private void SimulationMethod(bool select_TEM, bool select_STEM, bool select_CBED, int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer)
		{
			if (select_TEM)
			{
				SimulateTEM(ref progressReporter,ref timer);
			}
			else if (select_STEM)
			{
				SimulateSTEM(TDSruns, ref progressReporter, ref timer);
			}
			else if (select_CBED)
			{
				SimulateCBED(TDSruns, ref progressReporter,ref timer);
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
			DiffDisplay.xDim = Resolution;
			DiffDisplay.yDim = Resolution;
			
			DiffDisplay._ImgBMP = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
			DiffDisplay.tImage.Source = DiffDisplay._ImgBMP;

			DiffDisplay.ImageData = new float[Resolution * Resolution];

			mCL.GetDiffImage(DiffDisplay.ImageData, Resolution);
			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel2 = (DiffDisplay._ImgBMP.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride2 = DiffDisplay._ImgBMP.PixelWidth * bytesPerPixel2;

			// Create a byte array for a the entire size of bitmap.
			var arraySize2 = stride2 * DiffDisplay._ImgBMP.PixelHeight;
			var pixelArray2 = new byte[arraySize2];

			float min2 = mCL.GetDiffMin();
			float max2 = mCL.GetDiffMax();

			for (int row = 0; row < DiffDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < DiffDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((DiffDisplay.ImageData[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((DiffDisplay.ImageData[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((DiffDisplay.ImageData[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
				}


			Int32Rect rect2 = new Int32Rect(0, 0, DiffDisplay._ImgBMP.PixelWidth, DiffDisplay._ImgBMP.PixelHeight);

			DiffDisplay._ImgBMP.WritePixels(rect2, pixelArray2, stride2, 0);
		}

		private void UpdateTDSImage()
		{
			DiffDisplay._ImgBMP = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
			DiffDisplay.tImage.Source = DiffDisplay._ImgBMP;

			// Calculate the number of bytes per pixel (should be 4 for this format). 
			var bytesPerPixel2 = (DiffDisplay._ImgBMP.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride2 = DiffDisplay._ImgBMP.PixelWidth * bytesPerPixel2;

			// Create a byte array for a the entire size of bitmap.
			var arraySize2 = stride2 * DiffDisplay._ImgBMP.PixelHeight;
			var pixelArray2 = new byte[arraySize2];

			float min2 = mCL.GetDiffMin();
			float max2 = mCL.GetDiffMax();

			for (int row = 0; row < DiffDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < DiffDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
				}


			Int32Rect rect2 = new Int32Rect(0, 0, DiffDisplay._ImgBMP.PixelWidth, DiffDisplay._ImgBMP.PixelHeight);

			DiffDisplay._ImgBMP.WritePixels(rect2, pixelArray2, stride2, 0);
		}

		private void SimulateTEM(ref ProgressReporter progressReporter, ref Stopwatch timer)
		{
			mCL.InitialiseSimulation(Resolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish);

			// Reset atoms incase TDS has been used
			mCL.SortStructure(false);

			// Use Background worker to progress through each step
			int NumberOfSlices = 0;
			mCL.GetNumberSlices(ref NumberOfSlices);
			// Seperate into setup, loop over slices and final steps to allow for progress reporting.

			for (int i = 1; i <= NumberOfSlices; i++)
			{
				timer.Start();
				mCL.MultisliceStep(i, NumberOfSlices);
				timer.Stop();
				int mem = mCL.MemoryUsed();
				// Report progress of the work. 

				float ms = timer.ElapsedMilliseconds;
				progressReporter.ReportProgress((val) =>
				{
					UI_UpdateSimulationProgress(ms, NumberOfSlices, 1, 1, i, mem);
				}, i);

			}
		}
		
		private void SimulateSTEM(int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer)
		{
			LockedDetectors = Detectors;
			LockedArea = STEMRegion;

			if (LockedDetectors.Count == 0)
			{
				var result = MessageBox.Show("No Detectors Have Been Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			progressReporter.ReportProgress((val) =>
			{
				DiffDisplay.tCanvas.Children.Clear();

				DiffDisplay.tCanvas.Width = Resolution;
				DiffDisplay.tCanvas.Height = Resolution;

				// enable checkbox here if it is implemented?
				// will also possibly change initial visibility of ellipses

				ColourGenerator.ColourGenerator cgen = new ColourGenerator.ColourGenerator();
				var converter = new System.Windows.Media.BrushConverter();

				foreach (DetectorItem i in LockedDetectors)
				{
					// calculate the radii and reset properties
					i.setEllipse(Resolution, pixelScale, wavelength);

					// add to canvas
					i.AddToCanvas(DiffDisplay.tCanvas);
				}
			}, 0);


			int numPix = LockedArea.xPixels * LockedArea.yPixels;
			int pix = 0;

			foreach (DetectorItem i in LockedDetectors)
			{
				i.ImageData = new float[numPix];
			}

			int runs = 1;
			if (TDS)
			{
				runs = TDSruns;
			}

			numPix *= runs;

			mCL.InitialiseSTEMSimulation(Resolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish);

			float xInterval = LockedArea.getxInterval;
			float yInterval = LockedArea.getyInterval;

			for (int posY = 0; posY < LockedArea.yPixels; posY++)
			{
				float fCoordy = (LockedArea.yStart + posY * yInterval) / pixelScale;

				for (int posX = 0; posX < LockedArea.xPixels; posX++)
				{
					TDSImage = new float[Resolution * Resolution];

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

							timer.Start();
							mCL.MultisliceStep(i, NumberOfSlices);
							timer.Stop();
							int mem = mCL.MemoryUsed();
							float ms = timer.ElapsedMilliseconds;

							progressReporter.ReportProgress((val) =>
							{
								// Note: code passed to "ReportProgress" can access UI elements freely. 
								UI_UpdateSimulationProgressSTEM(ms, numPix, pix, NumberOfSlices, i, mem);
							}, i);
						}
						pix++;

						// After a complete run if TDS need to sum up the DIFF...
						mCL.AddTDSDiffImage(TDSImage, Resolution);
						// Sum it in C++ also for the stem pixel measurement...
						mCL.AddTDS();

						progressReporter.ReportProgress((val) =>
						{
							UpdateTDSImage();
						}, j);
					}

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

			}
		}

		private void SimulateCBED(int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer)
		{
			int numPix = 1;
			int pix = 0;

			mCL.InitialiseSTEMSimulation(Resolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish);

			int posX = Resolution / 2;
			int posY = Resolution / 2;

			mCL.MakeSTEMWaveFunction(posX, posY);

			// Use Background worker to progress through each step
			int NumberOfSlices = 0;
			mCL.GetNumberSlices(ref NumberOfSlices);
			// Seperate into setup, loop over slices and final steps to allow for progress reporting.

			int runs = 1;
			if (TDS)
			{
				runs = TDSruns;
			}

			TDSImage = new float[Resolution * Resolution];

			for (int j = 0; j < runs; j++)
			{
				mCL.SortStructure(TDS);

				for (int i = 1; i <= NumberOfSlices; i++)
				{
					timer.Start();
					mCL.MultisliceStep(i, NumberOfSlices);
					timer.Stop();
					int mem = mCL.MemoryUsed();
					float ms = timer.ElapsedMilliseconds;

					// Report progress of the work. 
					progressReporter.ReportProgress((val) =>
					{
						// Note: code passed to "ReportProgress" can access UI elements freely. 
						UI_UpdateSimulationProgress(ms, NumberOfSlices, runs, j, i, mem);
					}, i);
				}

				// After a complete run if TDS need to sum up the DIFF...
				mCL.AddTDSDiffImage(TDSImage, Resolution);
				// Sum it in C++ also for the stem pixel measurement...
				mCL.AddTDS();

				progressReporter.ReportProgress((val) =>
				{
					UpdateDiffractionImage();
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
			CTEMDisplay.xDim = Resolution;
			CTEMDisplay.yDim = Resolution;

			CTEMDisplay._ImgBMP = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
			CTEMDisplay.tImage.Source = CTEMDisplay._ImgBMP;

			// When its completed we want to get data to c# for displaying in an image...
			CTEMDisplay.ImageData = new float[Resolution * Resolution];

			if (CCD != 0)
				mCL.GetCTEMImage(CTEMDisplay.ImageData, Resolution, dpp, binning, CCD);
			else
				mCL.GetCTEMImage(CTEMDisplay.ImageData, Resolution);

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

			for (int row = 0; row < CTEMDisplay._ImgBMP.PixelHeight; row++)
				for (int col = 0; col < CTEMDisplay._ImgBMP.PixelWidth; col++)
				{
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * Resolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * Resolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay._ImgBMP.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * Resolution] - min) / (max - min)) * 254.0f));
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
							output.SetField(TiffTag.ROWSPERSTRIP, Resolution);
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
            mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical,
                                   ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);


			// Calculate Dose Per Pixel
			float dpp = Convert.ToSingle(DoseTextBox.Text) * (pixelScale * pixelScale);
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

        }

    }
}
