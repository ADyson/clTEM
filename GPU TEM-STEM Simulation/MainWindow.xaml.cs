using System;
using System.Collections;
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
using System.Windows.Markup;
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
    public partial class MainWindow : Elysium.Controls.Window
    {
        bool IsResolutionSet = false;
        bool HaveStructure = false;
        bool IsSorted = false;
        bool doTDS_STEM = false;
        bool doTDS_CBED = false;
        bool isFull3D = true;
        bool isFD = false;
        bool DetectorVis = false;
        bool HaveMaxMrad = false;

        bool goodfinite = true;

        bool CBED_posValid = true;
        float CBED_xpos = 0;
        float CBED_ypos = 0;

        bool select_TEM = false;
        bool select_STEM = false;
        bool select_CBED = false;

        int Resolution;
        int CurrentResolution = 0;
        float CurrentPixelScale = 0;
        float CurrentWavelength = 0;
        float CurrentVoltage = 0;
        List<String> devicesShort;
        List<String> devicesLong;

        TEMParams ImagingParameters;
        TEMParams ProbeParameters;

//<<<<<<< HEAD
//
//=======
        // Simulation Options (updated before simulation call)
        float dz = 1.0f;
        int integrals = 10;
//>>>>>>> Elysium

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
        DisplayTab EWDisplay2 = new DisplayTab("EW2");
        DisplayTab DiffDisplay = new DisplayTab("Diffraction");

        public MainWindow()
        {
            InitializeComponent();

            //add event handlers here so they aren't called when creating controls
            CBEDxpos.TextChanged += new TextChangedEventHandler(CBEDValidCheck);
            CBEDypos.TextChanged += new TextChangedEventHandler(CBEDValidCheck);


            Full3DIntegrals.TextChanged += new TextChangedEventHandler(FiniteValidCheck);
            SliceDz.TextChanged += new TextChangedEventHandler(FiniteValidCheck);

			CancelButton.IsEnabled = false;

			// add constant tabs to UI
			LeftTab.Items.Add(CTEMDisplay.Tab);
			LeftTab.Items.Add(EWDisplay.Tab);
            LeftTab.Items.Add(EWDisplay2.Tab);
			RightTab.Items.Add(DiffDisplay.Tab);

            CTEMDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            EWDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            EWDisplay2.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
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
            SliceDz.Text = "1";
            Full3DIntegrals.Text = "20";

			BinningCombo.SelectedIndex = 0;
			CCDCombo.SelectedIndex = 0;

            // Add fake device names for now
            devicesShort = new List<String>();
            devicesLong = new List<String>();

            var numDev = mCL.getCLdevCount();

            for (var i = 0; i < numDev; i++)
            {
                devicesShort.Add(mCL.getCLdevString(i, true));
                devicesLong.Add(mCL.getCLdevString(i, false));
            }

            DeviceSelector.ItemsSource = devicesShort;
        }

        private void ImportStructureButton(object sender, RoutedEventArgs e)
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "file name",
                DefaultExt = ".xyz",
                Filter = "XYZ Coordinates (.xyz)|*.xyz"
            };

            // Set defaults for file dialog.

            var result = openDialog.ShowDialog();

            if (result == true)
            {
                var fName = openDialog.FileName;
                fileNameLabel.Text = System.IO.Path.GetFileName(fName);
                fileNameLabel.ToolTip = fName;

                // Now pass filename through to unmanaged where atoms can be imported inside structure class...
                mCL.importStructure(openDialog.FileName);
                mCL.uploadParameterisation();

                // Update some dialogs if everything went OK.
                var Len = 0;
                float MinX = 0;
                float MinY = 0;
                float MinZ = 0;
                float MaxX = 0;
                float MaxY = 0;
                float MaxZ = 0;

                mCL.getStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

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
                    mCL.sortStructure(false);
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
            select_TEM = TEMRadioButton.IsChecked == true;
            select_STEM = STEMRadioButton.IsChecked == true;
            select_CBED = CBEDRadioButton.IsChecked == true;

			if (!TestSimulationPrerequisites())
				return;

            Application.Current.Resources["Accent"] = Application.Current.Resources["ErrorColOrig"];

            CurrentResolution = Resolution;
            CurrentPixelScale = pixelScale;

            CurrentWavelength = wavelength;
            CurrentVoltage = ImagingParameters.kilovoltage;

            // DiffDisplay.tCanvas.Width = CurrentResolution;
            // DiffDisplay.tCanvas.Height = CurrentResolution;

            SimulateEWButton.IsEnabled = false;
            SimulateImageButton.IsEnabled = false;

            var TDSruns = 1;

            if (select_STEM)
            {
                TDSruns = Convert.ToInt32(STEM_TDSCounts.Text);
            }
            else if (select_CBED)
            {
                TDSruns = Convert.ToInt32(CBED_TDSCounts.Text);
            }

            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            var progressReporter = new ProgressReporter();

            // Pull options from dialog
            Single.TryParse(SliceDz.Text, out dz);
            Int32.TryParse(Full3DIntegrals.Text, out integrals);



			CancelButton.IsEnabled = false;
            var task = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                var timer = new Stopwatch();

                // Upload Simulation Parameters to c++ class
                mCL.setCTEMParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical, ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

                mCL.setSTEMParams(ProbeParameters.df, ProbeParameters.astigmag, ProbeParameters.astigang, ProbeParameters.kilovoltage, ProbeParameters.spherical, ProbeParameters.beta, ProbeParameters.delta, ProbeParameters.aperturemrad);

				SimulationMethod(select_TEM, select_STEM, select_CBED, TDSruns, ref progressReporter, ref timer, ref cancellationToken);

            }, cancellationToken);

            // This runs on UI Thread so can access UI, probably better way of doing image though.
            //SimWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            progressReporter.RegisterContinuation(task, () =>
            {
				CancelButton.IsEnabled = false;
                ProgressBar1.Value = 100;
                ProgressBar2.Value = 100;

                if (select_STEM)
                {
                    if (LockedDetectors.Count == 0)
                    {
                        SimulateEWButton.IsEnabled = true;
                        return;
                    }

                    foreach (var i in LockedDetectors)
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

                Application.Current.Resources["Accent"] = Application.Current.Resources["AccentOrig"];
                SimulateEWButton.IsEnabled = true;
            });

        }

		private void UpdateEWImage()
		{
			EWDisplay.xDim = CurrentResolution;
			EWDisplay.yDim = CurrentResolution;

			EWDisplay.ImgBmp = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			EWDisplay.tImage.Source = EWDisplay.ImgBmp;

			// When its completed we want to get data to c# for displaying in an image...
			EWDisplay.ImageData = new float[CurrentResolution * CurrentResolution];
			mCL.getEWImage(EWDisplay.ImageData, CurrentResolution);

			// Calculate the number of bytes per pixel (should be 4 for this format).
			var bytesPerPixel = (EWDisplay.ImgBmp.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride = EWDisplay.ImgBmp.PixelWidth * bytesPerPixel;

			// Create a byte array for a the entire size of bitmap.
			var arraySize = stride * EWDisplay.ImgBmp.PixelHeight;
			var pixelArray = new byte[arraySize];

			var min = mCL.getEWMin();
			var max = mCL.getEWMax();

			if (min == max)
				return;

			for (var row = 0; row < EWDisplay.ImgBmp.PixelHeight; row++)
				for (var col = 0; col < EWDisplay.ImgBmp.PixelWidth; col++)
				{
					pixelArray[(row * EWDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((EWDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * EWDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			var rect = new Int32Rect(0, 0, EWDisplay.ImgBmp.PixelWidth, EWDisplay.ImgBmp.PixelHeight);

			EWDisplay.ImgBmp.WritePixels(rect, pixelArray, stride, 0);

            EWDisplay2.xDim = CurrentResolution;
            EWDisplay2.yDim = CurrentResolution;

            EWDisplay2.ImgBmp = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
            EWDisplay2.tImage.Source = EWDisplay2.ImgBmp;

            // When its completed we want to get data to c# for displaying in an image...
            EWDisplay2.ImageData = new float[CurrentResolution * CurrentResolution];
            mCL.getEWImage2(EWDisplay2.ImageData, CurrentResolution);

            min = mCL.getEWMin2();
            max = mCL.getEWMax2();

            if (min == max)
                return;

            for (var row = 0; row < EWDisplay2.ImgBmp.PixelHeight; row++)
                for (var col = 0; col < EWDisplay2.ImgBmp.PixelWidth; col++)
                {
                    pixelArray[(row * EWDisplay2.ImgBmp.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((EWDisplay2.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
                    pixelArray[(row * EWDisplay2.ImgBmp.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((EWDisplay2.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
                    pixelArray[(row * EWDisplay2.ImgBmp.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((EWDisplay2.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
                    pixelArray[(row * EWDisplay2.ImgBmp.PixelWidth + col) * bytesPerPixel + 3] = 0;
                }


            rect = new Int32Rect(0, 0, EWDisplay2.ImgBmp.PixelWidth, EWDisplay2.ImgBmp.PixelHeight);

            EWDisplay2.ImgBmp.WritePixels(rect, pixelArray, stride, 0);
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

				EWDisplay.xStartPosition = SimRegion.xStart;
				EWDisplay.yStartPosition = SimRegion.yStart;

				SimulateTEM(ref progressReporter,ref timer, ref ct);
			}
			else if (select_STEM)
			{
                UInt64 mem = mCL.getCLdevGlobalMemory();
                UInt64 multi64 = mem / ((UInt64)CurrentResolution * (UInt64)CurrentResolution * 8 * 4);
                int multistem = (int)multi64;
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
			this.ProgressBar1.Value =
				Convert.ToInt32(100 * Convert.ToSingle(i) /
								Convert.ToSingle(NumberOfSlices));
			this.ProgressBar2.Value =
				Convert.ToInt32(100 * Convert.ToSingle(j) /
								Convert.ToSingle(runs));
			this.TimerMessage.Text = ms.ToString() + " ms";
			this.MemUsageLabel.Text = mem / (1024 * 1024) + " MB";
		}

		private void UpdateDiffractionImage()
		{

			DiffDisplay.xDim = CurrentResolution;
			DiffDisplay.yDim = CurrentResolution;

			DiffDisplay.ImgBmp = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			DiffDisplay.tImage.Source = DiffDisplay.ImgBmp;

			DiffDisplay.ImageData = new float[CurrentResolution * CurrentResolution];

			mCL.getDiffImage(DiffDisplay.ImageData, CurrentResolution);
			// Calculate the number of bytes per pixel (should be 4 for this format).
			var bytesPerPixel2 = (DiffDisplay.ImgBmp.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride2 = DiffDisplay.ImgBmp.PixelWidth * bytesPerPixel2;

			// Create a byte array for a the entire size of bitmap.
			var arraySize2 = stride2 * DiffDisplay.ImgBmp.PixelHeight;
			var pixelArray2 = new byte[arraySize2];

			var min2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.getDiffMin()+1.0f)));
			var max2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.getDiffMax()+1.0f)));

			if (min2 == max2)
				return;

			for (int row = 0; row < DiffDisplay.ImgBmp.PixelHeight; row++)
				for (int col = 0; col < DiffDisplay.ImgBmp.PixelWidth; col++)
				{
					pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(DiffDisplay.ImageData[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(DiffDisplay.ImageData[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(DiffDisplay.ImageData[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
					pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
				}


			var rect2 = new Int32Rect(0, 0, DiffDisplay.ImgBmp.PixelWidth, DiffDisplay.ImgBmp.PixelHeight);

			DiffDisplay.ImgBmp.WritePixels(rect2, pixelArray2, stride2, 0);

            // to update diffraction rings, show they have changed.
            foreach (var det in Detectors)
                det.SetEllipse(CurrentResolution, CurrentPixelScale, CurrentWavelength, DetectorVis);
		}

		private void UpdateTDSImage()
		{
			DiffDisplay.ImgBmp = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			DiffDisplay.tImage.Source = DiffDisplay.ImgBmp;

			DiffDisplay.xDim = CurrentResolution;
			DiffDisplay.yDim = CurrentResolution;
			DiffDisplay.ImageData = TDSImage;

            DiffDisplay.tCanvas.Width = CurrentResolution;
            DiffDisplay.tCanvas.Height = CurrentResolution;

			// Calculate the number of bytes per pixel (should be 4 for this format).
			var bytesPerPixel2 = (DiffDisplay.ImgBmp.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride2 = DiffDisplay.ImgBmp.PixelWidth * bytesPerPixel2;

			// Create a byte array for a the entire size of bitmap.
			var arraySize2 = stride2 * DiffDisplay.ImgBmp.PixelHeight;
			var pixelArray2 = new byte[arraySize2];

			var min2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.getDiffMin()+1.0f)));
            var max2 = Convert.ToSingle(Math.Log(Convert.ToDouble(mCL.getDiffMax()+1.0f)));

            for (var row = 0; row < DiffDisplay.ImgBmp.PixelHeight; row++)
                for (var col = 0; col < DiffDisplay.ImgBmp.PixelWidth; col++)
                {
                    pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(TDSImage[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(TDSImage[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((Convert.ToSingle(Math.Log(Convert.ToDouble(TDSImage[col + row * CurrentResolution]+1.0f))) - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * DiffDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                }


			var rect2 = new Int32Rect(0, 0, DiffDisplay.ImgBmp.PixelWidth, DiffDisplay.ImgBmp.PixelHeight);

			DiffDisplay.ImgBmp.WritePixels(rect2, pixelArray2, stride2, 0);

            // to update diffraction rings, show they have changed.
            foreach (var det in Detectors)
                det.SetEllipse(CurrentResolution, CurrentPixelScale, CurrentWavelength, DetectorVis);
		}

		private void SimulateTEM(ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{

		    mCL.initialiseCTEMSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D, isFD,dz,integrals);

			// Reset atoms incase TDS has been used
			mCL.sortStructure(false);

			// Use Background worker to progress through each step
			var NumberOfSlices = 0;
			mCL.getNumberSlices(ref NumberOfSlices, isFD);

			// Seperate into setup, loop over slices and final steps to allow for progress reporting.
			for (var i = 1; i <= NumberOfSlices; i++)
			{
				if (ct.IsCancellationRequested == true)
					break;

				timer.Start();
				mCL.doMultisliceStep(i, NumberOfSlices);
				timer.Stop();
				var mem = mCL.getCLMemoryUsed();
				// Report progress of the work.

				float ms = timer.ElapsedMilliseconds;
				progressReporter.ReportProgress((val) =>
				{
					CancelButton.IsEnabled = true;
					UI_UpdateSimulationProgress(ms, NumberOfSlices, 1, 1, i, mem);
				}, i);

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
            if (doTDS_STEM)
			{
				runs = TDSruns;
			}

			int totalPix = numPix * runs;

            mCL.initialiseSTEMSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D,isFD, dz, integrals, multistem);

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

			for (int j = 0; j < runs; j++)
			{
                Shuffler.Shuffle<Tuple<Int32, Int32>>(Pixels);

				// Reset image contrast limits for every run....
				foreach (DetectorItem i in LockedDetectors)
				{
					i.Min = float.MaxValue;
					i.Max = float.MinValue;
				}



				//for (int posY = 0; posY < LockedArea.yPixels * LockedArea.xPixels; posY+=multistem) // won't loop over end?
				//{
                int posY = 0;
                int conPix = multistem;
                // numPix
                // multistem
                while (posY < numPix)
                {
                    mCL.sortStructure(doTDS_STEM);

                    int thisPosY = posY;

                    if (posY + multistem > numPix && posY + multistem - numPix + 1 < multistem)
                    {
                        conPix = numPix - posY;
                        posY = numPix;
                    }
                    else
                        posY += multistem;


                    for (int i = 1; i <= conPix; i++)
                    {
                        mCL.initialiseSTEMWaveFunction(((LockedArea.xStart + Pixels[(thisPosY + i - 1)].Item1 * xInterval - SimRegion.xStart) / pixelScale),
                            ((LockedArea.yStart + Pixels[(thisPosY + i - 1)].Item2 * yInterval - SimRegion.yStart) / pixelScale), i);
                    }

                    // Use Background worker to progress through each step
                    int NumberOfSlices = 0;
                    mCL.getNumberSlices(ref NumberOfSlices, isFD);
                    // Seperate into setup, loop over slices and final steps to allow for progress reporting.

                    for (int i = 1; i <= NumberOfSlices; i++)
                    {
                        if (ct.IsCancellationRequested == true)
                            break;

                        timer.Start();
                        mCL.doMultisliceStep(i, NumberOfSlices, conPix);
                        timer.Stop();
                        int mem = mCL.getCLMemoryUsed();
                        float ms = timer.ElapsedMilliseconds;

                        progressReporter.ReportProgress((val) =>
                        {
                            CancelButton.IsEnabled = true;
                            // Note: code passed to "ReportProgress" can access UI elements freely.
                            UI_UpdateSimulationProgressSTEM(ms, totalPix, pix, NumberOfSlices, i, mem);
                        }, i);
                    }
                    pix += conPix;

                    if (ct.IsCancellationRequested == true)
                        break;

                    for (int i = 1; i <= conPix; i++)
                    {
                        // After a complete run if TDS need to sum up the DIFF...
                        //mCL.AddTDSDiffImage(TDSImages[i-1], CurrentResolution,i);
                        // Sum it in C++ also for the stem pixel measurement...
                        //mCL.AddTDS(i);
                        mCL.getSTEMDiff(i);
                    }

                    progressReporter.ReportProgress((val) =>
                    {
                        CancelButton.IsEnabled = false;
                        //UpdateTDSImage();
                    }, j);

                    for (int p = 1; p <= conPix; p++)
                    {
                        // loop through and get each STEM pixel for each detector at the same time
                        foreach (DetectorItem i in LockedDetectors)
                        {
                            float pixelVal = mCL.getSTEMPixel(i.Inner, i.Outer, i.xCentre, i.yCentre, p);
                            float newVal = i.ImageData[LockedArea.xPixels * Pixels[thisPosY + p - 1].Item2 + Pixels[thisPosY + p - 1].Item1] + pixelVal;
                            i.ImageData[LockedArea.xPixels * Pixels[thisPosY + p - 1].Item2 + Pixels[thisPosY + p - 1].Item1] = newVal;

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
                    }, thisPosY);

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
			mCL.initialiseSTEMSimulation(CurrentResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, isFull3D, isFD, dz, integrals,1);

			//int posX = CurrentResolution / 2;
			//int posY = CurrentResolution / 2;

            var posx = (CBED_xpos - SimRegion.xStart) / pixelScale;
            var posy = (CBED_ypos - SimRegion.yStart) / pixelScale;

			// Use Background worker to progress through each step
			var NumberOfSlices = 0;
            mCL.getNumberSlices(ref NumberOfSlices, isFD);
			// Seperate into setup, loop over slices and final steps to allow for progress reporting.

			var runs = 1;
			if (doTDS_CBED)
			{
				runs = TDSruns;
			}

			TDSImage = new float[CurrentResolution * CurrentResolution];

			for (var j = 0; j < runs; j++)
			{
				mCL.sortStructure(doTDS_CBED);
                mCL.initialiseSTEMWaveFunction(posx, posy,1);


				for (var i = 1; i <= NumberOfSlices; i++)
				{
					if (ct.IsCancellationRequested == true)
						break;

					timer.Start();
					mCL.doMultisliceStep(i, NumberOfSlices);
					timer.Stop();
					var mem = mCL.getCLMemoryUsed();
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
				//mCL.AddTDSDiffImage(TDSImage, CurrentResolution);
                mCL.getDiffImage(TDSImage, CurrentResolution);

				// Sum it in C++ also for the stem pixel measurement...
				//mCL.AddTDS();

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

			i.ImgBmp = new WriteableBitmap(LockedArea.xPixels, LockedArea.yPixels, 96, 96, PixelFormats.Bgr32, null);
			i.tImage.Source = i.ImgBmp;

			RenderOptions.SetBitmapScalingMode(i.tImage, BitmapScalingMode.NearestNeighbor);

			// Calculate the number of bytes per pixel (should be 4 for this format).
			var bytesPerPixel = (i.ImgBmp.Format.BitsPerPixel + 7) / 8;
			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride = i.ImgBmp.PixelWidth * bytesPerPixel;

			// Create a byte array for a the entire size of bitmap.
			var arraySize = stride * i.ImgBmp.PixelHeight;
			var pixelArray = new byte[arraySize];

			var min = i.Min;
			var max = i.Max;

			if (min == max)
				return;

			for (var row = 0; row < i.ImgBmp.PixelHeight; row++)
				for (var col = 0; col < i.ImgBmp.PixelWidth; col++)
				{
					pixelArray[(row * i.ImgBmp.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - min) / (max - min)) * 254.0f));
					pixelArray[(row * i.ImgBmp.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - min) / (max - min)) * 254.0f));
					pixelArray[(row * i.ImgBmp.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - min) / (max - min)) * 254.0f));
					pixelArray[(row * i.ImgBmp.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			var rect = new Int32Rect(0, 0, i.ImgBmp.PixelWidth, i.ImgBmp.PixelHeight);

			i.ImgBmp.WritePixels(rect, pixelArray, stride, 0);
		}

		private void UpdateCTEMImage(float dpp, int binning, int CCD)
		{
			// Get pixelscale from EW and apply to image.
			CTEMDisplay.PixelScaleY = EWDisplay.PixelScaleY;
			CTEMDisplay.PixelScaleX = EWDisplay.PixelScaleY;

			CTEMDisplay.xDim = CurrentResolution;
			CTEMDisplay.yDim = CurrentResolution;

			CTEMDisplay.xStartPosition = EWDisplay.xStartPosition;
			CTEMDisplay.yStartPosition = EWDisplay.yStartPosition;

			CTEMDisplay.ImgBmp = new WriteableBitmap(CurrentResolution, CurrentResolution, 96, 96, PixelFormats.Bgr32, null);
			CTEMDisplay.tImage.Source = CTEMDisplay.ImgBmp;

			// When its completed we want to get data to c# for displaying in an image...
			CTEMDisplay.ImageData = new float[CurrentResolution * CurrentResolution];

			if (CCD != 0)
				mCL.getCTEMImage(CTEMDisplay.ImageData, CurrentResolution, dpp, binning, CCD);
			else
				mCL.getCTEMImage(CTEMDisplay.ImageData, CurrentResolution);

			// Calculate the number of bytes per pixel (should be 4 for this format).
			var bytesPerPixel = (CTEMDisplay.ImgBmp.Format.BitsPerPixel + 7) / 8;

			// Stride is bytes per pixel times the number of pixels.
			// Stride is the byte width of a single rectangle row.
			var stride = CTEMDisplay.ImgBmp.PixelWidth * bytesPerPixel;

			// Create a byte array for a the entire size of bitmap.
			var arraySize = stride * CTEMDisplay.ImgBmp.PixelHeight;
			var pixelArray = new byte[arraySize];

			var min = mCL.getCTEMMin();
			var max = mCL.getCTEMMax();

			if (min == max)
				return;

			for (var row = 0; row < CTEMDisplay.ImgBmp.PixelHeight; row++)
				for (var col = 0; col < CTEMDisplay.ImgBmp.PixelWidth; col++)
				{
					pixelArray[(row * CTEMDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((CTEMDisplay.ImageData[col + row * CurrentResolution] - min) / (max - min)) * 254.0f));
					pixelArray[(row * CTEMDisplay.ImgBmp.PixelWidth + col) * bytesPerPixel + 3] = 0;
				}


			var rect = new Int32Rect(0, 0, CTEMDisplay.ImgBmp.PixelWidth, CTEMDisplay.ImgBmp.PixelHeight);

			CTEMDisplay.ImgBmp.WritePixels(rect, pixelArray, stride, 0);

			CTEMDisplay.Tab.IsSelected = true;
		}

		private void UI_UpdateSimulationProgressSTEM(float ms, int numPix, int pix, int NumberOfSlices, int i, int mem)
		{
			this.ProgressBar1.Value =
				Convert.ToInt32(100 * Convert.ToSingle(i) /
								Convert.ToSingle(NumberOfSlices));
			this.ProgressBar2.Value =
				Convert.ToInt32(100 * Convert.ToSingle(pix) /
								Convert.ToSingle(numPix));
			this.TimerMessage.Text = ms.ToString() + " ms";
			this.MemUsageLabel.Text = mem / (1024 * 1024) + " MB";
		}

		private void SaveImageButton_Click(object sender, RoutedEventArgs e)
		{
			var tabs = new List<DisplayTab> {CTEMDisplay,EWDisplay,EWDisplay2};
		    tabs.AddRange(LockedDetectors);

            SaveImageFromTabs(tabs);
		}

        private void SaveImageButton2_Click(object sender, RoutedEventArgs e)
        {
			// Ideally want to check tab and use information to save either EW or CTEM....
			var tabs = new List<DisplayTab> {DiffDisplay};
            SaveImageFromTabs(tabs);
        }

		private void SaveImageFromTabs(IEnumerable<DisplayTab> tabs)
		{
			foreach (var dt in tabs)
			{
				if (dt.xDim != 0 || dt.yDim != 0)
				{
					if (dt.Tab.IsSelected == true)
					{
						// File saving dialog
						var saveDialog = new Microsoft.Win32.SaveFileDialog
						{
						    Title = "Save Output Image",
						    DefaultExt = ".tiff",
						    Filter = "TIFF (*.tiff)|*.tiff|PNG (*.png)|*.png|JPEG (*.jpeg)|*.jpeg"
						};

					    var result = saveDialog.ShowDialog();
						var filename = saveDialog.FileName;

						if (result == false)
							return;

                        if (filename.EndsWith(".tiff"))
                        {
    						using (var output = Tiff.Open(filename, "w"))
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

    							for (var i = 0; i < dt.yDim; ++i)
    							{
    								var buf = new float[dt.xDim];
    								var buf2 = new byte[4 * dt.xDim];

    								for (var j = 0; j < dt.yDim; ++j)
    								{
    									buf[j] = dt.ImageData[j + dt.xDim * i];
    								}

    								Buffer.BlockCopy(buf, 0, buf2, 0, buf2.Length);
    								output.WriteScanline(buf2, i);
    							}
    						}
                        }
                        else if (filename.EndsWith(".png"))
                        {
                            using (var stream = new FileStream(filename, FileMode.Create))
                            {
                                var encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(dt.ImgBmp.Clone()));
                                encoder.Save(stream);
                                stream.Close();
                            }
                        }
                        else if (filename.EndsWith(".jpeg"))
                        {
                            using (var stream = new FileStream(filename, FileMode.Create))
                            {
                                var encoder = new JpegBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(dt.ImgBmp.Clone()));
                                encoder.Save(stream);
                                stream.Close();
                            }
                        }
					}
				}
			}
		}

        private void Button_Click_SimImage(object sender, RoutedEventArgs e)
        {

			if (!TestImagePrerequisites())
				return;

			//Disable simulate EW button for the duration
			SimulateEWButton.IsEnabled = false;

            mCL.setCTEMParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, CurrentVoltage, ImagingParameters.spherical,
                                   ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

			// Calculate Dose Per Pixel
			var dpp = Convert.ToSingle(DoseTextBox.Text) * (CurrentPixelScale * CurrentPixelScale);
			// Get CCD and Binning

			var bincombo = BinningCombo.SelectedItem as ComboBoxItem;

			var binning = Convert.ToInt32(bincombo.Content);
			var CCD = CCDCombo.SelectedIndex;

			if (CCD != 0)
				mCL.simulateCTEM(CCD,binning);
			else
				mCL.simulateCTEM();

            //Update the displays
			UpdateCTEMImage(dpp, binning, CCD);
			UpdateDiffractionImage();

			SimulateEWButton.IsEnabled = true;
        }
    }
}
