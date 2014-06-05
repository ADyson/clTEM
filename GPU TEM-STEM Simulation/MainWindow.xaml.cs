using System;
using System.ComponentModel;
using System.Collections;
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
//using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using ManagedOpenCLWrapper;
using BitMiracle.LibTiff.Classic;
using PanAndZoom;
using WPFChart3D;

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

        private static WriteableBitmap CTEMImg;
        public static WriteableBitmap _CTEMImg
        {
            get { return MainWindow.CTEMImg; }
            set { MainWindow.CTEMImg = value; }
        }

        private static WriteableBitmap EWImg;
        public static WriteableBitmap _EWImg
        {
            get { return MainWindow.EWImg; }
            set { MainWindow.EWImg = value; }
        }


        private static WriteableBitmap DiffImg;
        public static WriteableBitmap _DiffImg
        {
            get { return MainWindow.DiffImg; }
            set { MainWindow.DiffImg = value; }
        }

        // Arrays to store image data
        float[] CTEMImage;
        float[] EWImage;
        float[] DiffImage;
        float[] TDSImage;
        public List<DetectorItem> Detectors = new List<DetectorItem>();
        List<DetectorItem> LockedDetectors;

        public STEMArea STEMRegion = new STEMArea { xStart = 0, xFinish = 1, yStart = 0, yFinish = 1, xPixels = 1, yPixels = 1 };
        STEMArea LockedArea;
        float pixelScale;

        //private List<ModelVisual3D> sphereModels = new List<ModelVisual3D>();

        private WPFChart3D.Chart3D m_3dChart;
		private ArrayList meshs;

        public WPFChart3D.TransformMatrix m_transformMatrix = new WPFChart3D.TransformMatrix();
        public int m_nChartModelIndex = -1;

        private void UpdateMaxMrad()
        {

            if (!HaveStructure)
                return;

            int Len = 0;
            float MinX = 0;
            float MinY = 0;
            float MinZ = 0;
            float MaxX = 0;
            float MaxY = 0;
            float MaxZ = 0;

            mCL.GetStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

            float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
            // Determine max mrads for reciprocal space, (need wavelength)...
            float MaxFreq = 1 / (2 * BiggestSize / Resolution);

            if (ImagingParameters.kilovoltage != 0 && IsResolutionSet)
            {
                float echarge = 1.6e-19f;
                float wavelength = Convert.ToSingle(6.63e-034 * 3e+008 / Math.Sqrt((echarge * ImagingParameters.kilovoltage * 1000 * 
                    (2 * 9.11e-031 * 9e+016 + echarge * ImagingParameters.kilovoltage * 1000))) * 1e+010);

                float mrads = 1000 * MaxFreq * wavelength;

                MaxMradsLabel.Content = "Max reciprocal (mrad): " + mrads.ToString("f2");
            }

        }

        new private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowedFloatNumber(e.Text);
        }

        private static bool IsTextAllowedFloatNumber(string text)
        {
           //Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
           Regex regex = new Regex(@"^[-]?(0|[1-9][0-9]*)?(\.[0-9]*)?([eE][+-]?[0-9]+)?$");
           //return !regex.IsMatch(text);
           return regex.IsMatch(text);
            
        }


        public MainWindow()
        {

            InitializeComponent();
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
            Imagingbeta.Text = "0.005";
            Imagingdelta.Text = "3";
            ImagingDf.Text = "0";
            ImagingA2.Text = "0";
            ImagingA2Phi.Text = "0";

            //DataContext = this;

            // Must be set twice
            DeviceSelector.SelectedIndex = -1;
            DeviceSelector.SelectedIndex = -1;

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

		private async void ImportStructureButton(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();

               // Set defaults for file dialog.
            openDialog.FileName = "file name";                  // Default file name
            openDialog.DefaultExt = ".xyz";                     // Default file extension
            openDialog.Filter = "XYZ Coordinates (.xyz)|*.xyz"; // Filter files by extension

            Nullable<bool> result = openDialog.ShowDialog();

            if (result == true)
            {
                fileNameLabel.Content = openDialog.FileName;
                
				this.cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = this.cancellationTokenSource.Token;
                var progressReporter = new ProgressReporter();

				var task = Task.Factory.StartNew(() =>
				{
					 // Now pass filename through to unmanaged where atoms can be imported inside structure class...
					 mCL.ImportStructure(openDialog.FileName);
					 mCL.UploadParameterisation();
				}, cancellationToken).ContinueWith((sort) => 
				{
					mCL.SortStructure(TDS);
				},cancellationToken,TaskContinuationOptions.LongRunning,TaskScheduler.Default);

				Task.Factory.ContinueWhenAll(new[] {task}, ts =>
				{
					IsSorted = true;

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

					WidthLabel.Content = "Width (A): " + (MaxX - MinX).ToString("f2");
					HeightLabel.Content = "Height (A): " + (MaxY - MinY).ToString("f2");
					DepthLabel.Content = "Depth (A): " + (MaxZ - MinZ).ToString("f2");
					atomNumberLabel.Content = Len.ToString() + " Atoms";

					if (IsResolutionSet)
					{
						float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
						pixelScale = BiggestSize / Resolution;
						PixelScaleLabel.Content = "Pixel Size (A): " + pixelScale.ToString("f2");

						UpdateMaxMrad();
					}

					DrawAtoms();
				},cancellationToken,TaskContinuationOptions.None,TaskScheduler.FromCurrentSynchronizationContext());

				
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

                WidthLabel.Content = "Width (A): " + (MaxX - MinX).ToString("f2");
                HeightLabel.Content = "Height (A): " + (MaxY - MinY).ToString("f2");
                DepthLabel.Content = "Depth (A): " + (MaxZ - MinZ).ToString("f2");
                atomNumberLabel.Content = Len.ToString() + " Atoms";

                float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
                pixelScale = BiggestSize / Resolution;
                PixelScaleLabel.Content = "Pixel Size (A): " + pixelScale.ToString("f2");

                UpdateMaxMrad();
            }
        }

        // Simulation Button
        private void Button_Click_1(object sender, RoutedEventArgs e)
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

            // Do Simulation in a background worker
            //Cancel += CancelProcess;
            //progressBar1.Minimum = 0;
            // progressBar1.Maximum = 100;

            //System.Windows.Threading.Dispatcher mwDispatcher = this.Dispatcher;
            //SimWorker = new BackgroundWorker();

            // Changed to alternate model of progress reporting
            //worker.WorkerReportsProgress = true;
            //SimWorker.WorkerSupportsCancellation = true;
            bool select_TEM = TEMRadioButton.IsChecked == true;
            bool select_STEM = STEMRadioButton.IsChecked == true;
            bool select_CBED = CBEDRadioButton.IsChecked == true;

            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            var progressReporter = new ProgressReporter();
            var task = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                Stopwatch timer = new Stopwatch();

                // Upload Simulation Parameters to c++ class
                mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical,
                                    ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

                mCL.SetStemParams(ProbeParameters.df, ProbeParameters.astigmag, ProbeParameters.astigang, ProbeParameters.kilovoltage, ProbeParameters.spherical,
                                 ProbeParameters.beta, ProbeParameters.delta, ProbeParameters.aperturemrad);

                // Will call different functions depending on type of simulation required, or just send flags to allow subsections to be performed differently

                if (select_TEM)
                {

                    mCL.InitialiseSimulation(Resolution);

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
                        progressReporter.ReportProgress((val) =>
                        {
                            // Note: code passed to "ReportProgress" can access UI elements freely. 
                            this.progressBar1.Value =
                                Convert.ToInt32(100*Convert.ToSingle(i)/
                                                Convert.ToSingle(NumberOfSlices));
                            this.statusmessage.Content = timer.ElapsedMilliseconds.ToString() + " ms."; 
                            this.statusmessage2.Content = mem/(1024*1024) + " MB.";
                        },i);
                      
                    }
                }
                else if (select_STEM)
                {

                    LockedDetectors = Detectors;
                    LockedArea = STEMRegion;

                    if (LockedDetectors.Count == 0)
                    {
                        var result = MessageBox.Show("No Detectors Have Been Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        //return 0;
                    }

                    int numPix = LockedArea.xPixels * LockedArea.yPixels;
                    int pix = 0;

                    foreach (DetectorItem i in LockedDetectors)
                    {
                        i.ImageData = new float[numPix];
                    }
                    
                    int runs = 1;
                    if (TDS)
                        runs = 10;

                    numPix *= runs;

                    mCL.InitialiseSTEMSimulation(Resolution);

                    float xInterval = LockedArea.getxInterval;
                    float yInterval = LockedArea.getyInterval;

                    for (int posY = 0; posY < LockedArea.yPixels; posY++)
                    {
                        float fCoordy = (LockedArea.yStart + posY * yInterval)/pixelScale;

                        for (int posX = 0; posX < LockedArea.xPixels; posX++)
                        {
                            TDSImage = new float[Resolution * Resolution];

                            for (int j = 0; j < runs; j++)
                            {    
    							// if TDS was used last atoms are in wrong place and need resetting via same function
                                // if (TDS)
                                    mCL.SortStructure(TDS);

                                float fCoordx = (LockedArea.xStart + posX * xInterval)/pixelScale;

                                mCL.MakeSTEMWaveFunction(fCoordx, fCoordy);

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
                               
                                    progressReporter.ReportProgress((val) =>
                                    {
                                        // Note: code passed to "ReportProgress" can access UI elements freely. 
                                        this.progressBar1.Value =
                                            Convert.ToInt32(100 * Convert.ToSingle(i) /
                                                            Convert.ToSingle(NumberOfSlices));
                                        this.progressBar2.Value =
                                            Convert.ToInt32(100 * Convert.ToSingle(pix) /
                                                            Convert.ToSingle(numPix));
                                        this.statusmessage.Content = timer.ElapsedMilliseconds.ToString() + " ms.";
										this.statusmessage2.Content = mem / (1024 * 1024) + " MB.";
                                    }, i);
                                }
                                pix++;
                                
                                // After a complete run if TDS need to sum up the DIFF...
                                mCL.AddTDSDiffImage(TDSImage, Resolution);
                                // Sum it in C++ also for the stem pixel measurement...
                                mCL.AddTDS();

                                progressReporter.ReportProgress((val) =>
                                {

                                    _DiffImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                                    DiffImageDisplay.Source = _DiffImg;


                                    // Calculate the number of bytes per pixel (should be 4 for this format). 
                                    var bytesPerPixel2 = (_DiffImg.Format.BitsPerPixel + 7) / 8;

                                    // Stride is bytes per pixel times the number of pixels.
                                    // Stride is the byte width of a single rectangle row.
                                    var stride2 = _DiffImg.PixelWidth * bytesPerPixel2;

                                    // Create a byte array for a the entire size of bitmap.
                                    var arraySize2 = stride2 * _DiffImg.PixelHeight;
                                    var pixelArray2 = new byte[arraySize2];

                                    float min2 = mCL.GetDiffMin();
                                    float max2 = mCL.GetDiffMax();

                                    for (int row = 0; row < _DiffImg.PixelHeight; row++)
                                        for (int col = 0; col < _DiffImg.PixelWidth; col++)
                                        {
                                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                                        }


                                    Int32Rect rect2 = new Int32Rect(0, 0, _DiffImg.PixelWidth, _DiffImg.PixelHeight);

                                    _DiffImg.WritePixels(rect2, pixelArray2, stride2, 0);
                                },j);
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
                                    i._ImgBMP = new WriteableBitmap(LockedArea.xPixels, LockedArea.yPixels, 96, 96, PixelFormats.Bgr32, null);
                                    i.Image.Source = i._ImgBMP;

                                    RenderOptions.SetBitmapScalingMode(i.Image, BitmapScalingMode.NearestNeighbor);

                                    // Calculate the number of bytes per pixel (should be 4 for this format). 
                                    var bytesPerPixelBF = (i._ImgBMP.Format.BitsPerPixel + 7) / 8;

                                    // Stride is bytes per pixel times the number of pixels.
                                    // Stride is the byte width of a single rectangle row.
                                    var strideBF = i._ImgBMP.PixelWidth * bytesPerPixelBF;

                                    // Create a byte array for a the entire size of bitmap.
                                    var arraySizeBF = strideBF * i._ImgBMP.PixelHeight;
                                    var pixelArrayBF = new byte[arraySizeBF];

                                    float minBF = i.Min;
                                    float maxBF = i.Max;

                                    if (minBF == maxBF)
                                        break;

                                    for (int row = 0; row < i._ImgBMP.PixelHeight; row++)
                                        for (int col = 0; col < i._ImgBMP.PixelWidth; col++)
                                        {
                                            pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 0] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - minBF) / (maxBF - minBF)) * 254.0f));
                                            pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 1] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - minBF) / (maxBF - minBF)) * 254.0f));
                                            pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 2] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - minBF) / (maxBF - minBF)) * 254.0f));
                                            pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 3] = 0;
                                        }


                                    Int32Rect rectBF = new Int32Rect(0, 0, i._ImgBMP.PixelWidth, i._ImgBMP.PixelHeight);

                                    i._ImgBMP.WritePixels(rectBF, pixelArrayBF, strideBF, 0);
                                }
							},posX);

                            // Reset TDS arrays after pixel values retrieved...
                            mCL.ClearTDS();

                        }

                    }
                }
                else if (select_CBED)
                {
                    int numPix = 1;
                    int pix = 0;

                    mCL.InitialiseSTEMSimulation(Resolution);

                    int posX = Resolution/2;
                    int posY = Resolution/2;

                    mCL.MakeSTEMWaveFunction(posX, posY);

                    // Use Background worker to progress through each step
                    int NumberOfSlices = 0;
                    mCL.GetNumberSlices(ref NumberOfSlices);
                    // Seperate into setup, loop over slices and final steps to allow for progress reporting.

                    int runs = 1;
                    if (TDS)
                        runs = 10;

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

                            // Report progress of the work. 
                            progressReporter.ReportProgress((val) =>
                            {
                                // Note: code passed to "ReportProgress" can access UI elements freely. 
                                this.progressBar1.Value =
                                    Convert.ToInt32(100 * Convert.ToSingle(i) /
                                                    Convert.ToSingle(NumberOfSlices));
                                this.progressBar2.Value =
                                    Convert.ToInt32(100 * Convert.ToSingle(j) /
                                                    Convert.ToSingle(runs));
                                this.statusmessage.Content = timer.ElapsedMilliseconds.ToString() + " ms.";
								this.statusmessage2.Content = mem / (1024 * 1024) + " MB.";
                            }, i);
                        }
             
                        // After a complete run if TDS need to sum up the DIFF...
                        mCL.AddTDSDiffImage(TDSImage, Resolution);
                        // Sum it in C++ also for the stem pixel measurement...
                        mCL.AddTDS();
                        
                        progressReporter.ReportProgress((val) =>
                        {
                            _DiffImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                            DiffImageDisplay.Source = _DiffImg;


                            // Calculate the number of bytes per pixel (should be 4 for this format). 
                            var bytesPerPixel2 = (_DiffImg.Format.BitsPerPixel + 7) / 8;

                            // Stride is bytes per pixel times the number of pixels.
                            // Stride is the byte width of a single rectangle row.
                            var stride2 = _DiffImg.PixelWidth * bytesPerPixel2;

                            // Create a byte array for a the entire size of bitmap.
                            var arraySize2 = stride2 * _DiffImg.PixelHeight;
                            var pixelArray2 = new byte[arraySize2];

                            float min2 = mCL.GetDiffMin();
                            float max2 = mCL.GetDiffMax();

                            for (int row = 0; row < _DiffImg.PixelHeight; row++)
                                for (int col = 0; col < _DiffImg.PixelWidth; col++)
                                {
                                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                                }


                            Int32Rect rect2 = new Int32Rect(0, 0, _DiffImg.PixelWidth, _DiffImg.PixelHeight);

                            _DiffImg.WritePixels(rect2, pixelArray2, stride2, 0);
                        },j);
                    }
                  }
                
                // Cleanup

                //return 0;
			}, cancellationToken,TaskCreationOptions.LongRunning, TaskScheduler.Default);

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
                        i._ImgBMP = new WriteableBitmap(LockedArea.xPixels, LockedArea.yPixels, 96, 96, PixelFormats.Bgr32, null);
                        i.Image.Source = i._ImgBMP;

                        RenderOptions.SetBitmapScalingMode(i.Image, BitmapScalingMode.NearestNeighbor);

                        // Calculate the number of bytes per pixel (should be 4 for this format). 
                        var bytesPerPixelBF = (i._ImgBMP.Format.BitsPerPixel + 7) / 8;

                        // Stride is bytes per pixel times the number of pixels.
                        // Stride is the byte width of a single rectangle row.
                        var strideBF = i._ImgBMP.PixelWidth * bytesPerPixelBF;

                        // Create a byte array for a the entire size of bitmap.
                        var arraySizeBF = strideBF * i._ImgBMP.PixelHeight;
                        var pixelArrayBF = new byte[arraySizeBF];

                        float minBF = i.Min;
                        float maxBF = i.Max;

                        for (int row = 0; row < i._ImgBMP.PixelHeight; row++)
                            for (int col = 0; col < i._ImgBMP.PixelWidth; col++)
                            {
                                pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 0] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - minBF) / (maxBF - minBF)) * 254.0f));
                                pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 1] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - minBF) / (maxBF - minBF)) * 254.0f));
                                pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 2] = Convert.ToByte(Math.Ceiling(((i.GetClampedPixel(col + row * LockedArea.xPixels) - minBF) / (maxBF - minBF)) * 254.0f));
                                pixelArrayBF[(row * i._ImgBMP.PixelWidth + col) * bytesPerPixelBF + 3] = 0;
                            }


                        Int32Rect rectBF = new Int32Rect(0, 0, i._ImgBMP.PixelWidth, i._ImgBMP.PixelHeight);

                        i._ImgBMP.WritePixels(rectBF, pixelArrayBF, strideBF, 0);
                    }

                    // just select the first tab for convenience
                    LockedDetectors[0].Tab.IsSelected = true;

                    SaveImageButton.IsEnabled = true;
                }
                else if (select_CBED)
                {
                    DiffImage = new float[Resolution * Resolution];
                    TDSImage.CopyTo(DiffImage, 0);

                    _DiffImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                    DiffImageDisplay.Source = _DiffImg;


                    // Calculate the number of bytes per pixel (should be 4 for this format). 
                    var bytesPerPixel2 = (_DiffImg.Format.BitsPerPixel + 7) / 8;

                    // Stride is bytes per pixel times the number of pixels.
                    // Stride is the byte width of a single rectangle row.
                    var stride2 = _DiffImg.PixelWidth * bytesPerPixel2;

                    // Create a byte array for a the entire size of bitmap.
                    var arraySize2 = stride2 * _DiffImg.PixelHeight;
                    var pixelArray2 = new byte[arraySize2];

                    float min2 = mCL.GetDiffMin();
                    float max2 = mCL.GetDiffMax();

                    for (int row = 0; row < _DiffImg.PixelHeight; row++)
                        for (int col = 0; col < _DiffImg.PixelWidth; col++)
                        {
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((TDSImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                        }


                    Int32Rect rect2 = new Int32Rect(0, 0, _DiffImg.PixelWidth, _DiffImg.PixelHeight);

                    _DiffImg.WritePixels(rect2, pixelArray2, stride2, 0);


                    SaveImageButton2.IsEnabled = true;

                }
                else
                {

                    _EWImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                    EWImageDisplay.Source = _EWImg;

                    // When its completed we want to get data to c# for displaying in an image...
                    EWImage = new float[Resolution * Resolution];
                    mCL.GetEWImage(EWImage, Resolution);


                    // Calculate the number of bytes per pixel (should be 4 for this format). 
                    var bytesPerPixel = (_EWImg.Format.BitsPerPixel + 7) / 8;

                    // Stride is bytes per pixel times the number of pixels.
                    // Stride is the byte width of a single rectangle row.
                    var stride = _EWImg.PixelWidth * bytesPerPixel;

                    // Create a byte array for a the entire size of bitmap.
                    var arraySize = stride * _EWImg.PixelHeight;
                    var pixelArray = new byte[arraySize];

                    float min = mCL.GetEWMin();
                    float max = mCL.GetEWMax();

                    for (int row = 0; row < _EWImg.PixelHeight; row++)
                        for (int col = 0; col < _EWImg.PixelWidth; col++)
                        {
                            pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((EWImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                            pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((EWImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                            pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((EWImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                            pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 3] = 0;
                        }


                    Int32Rect rect = new Int32Rect(0, 0, _EWImg.PixelWidth, _EWImg.PixelHeight);

                    _EWImg.WritePixels(rect, pixelArray, stride, 0);

                    EWTab.IsSelected = true;

                    _DiffImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                    DiffImageDisplay.Source = _DiffImg;

                    // When its completed we want to get data to c# for displaying in an image...
                    DiffImage = new float[Resolution * Resolution];
                    mCL.GetDiffImage(DiffImage, Resolution);

                    // Calculate the number of bytes per pixel (should be 4 for this format). 
                    var bytesPerPixel2 = (_DiffImg.Format.BitsPerPixel + 7) / 8;

                    // Stride is bytes per pixel times the number of pixels.
                    // Stride is the byte width of a single rectangle row.
                    var stride2 = _DiffImg.PixelWidth * bytesPerPixel2;

                    // Create a byte array for a the entire size of bitmap.
                    var arraySize2 = stride2 * _DiffImg.PixelHeight;
                    var pixelArray2 = new byte[arraySize2];

                    float min2 = mCL.GetDiffMin();
                    float max2 = mCL.GetDiffMax();

                    for (int row = 0; row < _DiffImg.PixelHeight; row++)
                        for (int col = 0; col < _DiffImg.PixelWidth; col++)
                        {
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                            pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                        }


                    Int32Rect rect2 = new Int32Rect(0, 0, _DiffImg.PixelWidth, _DiffImg.PixelHeight);

                    _DiffImg.WritePixels(rect2, pixelArray2, stride2, 0);

                    SaveImageButton.IsEnabled = true;
                    SaveImageButton2.IsEnabled = true;
                    SimulateImageButton.IsEnabled = true;
                }

                SimulateEWButton.IsEnabled = true;
            });
        
        }

        private void ImagingDf_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingDf.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.df);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.df);
        }

        private void ImagingCs_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingCs.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.spherical);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.spherical);
        }

        private void ImagingA1_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingA1.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astigmag);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.astigmag);
        }

        private void ImagingA1theta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingA1theta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astigang);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.astigang);
        }

        private void ImagingkV_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingkV.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.kilovoltage);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.kilovoltage);

            UpdateMaxMrad();
        }

        private void Imagingbeta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Imagingbeta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.beta);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.beta);
        }

        private void Imagingdelta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Imagingdelta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.delta);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.delta);
        }

        private void ImagingAperture_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.aperturemrad);
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.aperturemrad);
        }

        private void SimTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (TEMRadioButton.IsChecked == true)
            {
                ImagingA2.IsEnabled = true;
                ImagingA2Phi.IsEnabled = true;
                ImagingB2.IsEnabled = true;
                ImagingB2Phi.IsEnabled = true;
                Imagingdelta.IsEnabled = true;
                Imagingbeta.IsEnabled = true;
            }
            else if (STEMRadioButton.IsChecked == true)
            {
                ImagingA2.IsEnabled = false;
                ImagingA2Phi.IsEnabled = false;
                ImagingB2.IsEnabled = false;
                ImagingB2Phi.IsEnabled = false;
                Imagingdelta.IsEnabled = false;
                Imagingbeta.IsEnabled = false;
            }
            else if (CBEDRadioButton.IsChecked == true)
            {
                ImagingA2.IsEnabled = false;
                ImagingA2Phi.IsEnabled = false;
                ImagingB2.IsEnabled = false;
                ImagingB2Phi.IsEnabled = false;
                Imagingdelta.IsEnabled = false;
                Imagingbeta.IsEnabled = false;
            }
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Ideally want to check tab and use information to save either EW or CTEM....

              // File saving dialog
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
            
            saveDialog.Title = "Save Output Image";
            saveDialog.DefaultExt = ".tiff";                     // Default file extension
            saveDialog.Filter = "TIFF Image (.tiff)|*.tiff"; // Filter files by extension

            Nullable<bool> result = saveDialog.ShowDialog();

            if (result == true)
            {
                string filename = saveDialog.FileName;
                using (Tiff output = Tiff.Open(filename, "w"))
                {
                    output.SetField(TiffTag.IMAGEWIDTH, Resolution);
                    output.SetField(TiffTag.IMAGELENGTH, Resolution);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    output.SetField(TiffTag.SAMPLEFORMAT, 3);
                    output.SetField(TiffTag.BITSPERSAMPLE, 32);
                    output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
                    output.SetField(TiffTag.ROWSPERSTRIP, Resolution);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                    for (int i = 0; i < Resolution; ++i)
                    {
                        float[] buf = new float[Resolution];
                        byte[] buf2 = new byte[4 * Resolution];
                        if (EWTab.IsSelected == true)
                        {
                            for (int j = 0; j < Resolution; ++j)
                            {
                                buf[j] = EWImage[j + Resolution * i];
                            }
                        }
                        else if (CTEMTab.IsSelected == true)
                        {
                            for (int j = 0; j < Resolution; ++j)
                            {
                                buf[j] = CTEMImage[j + Resolution * i];
                            }
                        }
                        else
                        {
                            // this checks if the STEM images are open.
                            foreach (DetectorItem d in LockedDetectors)
                            {
                                if (d.Tab.IsSelected == true)
                                {
                                    for (int j = 0; j < Resolution; ++j)
                                    {
                                        buf[j] = d.ImageData[j + Resolution * i];
                                    }
                                }
                            }
                        }

                        Buffer.BlockCopy(buf, 0, buf2, 0, buf2.Length);
                        output.WriteScanline(buf2, i);
                    }
                }
            }

        }

        private void SaveImageButton2_Click(object sender, RoutedEventArgs e)
        {
            // Ideally want to check tab and use information to save either EW or CTEM....

            // File saving dialog
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();

            saveDialog.Title = "Save Output Image";
            saveDialog.DefaultExt = ".tiff";                     // Default file extension
            saveDialog.Filter = "TIFF Image (.tiff)|*.tiff"; // Filter files by extension

            Nullable<bool> result = saveDialog.ShowDialog();

            if (result == true)
            {
                string filename = saveDialog.FileName;
                using (Tiff output = Tiff.Open(filename, "w"))
                {
                    output.SetField(TiffTag.IMAGEWIDTH, Resolution);
                    output.SetField(TiffTag.IMAGELENGTH, Resolution);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    output.SetField(TiffTag.SAMPLEFORMAT, 3);
                    output.SetField(TiffTag.BITSPERSAMPLE, 32);
                    output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
                    output.SetField(TiffTag.ROWSPERSTRIP, Resolution);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                    for (int i = 0; i < Resolution; ++i)
                    {
                        float[] buf = new float[Resolution];
                        byte[] buf2 = new byte[4 * Resolution];
                        if (DiffTab.IsSelected == true)
                        {
                            for (int j = 0; j < Resolution; ++j)
                            {
                                buf[j] = DiffImage[j + Resolution * i];
                            }
                        }

                        Buffer.BlockCopy(buf, 0, buf2, 0, buf2.Length);
                        output.WriteScanline(buf2, i);
                    }
                }
            }

        }

        private void ImagingB2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.b2mag);
        }

        private void ImagingB2Phi_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.b2ang);
        }

        private void ImagingA2Phi_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astig2ang);
        }

        private void ImagingA2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astig2mag);
        }

        private void Button_Click_SimImage(object sender, RoutedEventArgs e)
        {
            mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical,
                                   ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad, ImagingParameters.astig2mag, ImagingParameters.astig2ang, ImagingParameters.b2mag, ImagingParameters.b2ang);

            mCL.SimulateCTEMImage();

            //Update the displays

            _CTEMImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
            CTEMImageDisplay.Source = _CTEMImg;

            // When its completed we want to get data to c# for displaying in an image...
            CTEMImage = new float[Resolution * Resolution];
            mCL.GetCTEMImage(CTEMImage, Resolution);

            // Calculate the number of bytes per pixel (should be 4 for this format). 
            var bytesPerPixel = (_CTEMImg.Format.BitsPerPixel + 7) / 8;

            // Stride is bytes per pixel times the number of pixels.
            // Stride is the byte width of a single rectangle row.
            var stride = _CTEMImg.PixelWidth * bytesPerPixel;

            // Create a byte array for a the entire size of bitmap.
            var arraySize = stride * _CTEMImg.PixelHeight;
            var pixelArray = new byte[arraySize];

            float min = mCL.GetIMMin();
            float max = mCL.GetIMMax();

            for (int row = 0; row < _CTEMImg.PixelHeight; row++)
                for (int col = 0; col < _CTEMImg.PixelWidth; col++)
                {
                    pixelArray[(row * _CTEMImg.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((CTEMImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                    pixelArray[(row * _CTEMImg.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((CTEMImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                    pixelArray[(row * _CTEMImg.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((CTEMImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                    pixelArray[(row * _CTEMImg.PixelWidth + col) * bytesPerPixel + 3] = 0;
                }


            Int32Rect rect = new Int32Rect(0, 0, _CTEMImg.PixelWidth, _CTEMImg.PixelHeight);

            _CTEMImg.WritePixels(rect, pixelArray, stride, 0);


            _DiffImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
            DiffImageDisplay.Source = _DiffImg;

            // When its completed we want to get data to c# for displaying in an image...
            DiffImage = new float[Resolution * Resolution];
            mCL.GetDiffImage(DiffImage, Resolution);

            // Calculate the number of bytes per pixel (should be 4 for this format). 
            var bytesPerPixel2 = (_DiffImg.Format.BitsPerPixel + 7) / 8;

            // Stride is bytes per pixel times the number of pixels.
            // Stride is the byte width of a single rectangle row.
            var stride2 = _DiffImg.PixelWidth * bytesPerPixel;

            // Create a byte array for a the entire size of bitmap.
            var arraySize2 = stride2 * _DiffImg.PixelHeight;
            var pixelArray2 = new byte[arraySize2];

            float min2 = mCL.GetDiffMin();
            float max2 = mCL.GetDiffMax();

            for (int row = 0; row < _DiffImg.PixelHeight; row++)
                for (int col = 0; col < _DiffImg.PixelWidth; col++)
                {
                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                    pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                }


            Int32Rect rect2 = new Int32Rect(0, 0, _DiffImg.PixelWidth, _DiffImg.PixelHeight);

            _DiffImg.WritePixels(rect2, pixelArray2, stride2, 0);

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TDS = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TDS = false;
        }

        private void STEMDet_Click(object sender, RoutedEventArgs e)
        {
            // open the window here
            var window = new STEMDialog(Detectors);
            window.Owner = this;
            window.AddDetectorEvent += new EventHandler<DetectorArgs>(STEM_AddDetector);
            window.RemDetectorEvent += new EventHandler<DetectorArgs>(STEM_RemoveDetector);
            window.ShowDialog();
        }

        private void STEMArea_Click(object sender, RoutedEventArgs e)
        {
            var window = new STEMAreaDialog(STEMRegion);
            window.Owner = this;
            window.AddAreaEvent += new EventHandler<AreaArgs>(STEM_AddArea);
            window.ShowDialog();
        }

        void STEM_AddDetector(object sender, DetectorArgs evargs)
        {
            Detectors.Add(evargs.Detector);
            // draw the tab
            LeftTab.Items.Add(evargs.Detector.Tab);
        }

        void STEM_RemoveDetector(object sender, DetectorArgs evargs)
        {
            foreach (DetectorItem i in evargs.DetectorArr)
            {
                LeftTab.Items.Remove(i.Tab);
                Detectors.Remove(i);
            }
        }

        void STEM_AddArea(object sender, AreaArgs evargs)
        {
            STEMRegion = evargs.AreaParams;
        }

        private void DeviceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var CB = sender as ComboBox;
            //mCL.SetDevice(CB.SelectedIndex);
        }

        private void DeviceSelector_DropDownOpened(object sender, EventArgs e)
        {
            DeviceSelector.ItemsSource = devicesLong;
        }

        private void DeviceSelector_DropDownClosed(object sender, EventArgs e)
        {
            var CB = sender as ComboBox;

            int index = CB.SelectedIndex;
            CB.ItemsSource = devicesShort;
            CB.SelectedIndex = index;
            if (index != -1) // Later, might want to check for index the same as before
            {
                mCL.SetDevice(CB.SelectedIndex);
                CB.IsEnabled = false;
            }
        }


		public void DrawAtoms()
		{
			m_3dChart = new ScatterChart3D();

			int Len = 0;
			float MinX = 0;
			float MinY = 0;
			float MinZ = 0;
			float MaxX = 0;
			float MaxY = 0;
			float MaxZ = 0;

			mCL.GetStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

			WPFChart3D.Model3D model3d = new WPFChart3D.Model3D();

			// Now we want to sorting the atoms ready for the simulation process do this in a background worker...
			this.cancellationTokenSource = new CancellationTokenSource();
			var cancellationToken = this.cancellationTokenSource.Token;

			var task2 = Task.Factory.StartNew(LenAtoms =>
			{
				Thread.CurrentThread.Priority = ThreadPriority.Normal;

				int nDotNo = (int)LenAtoms;
				// 1. set scatter chart data no.

				m_3dChart.SetDataNo(nDotNo); //number of objects/shapes

				float[] xcoord = new float[nDotNo];
				float[] ycoord = new float[nDotNo];
				float[] zcoord = new float[nDotNo];
                int[] aNumber = new int[nDotNo];

				mCL.GetAtomCoords(xcoord, ycoord, zcoord, aNumber, nDotNo);

				// 2. set property of each dot (size, position, shape, color)
				//Random randomObject = new Random();
				//int nDataRange = 1500;
				for (int i = 0; i < nDotNo; i++)
				{
					ScatterPlotItem plotItem = new ScatterPlotItem();

					plotItem.w = 0.3f;
					plotItem.h = 0.3f;

					plotItem.x = xcoord[i];
					plotItem.y = ycoord[i];
					plotItem.z = zcoord[i];

					plotItem.shape = (int)WPFChart3D.Chart3D.SHAPE.ELLIPSE;

					//Byte nR = (Byte)128;
					//Byte nG = (Byte)128;
					//Byte nB = (Byte)128;
                    plotItem.color = atomicColour(aNumber[i]);

					//plotItem.color = Color.FromRgb(nR, nG, nB);
					((ScatterChart3D)m_3dChart).SetVertex(i, plotItem);
				}

				// 3. set the axes
				m_3dChart.GetDataRange();
				m_3dChart.SetAxes();
				
				meshs = new ArrayList(((ScatterChart3D)m_3dChart).GetMeshes());
				
			}, Len, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith((plot) =>
				{
					// 6. display scatter plot in Viewport3D
					m_nChartModelIndex = model3d.UpdateModel(meshs, null, m_nChartModelIndex, this.mainViewport);

					double largestdim = Math.Max(Math.Max(MaxX - MinX, MaxY - MinY), MaxZ - MinZ);
					// 7. set projection matrix
					m_transformMatrix.CalculateProjectionMatrix(MinX, MinX + largestdim, MinY, MinY + largestdim, MinZ, MinZ + largestdim, 0.8);

					TransformChart();
				}, cancellationToken, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

        public void OnViewportMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            Point pt = args.GetPosition(mainViewport);

            if (args.ChangedButton == MouseButton.Left)// rotate or drag 3d model
            {
                m_transformMatrix.OnLBtnDown(pt);
            }


        }

        public void OnViewportMouseMove(object sender, System.Windows.Input.MouseEventArgs args)
        {
            Point pt = args.GetPosition(mainViewport);

            if (args.LeftButton == MouseButtonState.Pressed)                // rotate or drag 3d model
            {
                m_transformMatrix.OnMouseMove(pt, mainViewport);
                TransformChart();
            }
        }

        public void OnViewportMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            Point pt = args.GetPosition(mainViewport);

            if (args.ChangedButton == MouseButton.Left)
            {
                m_transformMatrix.OnLBtnUp();
            }
        }

        private void TransformChart()
        {
            if (m_nChartModelIndex == -1) return;
            ModelVisual3D visual3d = (ModelVisual3D)(this.mainViewport.Children[m_nChartModelIndex]);
            if (visual3d.Content == null) return;
            Transform3DGroup group1 = visual3d.Content.Transform as Transform3DGroup;
            group1.Children.Clear();
            group1.Children.Add(new MatrixTransform3D(m_transformMatrix.m_totalMatrix));
        }

        public Color atomicColour(int aNo)
        {
            switch (aNo)
            {
                case 1: return Color.FromRgb(255,255,255);
                    break;
                case 2: return Color.FromRgb(217,255,255);
                    break;
                case 3: return Color.FromRgb(204,128,255);
                    break;
                case 4: return Color.FromRgb(194,255,0);
                    break;
                case 5: return Color.FromRgb(255, 181, 181);
                    break;
                case 6: return Color.FromRgb(144, 144, 144);
                    break;
                case 7: return Color.FromRgb(48, 80, 248);
                    break;
                case 8: return Color.FromRgb(255, 13, 13);
                    break;
                case 9: return Color.FromRgb(144, 224, 80);
                    break;
                case 10: return Color.FromRgb(179, 227, 245);
                    break;
                case 11: return Color.FromRgb(171, 92, 242);
                    break;
                case 12: return Color.FromRgb(138, 255, 0);
                    break;
                case 13: return Color.FromRgb(191, 166, 166);
                    break;
                case 14: return Color.FromRgb(240, 200, 160);
                    break;
                case 15: return Color.FromRgb(255, 128, 0);
                    break;
                case 16: return Color.FromRgb(255, 255, 48);
                    break;
                case 17: return Color.FromRgb(31, 240, 31);
                    break;
                case 18: return Color.FromRgb(128, 209, 227);
                    break;
                case 19: return Color.FromRgb(143, 64, 212);
                    break;
                case 20: return Color.FromRgb(61, 255, 0);
                    break;
                case 21: return Color.FromRgb(230, 230, 230);
                    break;
                case 22: return Color.FromRgb(191, 194, 199);
                    break;
                case 23: return Color.FromRgb(166, 166, 171);
                    break;
                case 24: return Color.FromRgb(138, 153, 199);
                    break;
                case 25: return Color.FromRgb(156, 122, 199);
                    break;
                case 26: return Color.FromRgb(224, 102, 51);
                    break;
                case 27: return Color.FromRgb(240, 144, 160);
                    break;
                case 28: return Color.FromRgb(80, 208, 80);
                    break;
                case 29: return Color.FromRgb(200, 128, 51);
                    break;
                case 30: return Color.FromRgb(125, 128, 176);
                    break;
                case 31: return Color.FromRgb(194, 143, 143);
                    break;
                case 32: return Color.FromRgb(102, 143, 143);
                    break;
                case 33: return Color.FromRgb(189, 128, 227);
                    break;
                case 34: return Color.FromRgb(255, 161, 0);
                    break;
                case 35: return Color.FromRgb(166, 41, 41);
                    break;
                case 36: return Color.FromRgb(92, 184, 209);
                    break;
                case 37: return Color.FromRgb(112, 46, 176);
                    break;
                case 38: return Color.FromRgb(0, 255, 0);
                    break;
                case 39: return Color.FromRgb(148, 255, 255);
                    break;
                case 40: return Color.FromRgb(148, 224, 224);
                    break;
                case 41: return Color.FromRgb(115, 194, 201);
                    break;
                case 42: return Color.FromRgb(84, 181, 181);
                    break;
                case 43: return Color.FromRgb(59, 158, 158);
                    break;
                case 44: return Color.FromRgb(36, 143, 143);
                    break;
                case 45: return Color.FromRgb(10, 125, 140);
                    break;
                case 46: return Color.FromRgb(0, 105, 133);
                    break;
                case 47: return Color.FromRgb(192, 192, 192);
                    break;
                case 48: return Color.FromRgb(255, 217, 143);
                    break;
                case 49: return Color.FromRgb(166, 117, 115);
                    break;
                case 50: return Color.FromRgb(102, 128, 128);
                    break;
                case 51: return Color.FromRgb(158, 99, 181);
                    break;
                case 52: return Color.FromRgb(212, 122, 0);
                    break;
                case 53: return Color.FromRgb(148, 0, 148);
                    break;
                case 54: return Color.FromRgb(66, 158, 176);
                    break;
                case 55: return Color.FromRgb(87, 23, 143);
                    break;
                case 56: return Color.FromRgb(0, 201, 0);
                    break;
                case 57: return Color.FromRgb(112, 212, 255);
                    break;
                case 58: return Color.FromRgb(255, 255, 199);
                    break;
                case 59: return Color.FromRgb(217, 255, 199);
                    break;
                case 60: return Color.FromRgb(199, 255, 199);
                    break;
                case 61: return Color.FromRgb(163, 255, 199);
                    break;
                case 62: return Color.FromRgb(143, 255, 199);
                    break;
                case 63: return Color.FromRgb(97, 255, 199);
                    break;
                case 64: return Color.FromRgb(69, 255, 199);
                    break;
                case 65: return Color.FromRgb(48, 255, 199);
                    break;
                case 66: return Color.FromRgb(31, 255, 199);
                    break;
                case 67: return Color.FromRgb(0, 255, 156);
                    break;
                case 68: return Color.FromRgb(0, 230, 117);
                    break;
                case 69: return Color.FromRgb(0, 212, 82);
                    break;
                case 70: return Color.FromRgb(0, 191, 56);
                    break;
                case 71: return Color.FromRgb(0, 171, 36);
                    break;
                case 72: return Color.FromRgb(77, 194, 255);
                    break;
                case 73: return Color.FromRgb(77, 166, 255);
                    break;
                case 74: return Color.FromRgb(33, 148, 214);
                    break;
                case 75: return Color.FromRgb(38, 125, 171);
                    break;
                case 76: return Color.FromRgb(38, 102, 150);
                    break;
                case 77: return Color.FromRgb(23, 84, 135);
                    break;
                case 78: return Color.FromRgb(208, 208, 224);
                    break;
                case 79: return Color.FromRgb(255, 209, 35);
                    break;
                case 80: return Color.FromRgb(184, 184, 208);
                    break;
                case 81: return Color.FromRgb(166, 84, 77);
                    break;
                case 82: return Color.FromRgb(87, 89, 97);
                    break;
                case 83: return Color.FromRgb(158, 79, 181);
                    break;
                case 84: return Color.FromRgb(171, 92, 0);
                    break;
                case 85: return Color.FromRgb(117, 79, 69);
                    break;
                case 86: return Color.FromRgb(66, 130, 150);
                    break;
                case 87: return Color.FromRgb(66, 0, 102);
                    break;
                case 88: return Color.FromRgb(0, 125, 0);
                    break;
                case 89: return Color.FromRgb(112, 171, 250);
                    break;
                case 90: return Color.FromRgb(0, 186, 255);
                    break;
                case 91: return Color.FromRgb(0, 161, 255);
                    break;
                case 92: return Color.FromRgb(0, 143, 255);
                    break;
                case 93: return Color.FromRgb(0, 128, 255);
                    break;
                case 94: return Color.FromRgb(0, 107, 255);
                    break;
                case 95: return Color.FromRgb(84, 92, 242);
                    break;
                case 96: return Color.FromRgb(120, 92, 227);
                    break;
                case 97: return Color.FromRgb(138, 79, 227);
                    break;
                case 98: return Color.FromRgb(161, 54, 212);
                    break;
                case 99: return Color.FromRgb(179, 31, 212);
                    break;
                case 100: return Color.FromRgb(179, 31, 186);
                    break;
                case 101: return Color.FromRgb(179, 13, 166);
                    break;
                case 102: return Color.FromRgb(189, 13, 135);
                    break;
                case 103: return Color.FromRgb(199, 0, 102);
                    break;
                default: return Color.FromRgb(255,255,255);
                    break;
            }





        }

    }
}
