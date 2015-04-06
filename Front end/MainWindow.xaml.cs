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
using Framework.UI.Controls;
using SimulationGUI.Utils;

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
        // OpenCL device name storage

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

        List<DetectorItem> DetectorDisplay = new List<DetectorItem>();

        List<DetectorItem> _lockedDetectorDisplay = new List<DetectorItem>();


        private SimulationSettings Settings;

        private SimulationSettings _lockedSettings;

        //private SimulationSettings _imageSettings;

        bool IsResolutionSet = false;
        bool HaveStructure = false;
        bool DetectorVis = false;
        bool HaveMaxMrad = false;

        /// <summary>
        /// Cancel event to halt calculation.
        /// </summary>
        public event EventHandler Cancel = delegate { };

        /// <summary>
        /// Worker to perform calculations in Non UI Thread.
        /// </summary>
        readonly ManagedOpenCL _mCl;

        // TaskFactory stuff
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Class constructor.
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
            // Create Dialog
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

                HaveStructure = true;

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
        }

        /// <summary>
        /// Simulate exit wave.
        /// Checks/gets parameters and performs the simulation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulateExitWave(object sender, RoutedEventArgs e)
        {
			if (!TestSimulationPrerequisites())
				return;

            // Update GUI to 'working' colour
            UpdateWorkingColour(true);

            SimulateEWButton.IsEnabled = false;
            SimulateImageButton.IsEnabled = false;

            _lockedSettings = new SimulationSettings(Settings, CopyType.All);
            _lockedDetectorDisplay = DetectorDisplay;

            // Update the display tab sizes so we don't need to worry about this later
            _ewAmplitudeDisplay.SetSize(_lockedSettings.Resolution);
            _ewPhaseDisplay.SetSize(_lockedSettings.Resolution);
            _ctemDisplay.SetSize(_lockedSettings.Resolution);
            _diffDisplay.SetSize(_lockedSettings.Resolution);

            foreach(var det in _lockedDetectorDisplay)
                det.SetSize(_lockedSettings.STEM.ScanArea.xPixels, _lockedSettings.STEM.ScanArea.yPixels);

            // Create new instances to use to cancel the simulation and to run tasks.
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            var progressReporter = new ProgressReporter();

            // Set the simulation parameters
			CancelButton.IsEnabled = false;
            var task = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                var timer = new Stopwatch();

                // Do cimulation part
                DoSimulationMethod(ref progressReporter, ref timer, ref cancellationToken);

            }, cancellationToken);

            // Update all the images and return application to original state
            // This runs on UI thread so can access UI, probably better way of doing image updates though
            progressReporter.RegisterContinuation(task, () =>
            {
				CancelButton.IsEnabled = false;
                pbrSlices.Value = 100;
                pbrTotal.Value = 100;

                if (_lockedSettings.SimMode == 2)
                {
                    //_lockedSettings.SimMode = 2;
                    if (_lockedDetectorDisplay.Count == 0)
                    {
                        SimulateEWButton.IsEnabled = true;
                        return;
                    }

                    foreach (var det in _lockedDetectorDisplay)
                    {
                        UpdateSTEMImage(det);
                        // copy simulation settings to tab
                        // don't copy stem settings as they are set elsewhere?
                        det.SimParams = new SimulationSettings(_lockedSettings, CopyType.Base);
                    }

                    // Just select the first tab for convenience
                    //_lockedDetectorDisplay.Tab.IsSelected = true;
                    SaveImageButton.IsEnabled = true;
                }
                else if (_lockedSettings.SimMode == 1)
                {
                    //Settings.SimMode = 1;
                    UpdateDiffractionImage();

                    // copy simulation settings to tabs
                    _diffDisplay.SimParams = new SimulationSettings(_lockedSettings, CopyType.CBED);

                    SaveImageButton2.IsEnabled = true;

                }
                else if (_lockedSettings.SimMode == 0)
                {
                    //Settings.SimMode = 0;
					UpdateEWImages();
					UpdateDiffractionImage();

                    // copy simulation settings to tabs
                    _ewAmplitudeDisplay.SimParams = new SimulationSettings(_lockedSettings, CopyType.Base);
                    _ewPhaseDisplay.SimParams = new SimulationSettings(_lockedSettings, CopyType.Base);
                    _diffDisplay.SimParams = new SimulationSettings(_lockedSettings, CopyType.Base);

                    _ewAmplitudeDisplay.SimParams.TEMMode = 1;
                    _ewPhaseDisplay.SimParams.TEMMode = 2;
                    _diffDisplay.SimParams.TEMMode = 3;

                    _ewAmplitudeDisplay.Tab.IsSelected = true;
                    SaveImageButton.IsEnabled = true;
                    SaveImageButton2.IsEnabled = true;
                    SimulateImageButton.IsEnabled = true;
                }
                else
                {
                    return;
                }

                UpdateWorkingColour(false);
                SimulateEWButton.IsEnabled = true;
            });

        }

        /// <summary>
        /// Chooses the correct simulation to run (TEM, STEM, CBED) depending on the radio dial checked.
        /// </summary>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void DoSimulationMethod(ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
            // Conversion to units
            var cA1t = Settings.Microscope.a1t.val / Convert.ToSingle((180/Math.PI));
            var cA2t = Settings.Microscope.a2t.val / Convert.ToSingle((180 / Math.PI));
            var cB2t = Settings.Microscope.b2t.val / Convert.ToSingle((180 / Math.PI));
            var cB = Settings.Microscope.b.val / 1000;
            var cD = Settings.Microscope.d.val / 10;
            // Upload Simulation Parameters to c++
            _mCl.setCTEMParams(Settings.Microscope.df.val, Settings.Microscope.a1m.val, cA1t, Settings.Microscope.kv.val, Settings.Microscope.cs.val, cB,
                cD, Settings.Microscope.ap.val, Settings.Microscope.a2m.val, cA2t, Settings.Microscope.b2m.val, cB2t);

            _mCl.setSTEMParams(Settings.Microscope.df.val, Settings.Microscope.a1m.val, cA1t, Settings.Microscope.kv.val, Settings.Microscope.cs.val, cB,
                cD, Settings.Microscope.ap.val);

            // Add Pixelscale to image tabs and diffraction then run simulation
            if (_lockedSettings.SimMode == 0)
			{
                _ewAmplitudeDisplay.PixelScaleX = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleX = _lockedSettings.PixelScale;

                _ewAmplitudeDisplay.PixelScaleY = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleY = _lockedSettings.PixelScale;

				_ewAmplitudeDisplay.xStartPosition = Settings.SimArea.StartX;
                _ewAmplitudeDisplay.yStartPosition = Settings.SimArea.StartY;

				SimulateTEM(ref progressReporter,ref timer, ref ct);
			}
            else if (_lockedSettings.SimMode == 2)
			{
                _diffDisplay.PixelScaleX = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleY = _lockedSettings.PixelScale;
				SimulateSTEM(ref progressReporter, ref timer, ref ct);
			}
            else if (_lockedSettings.SimMode == 1)
			{
                _diffDisplay.PixelScaleX = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleY = _lockedSettings.PixelScale;
				SimulateCBED(ref progressReporter,ref timer, ref ct);
			}
		}

        /// <summary>
        /// Simulates the exit wave of TEM
        /// </summary>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void SimulateTEM(ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
        {
            // So we have the right voltage should we try to simulate an image later.
            //_lockedSettings.ImageVoltage = _lockedSettings.Microscope.kv.val;

            //_ewAmplitudeDisplay.SimParams = new SimulationSettings(_lockedSettings, CopyType.Base);

            // Initialise
            _mCl.initialiseCTEMSimulation(_lockedSettings.Resolution, _lockedSettings.SimArea.StartX, _lockedSettings.SimArea.StartY, _lockedSettings.SimArea.EndX, _lockedSettings.SimArea.EndY,
                                          _lockedSettings.IsFull3D, _lockedSettings.IsFiniteDiff, _lockedSettings.SliceThickness.val, _lockedSettings.Integrals.val);

			// Reset atoms incase TDS has been used previously
			_mCl.sortStructure(false);

			// Use Background worker to progress through each step
			var numberOfSlices = 0;
			_mCl.getNumberSlices(ref numberOfSlices, _lockedSettings.IsFiniteDiff);

			// Seperate into setup, loop over slices and final steps to allow for progress reporting.
			for (var slice = 1; slice <= numberOfSlices; slice++)
			{
				if (ct.IsCancellationRequested)
					break;

				timer.Start();
                // Do the actual simulation
				_mCl.doMultisliceStep(slice, numberOfSlices);
				timer.Stop();
				var memUsage = _mCl.getCLMemoryUsed();

                // Update GUI stuff
				float ms = timer.ElapsedMilliseconds;
				progressReporter.ReportProgress((val) =>
				{
					CancelButton.IsEnabled = true;
					UpdateStatus(numberOfSlices, 1, slice, 1, ms, memUsage);
				}, slice);

			}

		}

        /// <summary>
        /// Simulates CBED patterns
        /// </summary>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void SimulateCBED(ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
            // Initialise probe simulation
            _mCl.initialiseSTEMSimulation(_lockedSettings.Resolution, _lockedSettings.SimArea.StartX, _lockedSettings.SimArea.StartY, _lockedSettings.SimArea.EndX, _lockedSettings.SimArea.EndY,
                                          _lockedSettings.IsFull3D, _lockedSettings.IsFiniteDiff, _lockedSettings.SliceThickness.val, _lockedSettings.Integrals.val, 1);

            // Correct probe position for when the simulation region has been changed
            var posx = (_lockedSettings.CBED.x.val - _lockedSettings.SimArea.StartX) / _lockedSettings.PixelScale;
            var posy = (_lockedSettings.CBED.y.val - _lockedSettings.SimArea.StartY) / _lockedSettings.PixelScale;

			// Get number of steps in the multislice
			var numberOfSlices = 0;
            _mCl.getNumberSlices(ref numberOfSlices, _lockedSettings.IsFiniteDiff);

            // Initialise TDS runs
			var runs = 1;
            if (_lockedSettings.CBED.DoTDS)
			{
				runs = _lockedSettings.CBED.TDSRuns.val;
			}

            // Loops TDS runs
			for (var j = 0; j < runs; j++)
			{
                // Shuffle the structure for frozen phonon
                _mCl.sortStructure(Settings.CBED.DoTDS);
                // Initialise probe
                _mCl.initialiseSTEMWaveFunction(posx, posy, 1);

                // Do the multislice for this TDS run
				for (var i = 1; i <= numberOfSlices; i++)
				{
					if (ct.IsCancellationRequested == true)
						break;

					timer.Start();
                    // Actual simulation part
					_mCl.doMultisliceStep(i, numberOfSlices);
					timer.Stop();
					var memUsage = _mCl.getCLMemoryUsed();
					float simTime = timer.ElapsedMilliseconds;

					// Update GUI multislice progress
					progressReporter.ReportProgress((val) =>
					{
						CancelButton.IsEnabled = true;
						UpdateStatus(numberOfSlices, runs, i, j, simTime, memUsage);
					}, i);
				}

				if (ct.IsCancellationRequested == true)
					break;
                // Update TDS image
				progressReporter.ReportProgress((val) =>
				{
					CancelButton.IsEnabled = false;
					UpdateDiffractionImage();
				}, j);
			}
		}

        /// <summary>
        /// Performs the STEM simulations
        /// </summary>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void SimulateSTEM(ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
            // convenience variable
            var conPix = _lockedSettings.STEM.ConcurrentPixels.val;

            // Get steps we need to move the probe in
            var xInterval = _lockedSettings.STEM.ScanArea.getxInterval;
            var yInterval = _lockedSettings.STEM.ScanArea.getyInterval;

            // Updates pixel scales for display?
			foreach (var det in _lockedDetectorDisplay)
			{
                det.PixelScaleX = xInterval;
                det.PixelScaleY = yInterval;
				det.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
			}

            // calculate the number of STEM pixels
            int numPix = _lockedSettings.STEM.ScanArea.xPixels * _lockedSettings.STEM.ScanArea.yPixels;

            // Initialise detector images
		    foreach (DetectorItem det in _lockedDetectorDisplay)
			{
				det.ImageData = new float[numPix];
				det.Min = float.MaxValue;
				det.Max = float.MinValue;
			}

            // Get number of TDS runs needed
			var numRuns = 1;
            if (_lockedSettings.STEM.DoTDS)
                numRuns = _lockedSettings.STEM.TDSRuns.val;

            // Initialise probe
            _mCl.initialiseSTEMSimulation(_lockedSettings.Resolution, _lockedSettings.SimArea.StartX, _lockedSettings.SimArea.StartY, _lockedSettings.SimArea.EndX, _lockedSettings.SimArea.EndY,
                                          _lockedSettings.IsFull3D, _lockedSettings.IsFiniteDiff, _lockedSettings.SliceThickness.val, _lockedSettings.Integrals.val, conPix);

            // Create array of all the pixels coords
			var pixels = new List<Tuple<Int32, Int32>>();

            for (var yPx = 0; yPx < _lockedSettings.STEM.ScanArea.yPixels; yPx++)
			{
                for (var xPx = 0; xPx < _lockedSettings.STEM.ScanArea.xPixels; xPx++)
				{
					pixels.Add(new Tuple<Int32,Int32>(xPx, yPx));
				}
			}

            // Loop over number of TDS runs
			for (var j = 0; j < numRuns; j++)
			{
                // shuffle the pixels so when we do TDS, we avoid doing them all next to each other
                pixels.Shuffle();

                // Current pixel number
                var nPx = 0;

                // loop through pixels
                while (nPx < numPix)
                {
                    // sort the structure if needed
                    _mCl.sortStructure(Settings.STEM.DoTDS); // is there optimisation possible here?

                    // make a copy of current pixel as we are about to modify it
                    var currentPx = nPx;

                    // Check if nPx is inside limits
                    // If it isn't, adjust the number of concurrent pixels so it is
                    if (nPx + conPix > numPix && nPx + conPix - numPix + 1 < conPix)
                    {
                        conPix = numPix - nPx;
                        nPx = numPix;
                    }
                    else
                        nPx += conPix;

                    // Make probles for all concurrent probe positions
                    for (var i = 1; i <= conPix; i++)
                    {
                        _mCl.initialiseSTEMWaveFunction(((_lockedSettings.STEM.ScanArea.StartX + pixels[(currentPx + i - 1)].Item1 * xInterval - _lockedSettings.SimArea.StartX) / _lockedSettings.PixelScale),
                            ((_lockedSettings.STEM.ScanArea.StartY + pixels[(currentPx + i - 1)].Item2 * yInterval - _lockedSettings.SimArea.StartY) / _lockedSettings.PixelScale), i);
                    }

                    // Get number of slices in our multislice
                    int numberOfSlices = 0;
                    _mCl.getNumberSlices(ref numberOfSlices, _lockedSettings.IsFiniteDiff);

                    // Loop through slices and simulate as we go
                    for (var i = 1; i <= numberOfSlices; i++)
                    {
                        if (ct.IsCancellationRequested)
                            break;

                        timer.Start();
                        // The actual simulation part
                        _mCl.doMultisliceStep(i, numberOfSlices, conPix);
                        timer.Stop();

                        var memUsage = _mCl.getCLMemoryUsed();
                        float simTime = timer.ElapsedMilliseconds;

                        // Update the progress bars
                        progressReporter.ReportProgress(val =>
                        {
                            CancelButton.IsEnabled = true;
                            UpdateStatus(numberOfSlices, _lockedSettings.STEM.TDSRuns.val, numPix, i, j, nPx, simTime, memUsage);
                        }, i);
                    }

                    if (ct.IsCancellationRequested)
                        break;

                    // loop over the pixels we jsut simulated
                    for (var p = 1; p <= conPix; p++)
                    {
                        // get the diffraction pattern (in OpenCL buffers)
                        _mCl.getSTEMDiff(p);

                        // Loop through each detectors and get each STEM pixel by summing up diffraction over the detector area
                        foreach (DetectorItem det in _lockedDetectorDisplay)
                        {
                            var pixelVal = _mCl.getSTEMPixel(det.SimParams.STEM.Inner, det.SimParams.STEM.Outer, det.SimParams.STEM.x, det.SimParams.STEM.y, p);
                            // create new variable to avoid writing this out a lot
                            var newVal = det.ImageData[_lockedSettings.STEM.ScanArea.xPixels * pixels[currentPx + p - 1].Item2 + pixels[currentPx + p - 1].Item1] + pixelVal;
                            det.ImageData[_lockedSettings.STEM.ScanArea.xPixels * pixels[currentPx + p - 1].Item2 + pixels[currentPx + p - 1].Item1] = newVal;

                            // update maximum and minimum as we go
                            if (newVal < det.Min)
                                det.Min = newVal;
                            if (newVal > det.Max)
                                det.Max = newVal;

                        }
                    }

                }

				if (ct.IsCancellationRequested == true)
					break;
			}
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

		private void SaveLeftImage(object sender, RoutedEventArgs e)
		{
			var tabs = new List<DisplayTab> {_ctemDisplay,_ewAmplitudeDisplay,_ewPhaseDisplay};
            tabs.AddRange(_lockedDetectorDisplay);

            SaveImageFromTab(tabs);
		}

        private void SaveRightImage(object sender, RoutedEventArgs e)
        {
			// Ideally want to check tab and use information to save either EW or CTEM....
			var tabs = new List<DisplayTab> {_diffDisplay};
            SaveImageFromTab(tabs);
        }

		private static void SaveImageFromTab(IEnumerable<DisplayTab> tabs)
		{
			foreach (var dt in tabs)
			{
                if (dt.Tab.IsSelected != true) continue;
			    if (dt.xDim == 0 && dt.yDim == 0) continue;

			    // File saving dialog
			    var saveDialog = new Microsoft.Win32.SaveFileDialog
			    {
			        Title = "Save Output Image",
			        DefaultExt = ".tiff",
			        Filter = "TIFF (*.tiff)|*.tiff|PNG (*.png)|*.png|JPEG (*.jpeg)|*.jpeg"
			    };

			    var result = saveDialog.ShowDialog();

			    if (result == false) return;

                var filename = saveDialog.FileName;

			    SaveImage(dt, filename);
			}
		}

        private static void SaveImage(DisplayTab dt, string filename)
        {
            // for setings stuff (test)
            string extension = System.IO.Path.GetExtension(filename);
            string result = filename.Substring(0, filename.Length - extension.Length);
            result = result + ".txt";

            SaveSimulationSettings(dt, result);

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

        /// <summary>
        /// TODO: this function needs a lookover to work out if the correct values are being used (i.e. locked versions etc.)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulateImage(object sender, RoutedEventArgs e)
        {
			if (!TestImagePrerequisites())
				return;
			//Disable simulate EW button for the duration
			SimulateEWButton.IsEnabled = false;

            var bincombo = BinningCombo.SelectedItem as ComboBoxItem;
            var binning = Convert.ToInt32(bincombo.Content);
            var CCD = CCDCombo.SelectedIndex;
            string CCDName = ((ComboBoxItem)CCDCombo.SelectedItem).Content.ToString();

            Settings.TEM.CCD = CCD;
            Settings.TEM.CCDName = CCDName;
            Settings.TEM.Binning = binning;

            // copy settings used for the exit wave (settings from Amplitude and Phase should always be the same) 
            _ctemDisplay.SimParams = new SimulationSettings(_ewAmplitudeDisplay.SimParams, CopyType.Base);

            // then need to copy TEM params from current settings
            _ctemDisplay.SimParams.UpdateImageParameters(Settings);
            

            // Conversion to units of correctness
            var cA1t = Settings.Microscope.a1t.val / Convert.ToSingle((180 / Math.PI));
            var cA2t = Settings.Microscope.a2t.val / Convert.ToSingle((180 / Math.PI));
            var cB2t = Settings.Microscope.b2t.val / Convert.ToSingle((180 / Math.PI));
            var cB = Settings.Microscope.b.val / 1000;
            var cD = Settings.Microscope.d.val / 10;

            // Upload Simulation Parameters to c++
            _mCl.setCTEMParams(Settings.Microscope.df.val, Settings.Microscope.a1m.val, cA1t, Settings.Microscope.kv.val, Settings.Microscope.cs.val, cB,
                cD, Settings.Microscope.ap.val, Settings.Microscope.a2m.val, cA2t, Settings.Microscope.b2m.val, cB2t);

			// Calculate Dose Per Pixel
            var dpp = Settings.TEM.Dose.val * (_ctemDisplay.SimParams.PixelScale * _ctemDisplay.SimParams.PixelScale);
			
            // Get CCD and Binning

            if (CCD != 0)
                _mCl.simulateCTEM(CCD, binning);
            else
                _mCl.simulateCTEM();

            //Update the displays
            UpdateCTEMImage(dpp, binning, CCD);
            UpdateDiffractionImage();

			SimulateEWButton.IsEnabled = true;
        }

        static private void SaveSimulationSettings(DisplayTab Tab, string filename)
        {
            if (Tab.SimParams.SimArea == null)
                return; // might want other checks? this one is definitely used after a simulation?

            var general = SettingsStrings.UniversalSettings;
            general = general.Replace("{{filename}}", Tab.SimParams.FileName); // save this beforehand somewhere and lock it?
            general = general.Replace("{{simareaxstart}}", Tab.SimParams.SimArea.StartX.ToString());
            general = general.Replace("{{simareaxend}}", Tab.SimParams.SimArea.EndX.ToString());
            general = general.Replace("{{simareaystart}}", Tab.SimParams.SimArea.StartY.ToString());
            general = general.Replace("{{simareayend}}", Tab.SimParams.SimArea.EndY.ToString());
            general = general.Replace("{{resolution}}", Tab.SimParams.Resolution.ToString());

            general = general.Replace("{{mode}}", Tab.SimParams.GetModeString());

            general = general.Replace("{{full3d}}", Tab.SimParams.IsFull3D.ToString());

            if (Tab.SimParams.IsFull3D)
            {
                var Full3Dopt_string = SettingsStrings.Full3dSettings;
                Full3Dopt_string = Full3Dopt_string.Replace("{{3dint}}", Tab.SimParams.Integrals.val.ToString());

                general = general.Replace("{{Full3Dopt}}", Full3Dopt_string);
            }
            else
            {
                general = general.Replace("{{Full3Dopt}}", "");
            }

            general = general.Replace("{{fd}}", Tab.SimParams.IsFiniteDiff.ToString());
            general = general.Replace("{{slicethickness}}", Tab.SimParams.SliceThickness.val.ToString());

            // Microscope

            general = general.Replace("{{volts}}", Tab.SimParams.Microscope.kv.val.ToString());

            string Microscope_string = "";

            if (Tab.SimParams.SimMode != 0 || (Tab.SimParams.SimMode == 0 && Tab.SimParams.TEMMode == 0))
            {
                Microscope_string = SettingsStrings.MicroscopeSettings;

                Microscope_string = Microscope_string.Replace("{{aperture}}", Tab.SimParams.Microscope.ap.val.ToString());
                Microscope_string = Microscope_string.Replace("{{beta}}", Tab.SimParams.Microscope.b.val.ToString());
                Microscope_string = Microscope_string.Replace("{{delta}}", Tab.SimParams.Microscope.d.val.ToString());
                Microscope_string = Microscope_string.Replace("{{defocus}}", Tab.SimParams.Microscope.df.val.ToString());
                Microscope_string = Microscope_string.Replace("{{cs}}", Tab.SimParams.Microscope.cs.val.ToString());
                Microscope_string = Microscope_string.Replace("{{A1m}}", Tab.SimParams.Microscope.a1m.val.ToString());
                Microscope_string = Microscope_string.Replace("{{A1t}}", Tab.SimParams.Microscope.a1t.val.ToString());
                Microscope_string = Microscope_string.Replace("{{A2m}}", Tab.SimParams.Microscope.a2m.val.ToString());
                Microscope_string = Microscope_string.Replace("{{A2t}}", Tab.SimParams.Microscope.a2t.val.ToString());
                Microscope_string = Microscope_string.Replace("{{B2m}}", Tab.SimParams.Microscope.b2m.val.ToString());
                Microscope_string = Microscope_string.Replace("{{B2t}}", Tab.SimParams.Microscope.b2t.val.ToString());
            }

            general = general.Replace("{{microscopesettings}}", Microscope_string);

            // mode settings
            string Mode_string = "";

            if (Tab.SimParams.SimMode == 0 && Tab.SimParams.TEM != null && Tab.SimParams.TEM.CCD != 0)
            {
                Mode_string = SettingsStrings.DoseSettings;

                Mode_string = Mode_string.Replace("{{dose}}", Tab.SimParams.TEM.Dose.val.ToString());
                Mode_string = Mode_string.Replace("{{ccd}}", Tab.SimParams.TEM.CCDName);
                Mode_string = Mode_string.Replace("{{binning}}", Tab.SimParams.TEM.Binning.ToString());
            }
            else if (Tab.SimParams.SimMode == 1)
            {
                Mode_string = SettingsStrings.CBEDSettings;

                Mode_string = Mode_string.Replace("{{cbedx}}", Tab.SimParams.CBED.x.val.ToString());
                Mode_string = Mode_string.Replace("{{cbedy}}", Tab.SimParams.CBED.y.val.ToString());
                if (Tab.SimParams.CBED.DoTDS)
                    Mode_string = Mode_string.Replace("{{cbedtds}}", Tab.SimParams.CBED.TDSRuns.val.ToString());
                else
                    Mode_string = Mode_string.Replace("{{cbedtds}}", "1");
            }
            else if (Tab.SimParams.SimMode == 2)
            {
                Mode_string = SettingsStrings.STEMSettings;

                Mode_string = Mode_string.Replace("{{multistem}}", Tab.SimParams.STEM.ConcurrentPixels.val.ToString());
                if (Tab.SimParams.STEM.DoTDS)
                    Mode_string = Mode_string.Replace("{{stemtds}}", Tab.SimParams.STEM.TDSRuns.val.ToString());
                else
                    Mode_string = Mode_string.Replace("{{stemtds}}", "1");

                var detInfo_string = SettingsStrings.STEMDetectors;

                detInfo_string = detInfo_string.Replace("{{detectorname}}", Tab.SimParams.STEM.Name);
                detInfo_string = detInfo_string.Replace("{{inner}}", Tab.SimParams.STEM.Inner.ToString());
                detInfo_string = detInfo_string.Replace("{{outer}}", Tab.SimParams.STEM.Outer.ToString());
                detInfo_string = detInfo_string.Replace("{{centx}}", Tab.SimParams.STEM.x.ToString());
                detInfo_string = detInfo_string.Replace("{{centy}}", Tab.SimParams.STEM.y.ToString());

                Mode_string = Mode_string.Replace("{{stemdetectors}}", detInfo_string);
            }

            general = general.Replace("{{modesettings}}", Mode_string);

            //var saveDialog = new Microsoft.Win32.SaveFileDialog
            //{
            //    Title = "Save Simulation Settings",
            //    DefaultExt = ".txt",
            //    Filter = "txt (*.txt)|*.txt"
            //};

            //var result = saveDialog.ShowDialog();

            //if (result == false) return;

            //var filename = saveDialog.FileName;

            File.WriteAllText(filename, general);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = e.NewSize;
            var win = sender as System.Windows.Window;

            win.Width = newSize.Width;
            win.Height = newSize.Height;

        }
    }
}
