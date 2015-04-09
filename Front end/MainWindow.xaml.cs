using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using ManagedOpenCLWrapper;
using BitMiracle.LibTiff.Classic;
using SimulationGUI.Utils;
using System.Linq;
using System.Text.RegularExpressions;
using SimulationGUI.Controls;
using SimulationGUI.Dialogs;
using SimulationGUI.Utils.Settings;

// this stuff might have moved to the simulations file now.
// TODO: convert aberration angles from radians to degrees or other way around? ( /= Convert.ToSingle((180 / Math.PI)) )
// TODO: divide beta by 1000?
// TODO: times delta by 10?

namespace SimulationGUI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Holds names of OpenCL devices, full versions
        /// </summary>
        readonly List<String> _devicesLong;

        /// <summary>
        /// Holds names of OpenCL devices, short versions
        /// </summary>
        readonly List<String> _devicesShort;

        // Default display tabsstem

        /// <summary>
        /// Display tab for the TEM image simulation
        /// </summary>
        readonly DisplayTab _ctemDisplay = new DisplayTab("CTEM");

        /// <summary>
        /// Display tab for the exit wave amplitude image
        /// </summary>
        readonly DisplayTab _ewAmplitudeDisplay = new DisplayTab("EW A");

        /// <summary>
        /// Display tab for the exit wave phase image
        /// </summary>
        readonly DisplayTab _ewPhaseDisplay = new DisplayTab("EW θ");

        /// <summary>
        /// Display tab for the diffraction image
        /// </summary>
        readonly DisplayTab _diffDisplay = new DisplayTab("Diffraction");

        /// <summary>
        /// List storing all detectors (that act as tabs). This is the live version that is modifed by the detector dialog.
        /// </summary>
        List<DetectorItem> DetectorDisplay = new List<DetectorItem>();

        /// <summary>
        /// List storing all detectors (that act as tabs). This is the locked version used to stop users changing settings mid simulation.
        /// </summary>
        List<DetectorItem> _lockedDetectorDisplay = new List<DetectorItem>();

        /// <summary>
        /// Settings stores all the current settings and is updated as dialogs/textboxes are changed.
        /// </summary>
        private SimulationSettings Settings;

        /// <summary>
        /// Settings of the last simulated images, used to keep constant settings mid simulation.
        /// </summary>
        private SimulationSettings _lockedSettings;

        //TODO: see which of these we can do away with
        bool _isResolutionSet = false;
        bool _haveStructure = false;
        bool _detectorVis = false;
        bool _haveMaxMrad = false;

        /// <summary>
        /// Cancel event to halt calculation.
        /// </summary>
        public event EventHandler Cancel = delegate { };

        /// <summary>
        /// Worker to perform calculations in Non UI Thread.
        /// </summary>
        readonly ManagedOpenCL _mCl;

        /// <summary>
        /// TaskFactory stuff
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Class constructor. Makes GUI and sets up the settings.
        /// </summary>
        public MainWindow()
        {
            // has to be created before gui is created to avoid errors
            Settings = new SimulationSettings();

            // Initialise GPU
            InitializeComponent();

            // This was to supress some warnings, might not be needed
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;

            //add event handlers here so they aren't called when creating controls
            txtCBEDx.TextChanged += CheckTboxValid;
            txtCBEDy.TextChanged += CheckTboxValid;
            txt3DIntegrals.TextChanged += CheckTboxValid;
            txtSliceThickness.TextChanged += CheckTboxValid;
            txtMicroscopeKv.TextChanged += CheckTboxValid;
            txtMicroscopeAp.TextChanged += CheckTboxValid;
            txtSTEMmulti.TextChanged += CheckTboxValid;
            txtSTEMruns.TextChanged += CheckTboxValid;
            txtCBEDruns.TextChanged += CheckTboxValid;
            txtDose.TextChanged += CheckTboxValid;

			// add constant tabs to UI
			LeftTab.Items.Add(_ctemDisplay.Tab);
			LeftTab.Items.Add(_ewAmplitudeDisplay.Tab);
            LeftTab.Items.Add(_ewPhaseDisplay.Tab);
			RightTab.Items.Add(_diffDisplay.Tab);

            _ctemDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            _ewAmplitudeDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            _ewPhaseDisplay.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            _diffDisplay.SetPositionReadoutElements(ref RightXCoord, ref RightYCoord);
            _diffDisplay.Reciprocal = true;

			// Start in TEM mode.
            TEMRadioButton.IsChecked = true;

            // Setup Managed Wrapper and Upload Atom Parameterisation ready for Multislice calculations.
            // Moved parameterisation, will be redone each time we get new structure now :(
            _mCl = new ManagedOpenCL();

            // Set default OpenCL device to blank
            // Must be set twice for it to work
            DeviceSelector.SelectedIndex = -1;
            DeviceSelector.SelectedIndex = -1;

            // Link settings class to dialogs
            Settings.UpdateWindow(this);

            // Set Default Binning and CCD selected indices
			BinningCombo.SelectedIndex = 0;
			CCDCombo.SelectedIndex = 0;

            // Populate OpenCL device combo box
            // Create two lists to contain full information and short information
            _devicesShort = new List<String>();
            _devicesLong = new List<String>();

            var numDev = _mCl.getCLdevCount();

            for (var i = 0; i < numDev; i++)
            {
                _devicesShort.Add(_mCl.getCLdevString(i, true));
                _devicesLong.Add(_mCl.getCLdevString(i, false));
            }

            // Show short by default, Only want long when menu is dropped down
            DeviceSelector.ItemsSource = _devicesShort;
        }

        /// <summary>
        /// Import structure button clicked.
        /// Opens .xyz file in unmanaged code.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportXYZ(object sender, RoutedEventArgs e)
        {
            // Create open dialog
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "file name",
                DefaultExt = ".xyz",
                Filter = "XYZ Coordinates (.xyz)|*.xyz"
            };

            // Show it
            var result = openDialog.ShowDialog();

            // If we got a file
            if (result == true)
            {
                // Get name and show it
                var fName = openDialog.FileName;
                lblFileName.Text = System.IO.Path.GetFileName(fName);
                lblFileName.ToolTip = fName;

                Settings.FileName = fName;

                // Pass filename through to unmanaged where atoms can be imported inside structure class
                _mCl.importStructure(openDialog.FileName);
                _mCl.uploadParameterisation();

                // Update some dialogs if everything went OK.
                var Len = 0;
                float MinX = 0;
                float MinY = 0;
                float MinZ = 0;
                float MaxX = 0;
                float MaxY = 0;
                float MaxZ = 0;

                // Get structure details from unmanaged
                _mCl.getStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

                _haveStructure = true;

                // Update the displays
                WidthLabel.Content = (MaxX - MinX).ToString("f2") + " Å";
                HeightLabel.Content = (MaxY - MinY).ToString("f2") + " Å";
                DepthLabel.Content = (MaxZ - MinZ).ToString("f2") + " Å";
                AtomNoLabel.Content = Len.ToString();

                // Change area parameters only if they aren't user set
                if (!Settings.STEM.UserSetArea)
                {
                    Settings.STEM.ScanArea.EndX = Convert.ToSingle((MaxX - MinX).ToString("f2"));
                    Settings.STEM.ScanArea.EndY = Convert.ToSingle((MaxY - MinY).ToString("f2"));
                }

                if (!Settings.UserSetArea)
                {
                    Settings.SimArea.EndX = Convert.ToSingle((MaxX - MinX).ToString("f2"));
                    Settings.SimArea.EndY = Convert.ToSingle((MaxY - MinY).ToString("f2"));
                }

                // Try and update the pixelscale
                UpdatePixelScale();

                // Now we want to sorting the atoms ready for the simulation process do this in a background worker...
                cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = this.cancellationTokenSource.Token;
                var progressReporter = new ProgressReporter();
                var task = Task.Factory.StartNew(() =>
                {
                    // This is where we start sorting the atoms in the background ready to be processed later...
                    _mCl.sortStructure(false);
                    return 0;
                },cancellationToken);

                ErrorMessage.ToggleCode(0, true);
            }
        }

        /// <summary>
        /// Placeholder to eventually import cif files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportUnitCell(object sender, RoutedEventArgs e)
        {
            // TODO: implement cif file inputs.
            // would need to calculate the unit cell coordinates and then tile them to some user set range?
        }

        private void UpdateEWImages()
        {
            // Update amplitude
            _mCl.getEWImage(_ewAmplitudeDisplay.ImageData, _lockedSettings.Resolution);
            _ewAmplitudeDisplay.Max = _mCl.getEWMax();
            _ewAmplitudeDisplay.Min = _mCl.getEWMin();
            UpdateTabImage(_ewAmplitudeDisplay, x => x);

            // Update phase
            _mCl.getEWImage2(_ewPhaseDisplay.ImageData, _lockedSettings.Resolution);
            _ewPhaseDisplay.Max = _mCl.getEWMax2();
            _ewPhaseDisplay.Min = _mCl.getEWMin2();
            UpdateTabImage(_ewPhaseDisplay, x => x);
        }

        /// <summary>
        /// Updates the CTEM image tab
        /// </summary>
        /// <param name="dpp">Dose per pixel value (units ???)</param>
        /// <param name="binning">Binning number</param>
        /// <param name="CCD">Integer value denoting CCD selection in dropdown box (0 == perfect)</param>
        private void UpdateCTEMImage(float dpp, int binning, int CCD)
        {
            if (CCD != 0)
                _mCl.getCTEMImage(_ctemDisplay.ImageData, _lockedSettings.Resolution, dpp, binning, CCD);
            else
                _mCl.getCTEMImage(_ctemDisplay.ImageData, _lockedSettings.Resolution);
            _ctemDisplay.Max = _mCl.getCTEMMax();
            _ctemDisplay.Min = _mCl.getCTEMMin();
            UpdateTabImage(_ctemDisplay, x => x);
        }

        private void UpdateDiffractionImage()
        {
            _mCl.getDiffImage(_diffDisplay.ImageData, _lockedSettings.Resolution);
            _diffDisplay.Max = _mCl.getDiffMax();
            _diffDisplay.Min = _mCl.getDiffMin();
            UpdateTabImage(_diffDisplay, x => Convert.ToSingle(Math.Log(Convert.ToDouble(x + 1.0f))));
        }

        private static void UpdateSTEMImage(DetectorItem det)
        {
            // Limits and data are updated throughout simulation so should be good to go here
            UpdateTabImage(det, x => x);
        }

        /// <summary>
        /// Updates the image inside the display tabs
        /// </summary>
        /// <param name="imageTab">Tab to be updates</param>
        /// <param name="scale">Function used to apply scaling (i.e. logarithmic).</param>
        private static void UpdateTabImage(DisplayTab imageTab, Func<float, float> scale)
        {

            var min = scale(imageTab.Min);
            var max = scale(imageTab.Max);

            if (min == max) // Possible precision errors?
                return;

            var xDim = imageTab.xDim;
            var yDim = imageTab.yDim;

            imageTab.ImgBmp = new WriteableBitmap(xDim, yDim, 96, 96, PixelFormats.Bgr32, null);
            imageTab.tImage.Source = imageTab.ImgBmp;

            var bytesPerPixel = (imageTab.ImgBmp.Format.BitsPerPixel + 7) / 8;

            var stride = imageTab.ImgBmp.PixelWidth*bytesPerPixel;

            var arraySize = stride*yDim;
            var pixelArray = new byte[arraySize];

            for (var row = 0; row < yDim; row++)
                for (var col = 0; col < xDim; col++)
                {
                    pixelArray[(row * xDim + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling((( scale(imageTab.ImageData[col + row * xDim]) - min) / (max - min)) * 254.0f));
                    pixelArray[(row * xDim + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling((( scale(imageTab.ImageData[col + row * xDim]) - min) / (max - min)) * 254.0f));
                    pixelArray[(row * xDim + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling((( scale(imageTab.ImageData[col + row * xDim]) - min) / (max - min)) * 254.0f));
                    pixelArray[(row * xDim + col) * bytesPerPixel + 3] = 0;
                }

            var rect = new Int32Rect(0, 0, xDim, yDim);

            imageTab.ImgBmp.WritePixels(rect, pixelArray, stride, 0);
        }

        /// <summary>
        /// Overload function to update the progress bar for 'single pixel' simulations (i.e. TEM)
        /// </summary>
        /// <param name="numberOfSlices">Number of slices in simulation</param>
        /// <param name="numberOfRuns">Number of TDS runs</param>
        /// <param name="currentSlice">The current slice</param>
        /// <param name="currentRun">The current TDS run</param>
        /// <param name="simTime">Run time on the OpenCL device in ms</param>
        /// <param name="memUsage">Memory usage of the OpenCL device</param>
        private void UpdateStatus(int numberOfSlices, int numberOfRuns, int currentSlice, int currentRun, float simTime, int memUsage)
        {
            UpdateStatus(numberOfSlices, numberOfRuns, 1, currentSlice, currentRun, 1, simTime, memUsage);
        }

        /// <summary>
        /// Function to update the progress bar for simulations (i.e. TEM)
        /// </summary>
        /// <param name="numberOfSlices">Number of slices in simulation</param>
        /// <param name="numberOfRuns">Number of TDS runs</param>
        /// <param name="numberOfPixels">Number of pixels in STEM image</param>
        /// <param name="currentSlice">The current slice</param>
        /// <param name="currentRun">The current TDS run</param>
        /// <param name="currentPixel">The current pixel being simulated</param>
        /// <param name="simTime">Run time on the OpenCL device in ms</param>
        /// <param name="memUsage">Memory usage of the OpenCL device</param>
		private void UpdateStatus(int numberOfSlices, int numberOfRuns, int numberOfPixels, int currentSlice, int currentRun, int currentPixel, float simTime, int memUsage)
		{
			pbrSlices.Value = Convert.ToInt32(100 * Convert.ToSingle(currentSlice) / Convert.ToSingle(numberOfSlices));
			pbrTotal.Value = Convert.ToInt32(100 * Convert.ToSingle(currentRun*numberOfPixels + currentPixel) / Convert.ToSingle(numberOfRuns*numberOfPixels));
			TimerMessage.Text = simTime + " ms";
			MemUsageLabel.Text = memUsage / (1024 * 1024) + " MB"; // TODO: check why 1024*1024?
		}

        //TODO: can combine these by getting the sender too?
		private void SaveLeftImage(object sender, RoutedEventArgs e)
		{
            // get display tab that is selected
            var tabs = new List<DisplayTab> { _ctemDisplay, _ewAmplitudeDisplay, _ewPhaseDisplay };
            // for left tab only
            tabs.AddRange(_lockedDetectorDisplay);

            foreach (var window in from dt in tabs where dt.Tab.IsSelected where dt.xDim != 0 || dt.yDim != 0 select new InfoSaveDialog(dt) { Owner = this })
            {
                window.ShowDialog();
            }
		}

        private void SaveRightImage(object sender, RoutedEventArgs e)
        {
			// Ideally want to check tab and use information to save either EW or CTEM....
			var tabs = new List<DisplayTab> {_diffDisplay};

            foreach (var window in from dt in tabs where dt.Tab.IsSelected where dt.xDim != 0 || dt.yDim != 0 select new InfoSaveDialog(dt) { Owner = this })
            {
                window.ShowDialog();
            }
        }

        /// <summary>
        /// Check CBED positions are in simulation range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckTboxValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;

            var text = tbox.Text;
            // have replaced validCBED and goofinite

            float fVal;
            int iVal;
            bool good;

            if (Equals(tbox, txtCBEDx))
            {
                float.TryParse(text, out fVal);
                good = fVal >= Settings.SimArea.StartX && fVal <= Settings.SimArea.EndX;
                ErrorMessage.ToggleCode(30, good);
            }
            else if (Equals(tbox, txtCBEDy))
            {
                float.TryParse(text, out fVal);
                good = fVal >= Settings.SimArea.StartY && fVal <= Settings.SimArea.EndY;
                ErrorMessage.ToggleCode(30, good);
            }
            else if (Equals(tbox, txtCBEDruns))
            {
                int.TryParse(text, out iVal);
                good = iVal > 0;
                ErrorMessage.ToggleCode(31, good);
            }
            else if (Equals(tbox, txt3DIntegrals))
            {
                int.TryParse(text, out iVal);
                good = iVal > 0;
                ErrorMessage.ToggleCode(6, good);
            }
            else if (Equals(tbox, txtSliceThickness))
            {
                float.TryParse(text, out fVal);
                good = fVal > 0;
                ErrorMessage.ToggleCode(5, good);
            }
            else if (Equals(tbox, txtMicroscopeAp))
            {
                float.TryParse(text, out fVal);
                good = fVal > 0;
                WarningMessage.ToggleCode(10, good);
            }
            else if (Equals(tbox, txtMicroscopeKv))
            {
                float.TryParse(text, out fVal);
                good = fVal > 0;
                ErrorMessage.ToggleCode(3, good);
            }
            else if (Equals(tbox, txtSTEMruns))
            {
                int.TryParse(tbox.Text, out iVal);
                good = iVal > 0;
                ErrorMessage.ToggleCode(41, good);
            }
            else if (Equals(tbox, txtSTEMmulti))
            {
                int.TryParse(tbox.Text, out iVal);
                good = iVal > 0;
                ErrorMessage.ToggleCode(41, good);
            }
            else if (Equals(tbox, txtDose))
            {
                int.TryParse(tbox.Text, out iVal);
                good = iVal > 0;
                WarningMessage.ToggleCode(50, good);
            }
            else
                return;

            if (!good)
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
        }

        private static void UpdateWorkingColour(bool working)
        {
            if (working)
                Application.Current.Resources["Accent"] = Application.Current.Resources["WorkingColOrig"];
            else
                Application.Current.Resources["Accent"] = Application.Current.Resources["AccentOrig"];
        }

        /// <summary>
        /// Updates the pixel scale and also the maximum milliradians
        /// </summary>
        private void UpdatePixelScale()
        {
            if (!_haveStructure || !_isResolutionSet) return;
            var biggestSize = Math.Max(Settings.SimArea.EndX - Settings.SimArea.StartX, Settings.SimArea.EndY - Settings.SimArea.StartY);
            Settings.PixelScale = biggestSize / Settings.Resolution;
            PixelScaleLabel.Content = Settings.PixelScale.ToString("f2") + " Å";

            UpdateMaxMrad();
        }

        /// <summary>
        /// Updates maximum milliradians visible in reciprocal space
        /// </summary>
        private void UpdateMaxMrad()
        {
            if (!_haveStructure)
                return;

            var minX = Settings.SimArea.StartX;
            var minY = Settings.SimArea.StartY;

            var maxX = Settings.SimArea.EndX;
            var maxY = Settings.SimArea.EndY;

            var biggestSize = Math.Max(maxX - minX, maxY - minY);
            // Determine max mrads for reciprocal space, (need wavelength)...
            var maxFreq = 1 / (2 * biggestSize / Settings.Resolution);

            if (Settings.Microscope.Voltage.Val <= 0 && _isResolutionSet)
            {
                const float echarge = 1.6e-19f;
                Settings.Wavelength = Convert.ToSingle(6.63e-034 * 3e+008 / Math.Sqrt((echarge * Settings.Microscope.Voltage.Val * 1000 *
                    (2 * 9.11e-031 * 9e+016 + echarge * Settings.Microscope.Voltage.Val * 1000))) * 1e+010);

                var mrads = (1000 * maxFreq * Settings.Wavelength) / 2; //divide by two to get mask limits

                MaxMradsLabel.Content = mrads.ToString("f2") + " mrad";

                _haveMaxMrad = true;
            }
        }

        private void txtImageKv_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateMaxMrad();
        }

        private void SimTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (TEMRadioButton.IsChecked == true)
            {
                txtMicroscopeD.IsEnabled = true;
                txtMicroscopeB.IsEnabled = true;

                TEMbox.Visibility = Visibility.Visible;
                STEMbox.Visibility = Visibility.Hidden;
                CBEDbox.Visibility = Visibility.Hidden;

                Settings.SimMode = 0;

            }
            else if (STEMRadioButton.IsChecked == true)
            {
                txtMicroscopeD.IsEnabled = false;
                txtMicroscopeB.IsEnabled = false;

                STEMbox.Visibility = Visibility.Visible;
                TEMbox.Visibility = Visibility.Hidden;
                CBEDbox.Visibility = Visibility.Hidden;

                Settings.SimMode = 2;
            }
            else if (CBEDRadioButton.IsChecked == true)
            {
                txtMicroscopeD.IsEnabled = false;
                txtMicroscopeB.IsEnabled = false;

                STEMbox.Visibility = Visibility.Hidden;
                TEMbox.Visibility = Visibility.Hidden;
                CBEDbox.Visibility = Visibility.Visible;

                Settings.SimMode = 1;
            }
        }

        private void STEM_TDStoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) Settings.STEM.DoTDS = chk.IsChecked == true;
        }

        private void CBED_TDStoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) Settings.CBED.DoTDS = chk.IsChecked == true;
        }

        private void Full3Dtoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) Settings.IsFull3D = chk.IsChecked == true;

            if (ToggleFD != null && Settings.IsFull3D)
            {
                ToggleFD.IsChecked = false;
                Settings.IsFiniteDiff = false;
            }
        }

        private void FDtoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) Settings.IsFiniteDiff = chk.IsChecked == true;

            if (ToggleFull3D != null && Settings.IsFiniteDiff)
            {
                ToggleFull3D.IsChecked = false;
                Settings.IsFull3D = false;
            }
        }

        private void Show_detectors(object sender, RoutedEventArgs e)
        {
            foreach (var i in DetectorDisplay)
            {
                i.SetVisibility(true);
            }
            _detectorVis = true;
        }

        private void Hide_Detectors(object sender, RoutedEventArgs e)
        {
            foreach (var i in DetectorDisplay)
            {
                i.SetVisibility(false);
            }
            _detectorVis = false;
        }

        private void OpenSTEMDetDlg(object sender, RoutedEventArgs e)
        {
            // open the window here
            var window = new STEMDetectorDialog(DetectorDisplay) { Owner = this };
            window.AddDetectorEvent += STEM_AddDetector;
            window.RemDetectorEvent += STEM_RemoveDetector;
            window.ShowDialog();
        }

        private void OpenSTEMAreaDlg(object sender, RoutedEventArgs e)
        {
            var window = new STEMAreaDialog(Settings.STEM.ScanArea, Settings.SimArea) { Owner = this };
            window.AddSTEMAreaEvent += STEM_AddArea;
            window.ShowDialog();
        }

        void STEM_AddDetector(object sender, DetectorArgs evargs)
        {
            var added = evargs.Detector;
            LeftTab.Items.Add(added.Tab);
            added.AddToCanvas(_diffDisplay.tCanvas);
            //if(HaveMaxMrad)
            //   added.SetEllipse(Settings.Resolution, _lockedSettings.PixelScale, _lockedSettings.Wavelength, DetectorVis);
        }

        void STEM_RemoveDetector(object sender, DetectorArgs evargs)
        {
            foreach (var i in evargs.DetectorList)
            {
                i.RemoveFromCanvas(_diffDisplay.tCanvas);
                LeftTab.Items.Remove(i.Tab);
            }

            foreach (var i in DetectorDisplay)
            {
                i.SetColour();
            }
        }

        void STEM_AddArea(object sender, StemAreaArgs evargs)
        {
            Settings.STEM.UserSetArea = true;
            Settings.STEM.ScanArea = evargs.AreaParams;
        }

        private void DeviceSelector_DropDownOpened(object sender, EventArgs e)
        {
            DeviceSelector.ItemsSource = _devicesLong;
        }

        private void DeviceSelector_DropDownClosed(object sender, EventArgs e)
        {
            var cb = sender as ComboBox;

            if (cb == null) return;
            var index = cb.SelectedIndex;
            cb.ItemsSource = _devicesShort;
            cb.SelectedIndex = index;
            if (index != -1)
            {
                _mCl.setCLdev(cb.SelectedIndex);
                SimulateImageButton.IsEnabled = false;
            }
            ErrorMessage.ToggleCode(1, index != -1);
        }

        private void OpenAreaDlg(object sender, RoutedEventArgs e)
        {
            var window = new SimAreaDialog(Settings.SimArea) { Owner = this };
            window.SetAreaEvent += SetArea;
            window.Show();
        }

        void SetArea(object sender, SimAreaArgs evargs)
        {
            var changedx = false;
            var changedy = false;
            Settings.UserSetArea = true;
            Settings.SimArea = evargs.AreaParams;

            var xscale = (Settings.STEM.ScanArea.StartX - Settings.STEM.ScanArea.EndX) / Settings.STEM.ScanArea.xPixels;
            var yscale = (Settings.STEM.ScanArea.StartY - Settings.STEM.ScanArea.EndY) / Settings.STEM.ScanArea.yPixels;

            if (Settings.STEM.ScanArea.StartX < Settings.SimArea.StartX || Settings.STEM.ScanArea.StartX > Settings.SimArea.EndX)
            {
                Settings.STEM.ScanArea.StartX = Settings.SimArea.StartX;
                changedx = true;
            }

            if (Settings.STEM.ScanArea.EndX > Settings.SimArea.EndX || Settings.STEM.ScanArea.EndX < Settings.SimArea.StartX)
            {
                Settings.STEM.ScanArea.EndX = Settings.SimArea.EndX;
                changedx = true;
            }

            if (Settings.STEM.ScanArea.StartY < Settings.SimArea.StartY || Settings.STEM.ScanArea.StartY > Settings.SimArea.EndY)
            {
                Settings.STEM.ScanArea.StartY = Settings.SimArea.StartY;
                changedy = true;
            }

            if (Settings.STEM.ScanArea.EndY > Settings.SimArea.EndY || Settings.STEM.ScanArea.EndY < Settings.SimArea.StartY)
            {
                Settings.STEM.ScanArea.EndY = Settings.SimArea.EndY;
                changedy = true;
            }

            if (changedx)
                Settings.STEM.ScanArea.xPixels = (int)Math.Ceiling((Settings.STEM.ScanArea.StartX - Settings.STEM.ScanArea.EndX) / xscale);

            if (changedy)
                Settings.STEM.ScanArea.yPixels = (int)Math.Ceiling((Settings.STEM.ScanArea.StartY - Settings.STEM.ScanArea.EndY) / yscale);

            UpdatePixelScale();
        }

        /// <summary>
        /// Test if the required parameters have been set.
        /// 1. Structure has been set.
        /// 2. Resolution has been set.
        /// 3. OpenCL device has been set.
        /// 4. Microscope parameters make sense.
        /// </summary>
        /// <returns>bool if all the parameters have been set.</returns>
        private bool TestSimulationPrerequisites()
        {
            var errorMsg = new List<string>();
            var warnMsg = new List<string>();

            // At the moment easiest to check this here
            if (DetectorDisplay.Count == 0)
                ErrorMessage.AddCode(42);
            else
                ErrorMessage.RemoveCode(42);

            if (Settings.SimMode == 0)
            {
                errorMsg = ErrorMessage.GetCTEMCodes();
                warnMsg = WarningMessage.GetCTEMCodes();
            }
            else if (Settings.SimMode == 1)
            {
                errorMsg = ErrorMessage.GetCBEDCodes();
                warnMsg = WarningMessage.GetCBEDCodes();
            }
            else if (Settings.SimMode == 2)
            {
                errorMsg = ErrorMessage.GetSTEMCodes();
                warnMsg = WarningMessage.GetSTEMCodes();
            }

            if (errorMsg.Count == 0 && warnMsg.Count == 0)
                return true;

            var message = errorMsg.Aggregate("", (current, msg) => current + ("Error: " + msg + "\n"));
            message = warnMsg.Aggregate(message, (current, msg) => current + ("Warning: " + msg + "\n"));

            if (errorMsg.Count == 0)
            {
                var dlg = new WarningDialog(message, MessageBoxButton.OKCancel, WarningColour.Warning) { Owner = GetWindow(this) };
                var ok = dlg.ShowDialog();
                return ok ?? true;
            }
            else
            {
                var dlg = new WarningDialog(message, MessageBoxButton.OK, WarningColour.Error) { Owner = GetWindow(this) };
                dlg.ShowDialog();
                return false;
            }
        }

        private bool TestImagePrerequisites()
        {
            var errorMsg = ErrorMessage.GetImageCodes();
            var warnMsg = WarningMessage.GetImageCodes();

            if (errorMsg.Count == 0 && warnMsg.Count == 0)
                return true;

            var message = errorMsg.Aggregate("", (current, msg) => current + ("Error: " + msg + "\n"));

            message = warnMsg.Aggregate(message, (current, msg) => current + ("Warning: " + msg + "\n"));

            if (errorMsg.Count == 0)
            {
                var dlg = new WarningDialog(message, MessageBoxButton.OKCancel, WarningColour.Warning) { Owner = GetWindow(this) };
                var ok = dlg.ShowDialog();
                return ok ?? true;
            }
            else
            {
                var dlg = new WarningDialog(message, MessageBoxButton.OK, WarningColour.Error) { Owner = GetWindow(this) };
                dlg.ShowDialog();
                return false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        private void CboResolutionChanged(object sender, SelectionChangedEventArgs e)
        {
            int.TryParse(ResolutionCombo.SelectedValue.ToString(), out Settings.Resolution);

            ErrorMessage.ToggleCode(2, true);

            _isResolutionSet = true;

            if (!Settings.STEM.UserSetArea)
            {
                Settings.STEM.ScanArea.xPixels = Settings.Resolution;
                Settings.STEM.ScanArea.yPixels = Settings.Resolution;
            }

            UpdatePixelScale();
        }

        private void ShowAberrationDialog(object sender, RoutedEventArgs e)
        {
            var window = new AberrationsDialog(Settings) { Owner = this };
            window.ShowDialog();
        }
    }
}
