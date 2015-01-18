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
using GPUTEMSTEMSimulation.Utils;
using ManagedOpenCLWrapper;
using BitMiracle.LibTiff.Classic;

// TODO: convert aberration angles from radians to degrees or other way around? ( /= Convert.ToSingle((180 / Math.PI)) )
// TODO: divide beta by 1000?
// TODO: times delta by 10?

namespace GPUTEMSTEMSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Elysium.Controls.Window
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
        readonly DisplayTab _ewAmplitudeDisplay = new DisplayTab("EW Amp");

        /// <summary>
        /// Display tab for the exit wave phase image
        /// </summary>
        readonly DisplayTab _ewPhaseDisplay = new DisplayTab("EW2 phase");

        /// <summary>
        /// Display tab for the diffraction image
        /// </summary>
        readonly DisplayTab _diffDisplay = new DisplayTab("Diffraction");

        // parameters

        /// <summary>
        /// List of detectors.
        /// User changeable version.
        /// </summary>
        public List<DetectorItem> Detectors = new List<DetectorItem>();

        /// <summary>
        /// Describes the region that STEM simulations will cover.
        /// User changeable version.
        /// </summary>
        public STEMArea STEMRegion = new STEMArea { xStart = 0, xFinish = 1, yStart = 0, yFinish = 1, xPixels = 1, yPixels = 1 };

        /// <summary>
        /// Stores microscope parameters
        /// </summary>
        private readonly Microscope _microscopeParams;

        private readonly fParam _dose;

        private readonly fParam _fdThickness;

        private readonly iParam _fdIntegrals;

        private readonly iParam _runsSTEM;

        private readonly iParam _multiSTEM;

        private readonly fParam _pxCBED;

        private readonly fParam _pyCBED;

        private readonly iParam _runsCBED;

        // Locking copies of editable parameters

        /// <summary>
        /// List of detectors.
        /// Locked from changes mid simulation.
        /// </summary>
        List<DetectorItem> _lockedDetectors = new List<DetectorItem>();

        /// <summary>
        /// Describes the region that STEM simulations will cover.
        /// Locked from changes mid simulation.
        /// </summary>
        STEMArea _lockedSTEMRegion;

        int _lockedResolution;

        float _lockedPixelScale;

        float _lockedWavelength;

        float _lockedVoltage;

        private float _imageVoltage;

        // uncommented

        bool IsResolutionSet = false;
        bool HaveStructure = false;
        bool IsSorted = false;
        bool doTDS_STEM = false;
        bool doTDS_CBED = false;
        bool doFull3D = true;
        bool doFD = false;
        bool DetectorVis = false;
        bool HaveMaxMrad = false;

        bool goodfinite = true;

        bool CBED_posValid = true;

        bool select_TEM = false;
        bool select_STEM = false;
        bool select_CBED = false;

        int Resolution;

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

        public SimArea SimRegion = new SimArea { xStart = 0, xFinish = 10, yStart = 0, yFinish = 10};

        bool userSIMarea;
        bool userSTEMarea;

        float pixelScale;
        float wavelength;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MainWindow()
        {
            // Initialise GPU
            InitializeComponent();

            // This was to supress some warnings, might not be needed
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;

            //add event handlers here so they aren't called when creating controls
            txtCBEDx.TextChanged += CheckCBEDValid;
            txtCBEDy.TextChanged += CheckCBEDValid;

            txtFDIntegrals.TextChanged += CheckFDValid;
            txtFDthickness.TextChanged += CheckFDValid;

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

            // Set default microscope values
            _microscopeParams = new Microscope(this);
            _microscopeParams.SetDefaults();

            // set up other parameters to update with textboxes
            _dose = new fParam();
            DoseTextBox.DataContext = _dose;
            _dose.val = 0;

            _fdThickness = new fParam();
            txtFDthickness.DataContext = _fdThickness;
            _fdThickness.val = 1;

            _fdIntegrals = new iParam();
            txtFDIntegrals.DataContext = _fdIntegrals;
            _fdIntegrals.val = 20;

            _runsSTEM = new iParam();
            STEM_TDSCounts.DataContext = _runsSTEM;
            _runsSTEM.val = 10;

            _multiSTEM = new iParam();
            mSTEM.DataContext = _multiSTEM;
            _multiSTEM.val = 10;

            _pxCBED = new fParam();
            txtCBEDx.DataContext = _pxCBED;
            _pxCBED.val = 0;

            _pyCBED = new fParam();
            txtCBEDy.DataContext = _pyCBED;
            _pyCBED.val = 0;

            _runsCBED = new iParam();
            CBED_TDSCounts.DataContext = _runsCBED;
            _runsCBED.val = 10;

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

        private void testing_click(object sender, RoutedEventArgs e)
        {
            _microscopeParams.df.sVal = "1337";
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
                fileNameLabel.Text = System.IO.Path.GetFileName(fName);
                fileNameLabel.ToolTip = fName;

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

                // Try and update the pixelscale
                UpdatePixelScale();

                // Now we want to sorting the atoms ready for the simulation process do this in a background worker...
                this.cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = this.cancellationTokenSource.Token;
                var progressReporter = new ProgressReporter();
                var task = Task.Factory.StartNew(() =>
                {
                    // This is where we start sorting the atoms in the background ready to be processed later...
                    _mCl.sortStructure(false);
                    return 0;
                },cancellationToken);

                // This runs on UI Thread so can access UI, probably better way of doing image though.
                progressReporter.RegisterContinuation(task, () =>
                {
                    IsSorted = true;
                });

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
            // Find which radio button is checked
            select_TEM = TEMRadioButton.IsChecked == true;
            select_STEM = STEMRadioButton.IsChecked == true;
            select_CBED = CBEDRadioButton.IsChecked == true;

			if (!TestSimulationPrerequisites())
				return;

            // Update GUI to 'working' colour
            Application.Current.Resources["Accent"] = Application.Current.Resources["ErrorColOrig"];

            SimulateEWButton.IsEnabled = false;
            SimulateImageButton.IsEnabled = false;

            _lockedResolution = Resolution;
            _lockedPixelScale = pixelScale;
            _lockedWavelength = wavelength;
            _lockedVoltage = _microscopeParams.kv.val;
            _lockedDetectors = Detectors; // will do even if not simulating STEM
            _lockedSTEMRegion = STEMRegion;

            // Update the display tab sizes so we don't need to worry about this later
            _ewAmplitudeDisplay.SetSize(_lockedResolution);
            _ewPhaseDisplay.SetSize(_lockedResolution);
            _ctemDisplay.SetSize(_lockedResolution);
            _diffDisplay.SetSize(_lockedResolution);

            foreach(var det in _lockedDetectors)
                det.SetSize(_lockedSTEMRegion.xPixels, _lockedSTEMRegion.yPixels);

            // Get number of TDS runs set
            var TDSruns = 1;

            if (select_STEM)
                TDSruns = _runsSTEM.val;
            else if (select_CBED)
                TDSruns = _runsCBED.val;

            // Create new instances to use to cancel the simulation and to run tasks.
            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            var progressReporter = new ProgressReporter();

            // Set the simulation parameters
			CancelButton.IsEnabled = false;
            var task = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                var timer = new Stopwatch();

                // Do cimulation part
				DoSimulationMethod(select_TEM, select_STEM, select_CBED, TDSruns, ref progressReporter, ref timer, ref cancellationToken);

            }, cancellationToken);

            // Update all the images and return application to original state
            // This runs on UI thread so can access UI, probably better way of doing image updates though
            progressReporter.RegisterContinuation(task, () =>
            {
				CancelButton.IsEnabled = false;
                pbrSlices.Value = 100;
                pbrTotal.Value = 100;

                if (select_STEM)
                {
                    if (_lockedDetectors.Count == 0)
                    {
                        SimulateEWButton.IsEnabled = true;
                        return;
                    }

                    foreach (var i in _lockedDetectors)
                    {
                        UpdateSTEMImage(i);
                    }

                    // Just select the first tab for convenience
                    _lockedDetectors[0].Tab.IsSelected = true;
                    SaveImageButton.IsEnabled = true;
                }
                else if (select_CBED)
                {
                    UpdateDiffractionImage();
                    SaveImageButton2.IsEnabled = true;

                }
                else
                {
					UpdateEWImages();
					_ewAmplitudeDisplay.Tab.IsSelected = true;
					UpdateDiffractionImage();
                    SaveImageButton.IsEnabled = true;
                    SaveImageButton2.IsEnabled = true;
                    SimulateImageButton.IsEnabled = true;
                }

                Application.Current.Resources["Accent"] = Application.Current.Resources["AccentOrig"];
                SimulateEWButton.IsEnabled = true;
            });

        }

        /// <summary>
        /// Chooses the correct simulation to run (TEM, STEM, CBED) depending on the radio dial checked.
        /// </summary>
        /// <param name="selectTEM"> true if TEM is selected.</param>
        /// <param name="selectSTEM"> true if STEM is selected.</param>
        /// <param name="selectCBED"> true if CBED is selected.</param>
        /// <param name="TDSruns"> Number of TDS runs to perform.</param>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void DoSimulationMethod(bool selectTEM, bool selectSTEM, bool selectCBED, int TDSruns, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
            //TEMP
            _microscopeParams.a1t.val /= Convert.ToSingle((180/Math.PI));
            _microscopeParams.a2t.val /= Convert.ToSingle((180 / Math.PI));
            _microscopeParams.b2t.val /= Convert.ToSingle((180 / Math.PI));
            _microscopeParams.b.val /= 1000;
            _microscopeParams.d.val /= 10;
            // Upload Simulation Parameters to c++ class
            // TODO: only use one function to set simulation parameters
            _mCl.setCTEMParams(_microscopeParams.df.val, _microscopeParams.a1m.val, _microscopeParams.a1t.val, _microscopeParams.kv.val, _microscopeParams.cs.val, _microscopeParams.b.val,
                _microscopeParams.d.val, _microscopeParams.ap.val, _microscopeParams.a2m.val, _microscopeParams.a2t.val, _microscopeParams.b2m.val, _microscopeParams.b2t.val);

            _mCl.setSTEMParams(_microscopeParams.df.val, _microscopeParams.a1m.val, _microscopeParams.a1t.val, _microscopeParams.kv.val, _microscopeParams.cs.val, _microscopeParams.b.val,
                _microscopeParams.d.val, _microscopeParams.ap.val);

            // Add Pixelscale to image tabs and diffraction then run simulation
			if (selectTEM)
			{
                _ewAmplitudeDisplay.PixelScaleX = pixelScale;
                _diffDisplay.PixelScaleX = pixelScale;

                _ewAmplitudeDisplay.PixelScaleY = pixelScale;
                _diffDisplay.PixelScaleY = pixelScale;

				_ewAmplitudeDisplay.xStartPosition = SimRegion.xStart;
				_ewAmplitudeDisplay.yStartPosition = SimRegion.yStart;

				SimulateTEM(ref progressReporter,ref timer, ref ct);
			}
			else if (selectSTEM)
			{
                _diffDisplay.PixelScaleX = pixelScale;
                _diffDisplay.PixelScaleY = pixelScale;
				SimulateSTEM(TDSruns, ref progressReporter, ref timer, ref ct);
			}
			else if (selectCBED)
			{
                _diffDisplay.PixelScaleX = pixelScale;
                _diffDisplay.PixelScaleY = pixelScale;
				SimulateCBED(TDSruns, ref progressReporter,ref timer, ref ct);
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
            _imageVoltage = _lockedVoltage;

            // Initialise
		    _mCl.initialiseCTEMSimulation(_lockedResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, doFull3D, doFD, _fdThickness.val, _fdIntegrals.val);

			// Reset atoms incase TDS has been used previously
			_mCl.sortStructure(false);

			// Use Background worker to progress through each step
			var numberOfSlices = 0;
			_mCl.getNumberSlices(ref numberOfSlices, doFD);

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
        /// <param name="numTDS">Number of TDS runs to perform</param>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void SimulateCBED(int numTDS, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
            // Initialise probe simulation
            _mCl.initialiseSTEMSimulation(_lockedResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, doFull3D, doFD, _fdThickness.val, _fdIntegrals.val, 1);

            // Correct probe position for when the simulation region has been changed
            var posx = (_pxCBED.val - SimRegion.xStart) / pixelScale;
            var posy = (_pyCBED.val - SimRegion.yStart) / pixelScale;

			// Get number of steps in the multislice
			var numberOfSlices = 0;
            _mCl.getNumberSlices(ref numberOfSlices, doFD);

            // Initialise TDS runs
			var runs = 1;
			if (doTDS_CBED)
			{
				runs = numTDS;
			}

            // Loops TDS runs
			for (var j = 0; j < runs; j++)
			{
                // Shuffle the structure for frozen phonon
				_mCl.sortStructure(doTDS_CBED);
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
        /// <param name="numTDS"> Number of TDS runs to perform.</param>
        /// <param name="progressReporter"></param>
        /// <param name="timer"></param>
        /// <param name="ct"></param>
		private void SimulateSTEM(int numTDS, ref ProgressReporter progressReporter, ref Stopwatch timer, ref CancellationToken ct)
		{
            // Lock the detectors so user can't change them mid simulation
            // might need some of these later so they are class members
            int conPix = _multiSTEM.val;

            // Make sure we have some STEM detectors
            if (_lockedDetectors.Count == 0)
            {
                var result = MessageBox.Show("No Detectors Have Been Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Resources["Accent"] = Application.Current.Resources["AccentOrig"];
                return;
            }

            // Updates pixel scales for display?
			foreach (var det in _lockedDetectors)
			{
				det.PixelScaleX = _lockedSTEMRegion.getxInterval;
				det.PixelScaleY = _lockedSTEMRegion.getyInterval;
				det.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
			}

            // calculate the number of STEM pixels
			int numPix = _lockedSTEMRegion.xPixels * _lockedSTEMRegion.yPixels;

            // Initialise detector images
		    foreach (DetectorItem det in _lockedDetectors)
			{
				det.ImageData = new float[numPix];
				det.Min = float.MaxValue;
				det.Max = float.MinValue;
			}

            // Get number of TDS runs needed
			var numRuns = 1;
            if (doTDS_STEM)
				numRuns = numTDS;

            // Initialise probe
            _mCl.initialiseSTEMSimulation(_lockedResolution, SimRegion.xStart, SimRegion.yStart, SimRegion.xFinish, SimRegion.yFinish, doFull3D, doFD, _fdThickness.val, _fdIntegrals.val, conPix);

            // Get steps we need to move the probe in
			var xInterval = _lockedSTEMRegion.getxInterval;
			var yInterval = _lockedSTEMRegion.getyInterval;

            // Create array of all the pixels coords
			var pixels = new List<Tuple<Int32, Int32>>();

			for (var yPx = 0; yPx < _lockedSTEMRegion.yPixels; yPx++)
			{
				for (var xPx = 0; xPx < _lockedSTEMRegion.xPixels; xPx++)
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
                    _mCl.sortStructure(doTDS_STEM); // is there optimisation possible here?

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
                        _mCl.initialiseSTEMWaveFunction(((_lockedSTEMRegion.xStart + pixels[(currentPx + i - 1)].Item1 * xInterval - SimRegion.xStart) / pixelScale),
                            ((_lockedSTEMRegion.yStart + pixels[(currentPx + i - 1)].Item2 * yInterval - SimRegion.yStart) / pixelScale), i);
                    }

                    // Get number of slices in our multislice
                    int numberOfSlices = 0;
                    _mCl.getNumberSlices(ref numberOfSlices, doFD);

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
                            UpdateStatus(numberOfSlices, numTDS, numPix, i, j, nPx, simTime, memUsage);
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
                        foreach (DetectorItem det in _lockedDetectors)
                        {
                            var pixelVal = _mCl.getSTEMPixel(det.Inner, det.Outer, det.xCentre, det.yCentre, p);
                            // create new variable to avoid writing this out a lot
                            var newVal = det.ImageData[_lockedSTEMRegion.xPixels * pixels[currentPx + p - 1].Item2 + pixels[currentPx + p - 1].Item1] + pixelVal;
                            det.ImageData[_lockedSTEMRegion.xPixels * pixels[currentPx + p - 1].Item2 + pixels[currentPx + p - 1].Item1] = newVal;

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
            _mCl.getEWImage(_ewAmplitudeDisplay.ImageData, _lockedResolution);
            _ewAmplitudeDisplay.Max = _mCl.getEWMax();
            _ewAmplitudeDisplay.Min = _mCl.getEWMin();
            UpdateTabImage(_ewAmplitudeDisplay, x => x);

            // Update phase
            _mCl.getEWImage2(_ewPhaseDisplay.ImageData, _lockedResolution);
            _ewPhaseDisplay.Max = _mCl.getEWMax2();
            _ewPhaseDisplay.Min = _mCl.getEWMin2();
            UpdateTabImage(_ewPhaseDisplay, x => x);
        }

        private void UpdateCTEMImage(float dpp, int binning, int CCD)
        {
            if (CCD != 0)
                _mCl.getCTEMImage(_ctemDisplay.ImageData, _lockedResolution, dpp, binning, CCD);
            else
                _mCl.getCTEMImage(_ctemDisplay.ImageData, _lockedResolution);
            _ctemDisplay.Max = _mCl.getCTEMMax();
            _ctemDisplay.Min = _mCl.getCTEMMin();
            UpdateTabImage(_ctemDisplay, x => x);
        }

        private void UpdateDiffractionImage()
        {
            _mCl.getDiffImage(_diffDisplay.ImageData, _lockedResolution);
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
            // update xdim needs to be done on appropriate simulation part?

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
		    tabs.AddRange(_lockedDetectors);

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
			    if (dt.xDim == 0 && dt.yDim == 0) continue;
			    if (dt.Tab.IsSelected != true) continue;

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

        private void SimulateImage(object sender, RoutedEventArgs e)
        {

			if (!TestImagePrerequisites())
				return;

			//Disable simulate EW button for the duration
			SimulateEWButton.IsEnabled = false;
            //

            _mCl.setCTEMParams(_microscopeParams.df.val, _microscopeParams.a1m.val, _microscopeParams.a1t.val, _imageVoltage, _microscopeParams.cs.val, _microscopeParams.b.val,
                _microscopeParams.d.val, _microscopeParams.ap.val, _microscopeParams.a2m.val, _microscopeParams.a2t.val, _microscopeParams.b2m.val, _microscopeParams.b2t.val);

			// Calculate Dose Per Pixel
			var dpp = _dose.val * (_lockedPixelScale * _lockedPixelScale);
			// Get CCD and Binning

			var bincombo = BinningCombo.SelectedItem as ComboBoxItem;

            var binning = Convert.ToInt32(bincombo.Content);
            var CCD = CCDCombo.SelectedIndex;

            if (CCD != 0)
                _mCl.simulateCTEM(CCD,binning);
            else
                _mCl.simulateCTEM();

            //Update the displays
            UpdateCTEMImage(dpp, binning, CCD);
            UpdateDiffractionImage();

			SimulateEWButton.IsEnabled = true;
        }
    }
}
