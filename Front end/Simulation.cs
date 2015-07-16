using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using SimulationGUI.Utils;
using SimulationGUI.Utils.Settings;

namespace SimulationGUI
{
    public partial class MainWindow
    {

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

            _lockedSettings.CopySettings(_settings, CopyType.All);
            _lockedDetectorDisplay = _detectorDisplay;

            // Update the display tab sizes so we don't need to worry about this later
            _ewAmplitudeDisplay.SetSize(_lockedSettings.Resolution);
            _ewPhaseDisplay.SetSize(_lockedSettings.Resolution);
            _ctemDisplay.SetSize(_lockedSettings.Resolution);
            _diffDisplay.SetSize(_lockedSettings.Resolution);
            foreach (var det in _lockedDetectorDisplay)
                det.SetSize(_lockedSettings.STEM.ScanArea.xPixels, _lockedSettings.STEM.ScanArea.yPixels);

            // Create new instances to use to cancel the simulation and to run tasks.
            _cancelToken = new CancellationTokenSource();
            var cancellationToken = _cancelToken.Token;
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
                    if (_lockedDetectorDisplay.Count == 0)
                    {
                        SimulateEWButton.IsEnabled = true;
                        return;
                    }

                    foreach (var det in _lockedDetectorDisplay)
                    {
                        UpdateStemImage(det);
                        // copy simulation settings to tab
                        det.SimParams.CopySettings(_lockedSettings, CopyType.Base);
                        // copy across so everything is in one nice place.
                        det.SimParams.STEM.ScanArea = _lockedSettings.STEM.ScanArea;
                    }

                    // Just select the first tab for convenience
                    //_lockedDetectorDisplay.Tab.IsSelected = true;e
                    SaveImageButton.IsEnabled = true;
                }
                else if (_lockedSettings.SimMode == 1)
                {
                    //UpdateDiffractionImage();

                    // copy simulation settings to tabs
                    _diffDisplay.SimParams.CopySettings(_lockedSettings, CopyType.CBED);

                    SaveImageButton2.IsEnabled = true;

                }
                else if (_lockedSettings.SimMode == 0)
                {
                    UpdateEwImages();
                    UpdateDiffractionImage();

                    // copy simulation settings to tabs
                    _ewAmplitudeDisplay.SimParams.CopySettings(_lockedSettings, CopyType.Base);
                    _ewPhaseDisplay.SimParams.CopySettings(_lockedSettings, CopyType.Base);
                    _diffDisplay.SimParams.CopySettings(_lockedSettings, CopyType.Base);

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
            // used only for old method
            var cA1T = _settings.Microscope.C12Ang.Val / Convert.ToSingle((180 / Math.PI));
            var cA2T = _settings.Microscope.C23Ang.Val / Convert.ToSingle((180 / Math.PI));
            var cB2T = _settings.Microscope.C21Ang.Val / Convert.ToSingle((180 / Math.PI));
            var cB = _settings.Microscope.Alpha.Val / 1000;
            var cD = _settings.Microscope.Delta.Val / 10;

            var M = _lockedSettings.Microscope;

            // Upload Simulation Parameters to c++
            _mCl.setMicroscopeParams(
                M.Voltage.Val,
                M.Aperture.Val,
                M.C10.Val,
                M.C12Mag.Val, M.C12Ang.Val,
                M.C21Mag.Val, M.C21Ang.Val,
                M.C23Mag.Val, M.C23Ang.Val,
                M.C30.Val,
                M.C32Mag.Val, M.C32Ang.Val,
                M.C34Mag.Val, M.C34Ang.Val,
                M.C41Mag.Val, M.C41Ang.Val,
                M.C43Mag.Val, M.C43Ang.Val,
                M.C45Mag.Val, M.C45Ang.Val,
                M.C50.Val,
                M.C52Mag.Val, M.C52Ang.Val,
                M.C54Mag.Val, M.C54Ang.Val,
                M.C56Mag.Val, M.C56Ang.Val,
                M.Alpha.Val,
                M.Delta.Val
                );

            // Add Pixelscale to image tabs and diffraction then run simulation
            if (_lockedSettings.SimMode == 0)
            {
                _ewAmplitudeDisplay.PixelScaleX = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleX = _lockedSettings.PixelScale;

                _ewAmplitudeDisplay.PixelScaleY = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleY = _lockedSettings.PixelScale;

                _ewAmplitudeDisplay.xStartPosition = _settings.SimArea.StartX;
                _ewAmplitudeDisplay.yStartPosition = _settings.SimArea.StartY;

                SimulateTEM(ref progressReporter, ref timer, ref ct);
            }
            else if (_lockedSettings.SimMode == 2)
            {
                SimulateSTEM(ref progressReporter, ref timer, ref ct);
            }
            else if (_lockedSettings.SimMode == 1)
            {
                _diffDisplay.PixelScaleX = _lockedSettings.PixelScale;
                _diffDisplay.PixelScaleY = _lockedSettings.PixelScale;
                SimulateCBED(ref progressReporter, ref timer, ref ct);
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
            // Initialise
            _mCl.initialiseCTEMSimulation(_lockedSettings.Resolution, _lockedSettings.SimArea.StartX, _lockedSettings.SimArea.StartY, _lockedSettings.SimArea.EndX, _lockedSettings.SimArea.EndY,
                                          _lockedSettings.IsFull3D, _lockedSettings.IsFiniteDiff, _lockedSettings.SliceThickness.Val, _lockedSettings.Integrals.Val);

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
            _mCl.initialiseCBEDSimulation(_lockedSettings.Resolution, _lockedSettings.SimArea.StartX, _lockedSettings.SimArea.StartY, _lockedSettings.SimArea.EndX, _lockedSettings.SimArea.EndY,
                                          _lockedSettings.IsFull3D, _lockedSettings.IsFiniteDiff, _lockedSettings.SliceThickness.Val, _lockedSettings.Integrals.Val, 1);

            // Correct probe position for when the simulation region has been changed
            var posx = (_lockedSettings.CBED.x.Val - _lockedSettings.SimArea.StartX) / _lockedSettings.PixelScale;
            var posy = (_lockedSettings.CBED.y.Val - _lockedSettings.SimArea.StartY) / _lockedSettings.PixelScale;

            // Get number of steps in the multislice
            var numberOfSlices = 0;
            _mCl.getNumberSlices(ref numberOfSlices, _lockedSettings.IsFiniteDiff);

            // Initialise TDS runs
            var runs = 1;
            if (_lockedSettings.CBED.DoTDS)
            {
                runs = _lockedSettings.CBED.TDSRuns.Val;
            }

            // Loops TDS runs
            for (var j = 0; j < runs; j++)
            {
                // Shuffle the structure for frozen phonon
                _mCl.sortStructure(_settings.CBED.DoTDS);
                // Initialise probe
                _mCl.initialiseSTEMWaveFunction(posx, posy, 1);

                // Do the multislice for this TDS run
                for (var i = 1; i <= numberOfSlices; i++)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    timer.Start();
                    // Actual simulation part
                    _mCl.doMultisliceStep(i, numberOfSlices);
                    timer.Stop();
                    var memUsage = _mCl.getCLMemoryUsed();
                    float simTime = timer.ElapsedMilliseconds;

                    // Update GUI multislice progress
                    progressReporter.ReportProgress(val =>
                    {
                        CancelButton.IsEnabled = true;
                        UpdateStatus(numberOfSlices, runs, i, j, simTime, memUsage);
                    }, i);
                }

                if (ct.IsCancellationRequested)
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
            // convenience variables
            var conPix = _lockedSettings.STEM.ConcurrentPixels.Val;
            var ScanArea = _lockedSettings.STEM.ScanArea;

            // Get steps we need to move the probe in
            var xInterval = ScanArea.getxInterval;
            var yInterval = ScanArea.getyInterval;

            // Updates pixel scales for display?
            foreach (var det in _lockedDetectorDisplay)
            {
                det.PixelScaleX = xInterval;
                det.PixelScaleY = yInterval;
                det.SetPositionReadoutElements(ref LeftXCoord, ref LeftYCoord);
            }

            // calculate the number of STEM pixels
            var numPix = ScanArea.xPixels * ScanArea.yPixels;

            // Initialise detector images
            foreach (var det in _lockedDetectorDisplay)
            {
                det.ImageData = new float[numPix];
                det.Min = float.MaxValue;
                det.Max = float.MinValue;
            }

            // Get number of TDS runs needed
            var numRuns = 1;
            if (_lockedSettings.STEM.DoTDS)
                numRuns = _lockedSettings.STEM.TDSRuns.Val;

            // Initialise probe
            _mCl.initialiseSTEMSimulation(_lockedSettings.Resolution, _lockedSettings.SimArea.StartX, _lockedSettings.SimArea.StartY, _lockedSettings.SimArea.EndX, _lockedSettings.SimArea.EndY,
                                          _lockedSettings.IsFull3D, _lockedSettings.IsFiniteDiff, _lockedSettings.SliceThickness.Val, _lockedSettings.Integrals.Val, conPix);

            // Create array of all the pixels coords
            var pixels = new List<Tuple<Int32, Int32>>();

            for (var yPx = 0; yPx < ScanArea.yPixels; yPx++)
            {
                for (var xPx = 0; xPx < ScanArea.xPixels; xPx++)
                {
                    pixels.Add(new Tuple<Int32, Int32>(xPx, yPx));
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
                    _mCl.sortStructure(_lockedSettings.STEM.DoTDS); // is there optimisation possible here?

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
                        float a = ((_lockedSettings.STEM.ScanArea.StartX + pixels[(currentPx + i - 1)].Item1*xInterval -
                                _lockedSettings.SimArea.StartX) / _lockedSettings.PixelScale);

                        float b = ((_lockedSettings.STEM.ScanArea.StartY + pixels[(currentPx + i - 1)].Item2*yInterval -
                                    _lockedSettings.SimArea.StartY)/_lockedSettings.PixelScale);

                        _mCl.initialiseSTEMWaveFunction(a,b, i);
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
                            UpdateStatus(numberOfSlices, numRuns, numPix, i, j, nPx, simTime, memUsage);
                        }, i);
                    }

                    if (ct.IsCancellationRequested)
                        break;

                    // loop over the pixels we jsut simulated
                    for (var p = 1; p <= conPix; p++)
                    {
                        // Loop through each detectors and get each STEM pixel by summing up diffraction over the detector area
                        foreach (var det in _lockedDetectorDisplay)
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

                if (ct.IsCancellationRequested)
                    break;
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
            var binning = bincombo != null ? Convert.ToInt32(bincombo.Content) : 1;

            var ccd = CCDCombo.SelectedIndex;
            var ccdName = ((ComboBoxItem)CCDCombo.SelectedItem).Content.ToString();

            _settings.TEM.CCD = ccd;
            _settings.TEM.CCDName = ccdName;
            _settings.TEM.Binning = binning;

            // copy settings used for the exit wave (settings from Amplitude and Phase should always be the same) 
            _ctemDisplay.SimParams.CopySettings(_ewAmplitudeDisplay.SimParams, CopyType.Base);

            // then need to copy TEM params from current settings
            _ctemDisplay.SimParams.UpdateImageParameters(_settings);

            var M = _ctemDisplay.SimParams.Microscope;

            // Upload Simulation Parameters to c++
            _mCl.setMicroscopeParams(
                M.Voltage.Val,
                M.Aperture.Val,
                M.C10.Val,
                M.C12Mag.Val, M.C12Ang.Val,
                M.C21Mag.Val, M.C21Ang.Val,
                M.C23Mag.Val, M.C23Ang.Val,
                M.C30.Val,
                M.C32Mag.Val, M.C32Ang.Val,
                M.C34Mag.Val, M.C34Ang.Val,
                M.C41Mag.Val, M.C41Ang.Val,
                M.C43Mag.Val, M.C43Ang.Val,
                M.C45Mag.Val, M.C45Ang.Val,
                M.C50.Val,
                M.C52Mag.Val, M.C52Ang.Val,
                M.C54Mag.Val, M.C54Ang.Val,
                M.C56Mag.Val, M.C56Ang.Val,
                M.Alpha.Val,
                M.Delta.Val
                );

            // Calculate Dose Per Pixel
            var dpp = _settings.TEM.Dose.Val * (_ctemDisplay.SimParams.PixelScale * _ctemDisplay.SimParams.PixelScale);

            // Get CCD and Binning

            if (ccd != 0)
                _mCl.simulateCTEM(ccd, binning, dpp, 1);
            else
                _mCl.simulateCTEM();

            //Update the displays
            UpdateCtemImage(dpp, binning, ccd);
            // why does this get called here?
            //UpdateDiffractionImage();

            SimulateEWButton.IsEnabled = true;
        }

    }
}
