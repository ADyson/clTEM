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
        float wavelength;

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
                wavelength = Convert.ToSingle(6.63e-034 * 3e+008 / Math.Sqrt((echarge * ImagingParameters.kilovoltage * 1000 * 
                    (2 * 9.11e-031 * 9e+016 + echarge * ImagingParameters.kilovoltage * 1000))) * 1e+010);

                float mrads = (1000 * MaxFreq * wavelength) / 2;

                MaxMradsLabel.Content = mrads.ToString("f2")+" mrad";
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
            Imagingbeta.Text = "5";
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

                if (IsResolutionSet)
                {
                    float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
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

                float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
                pixelScale = BiggestSize / Resolution;
                PixelScaleLabel.Content = pixelScale.ToString("f2") + " Å";

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
                            this.TimerMessage.Content = timer.ElapsedMilliseconds.ToString() + " ms";
                            this.MemUsageLabel.Content = mem / (1024 * 1024) + " MB";
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
                        return 0;
                    }

                    progressReporter.ReportProgress((val) =>
                    {
                        diffCanvas.Children.Clear();

                        diffCanvas.Width = Resolution;
                        diffCanvas.Height = Resolution;

                        // enable checkbox here if it is implemented?
                        // will also possibly change initial visibility of ellipses

                        ColourGenerator.ColourGenerator cgen = new ColourGenerator.ColourGenerator();
                        var converter = new System.Windows.Media.BrushConverter();

                        foreach (DetectorItem i in LockedDetectors)
                        {
                            // calculate the radii and reset properties
                            i.setEllipse(Resolution, pixelScale, wavelength);

                            // add to canvas
                            i.AddToCanvas(diffCanvas);
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
                                        this.TimerMessage.Content = timer.ElapsedMilliseconds.ToString() + " ms";
                                        this.MemUsageLabel.Content = mem / (1024 * 1024) + " MB";
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
                                this.TimerMessage.Content = timer.ElapsedMilliseconds.ToString() + " ms";
                                this.MemUsageLabel.Content = mem / (1024 * 1024) + " MB";
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

                return 0;
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
                        {
                            return; // throw an error maybe
                        }

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
			bool ok = false;

			ok = float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astigang);
            ok |= float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.astigang);

			if (ok)
			{
				ImagingParameters.astigang *= Convert.ToSingle((180 / Math.PI));
				ProbeParameters.astigang *= Convert.ToSingle((180 / Math.PI));
			}

			
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
			bool ok = false;
            
			ok = float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.beta);
            ok |= float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.beta);
			
			if (ok)
			{
				ImagingParameters.beta /= 1000;
				ProbeParameters.beta /= 1000;
			}
        }

        private void Imagingdelta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Imagingdelta.Text;
			bool ok = false;

            ok = float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.delta);
            ok |= float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.delta);

			if (ok)
			{
				ImagingParameters.delta *= 10;
				ProbeParameters.delta *= 10;
			}
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
			bool ok = false;

			ok = float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.b2ang);
			ok |= float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.b2ang);

			if (ok)
			{
				ImagingParameters.b2ang *= Convert.ToSingle((180 / Math.PI));
				ProbeParameters.b2ang *= Convert.ToSingle((180 / Math.PI));
			}
        }

        private void ImagingA2Phi_TextChanged(object sender, TextChangedEventArgs e)
        {

            string temporarytext = ImagingAperture.Text;
			bool ok = false;

			ok = float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astig2ang);
			ok |= float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.astig2ang);

			if (ok)
			{
				ImagingParameters.astig2ang *= Convert.ToSingle((180 / Math.PI));
				ProbeParameters.astig2ang *= Convert.ToSingle((180 / Math.PI));
			}
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
            LeftTab.Items.Add(evargs.Detector.Tab);
        }

        void STEM_RemoveDetector(object sender, DetectorArgs evargs)
        {
            foreach (DetectorItem i in evargs.DetectorList)
            {
                i.RemoveFromCanvas(diffCanvas);
                LeftTab.Items.Remove(i.Tab);
            }

            foreach (DetectorItem i in Detectors)
            {
                i.setEllipse(Resolution, pixelScale, wavelength);
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
    }
}
