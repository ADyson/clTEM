using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Framework.UI.Controls;
using Framework.UI.Input;
using GPUTEMSTEMSimulation.Dialogs;
using SimulationGUI.Utils;

namespace SimulationGUI
{
    public partial class MainWindow
    {
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
                good = fVal >= SimRegion.StartX && fVal <= SimRegion.EndX;
                ErrorMessage.ToggleCode(30, good);
            }
            else if (Equals(tbox, txtCBEDy))
            {
                float.TryParse(text, out fVal);
                good = fVal >= SimRegion.StartY && fVal <= SimRegion.EndY;
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
            if (HaveStructure && IsResolutionSet)
            {
                var BiggestSize = Math.Max(SimRegion.EndX - SimRegion.StartX, SimRegion.EndY - SimRegion.StartY);
                pixelScale = BiggestSize / _resolution;
                PixelScaleLabel.Content = pixelScale.ToString("f2") + " Å";

                UpdateMaxMrad();
            }
        }

        /// <summary>
        /// Updates maximum milliradians visible in reciprocal space
        /// </summary>
        private void UpdateMaxMrad()
        {
            if (!HaveStructure)
                return;

            var MinX = SimRegion.StartX;
            var MinY = SimRegion.StartY;

            var MaxX = SimRegion.EndX;
            var MaxY = SimRegion.EndY;

            var BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
            // Determine max mrads for reciprocal space, (need wavelength)...
            var MaxFreq = 1 / (2 * BiggestSize / _resolution);

            if (_microscopeParams.kv.val != 0 && IsResolutionSet)
            {
                const float echarge = 1.6e-19f;
                wavelength = Convert.ToSingle(6.63e-034 * 3e+008 / Math.Sqrt((echarge * _microscopeParams.kv.val * 1000 *
                    (2 * 9.11e-031 * 9e+016 + echarge * _microscopeParams.kv.val * 1000))) * 1e+010);

                var mrads = (1000 * MaxFreq * wavelength) / 2; //divide by two to get mask limits

                MaxMradsLabel.Content = mrads.ToString("f2")+" mrad";

                HaveMaxMrad = true;
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
                txtMicroscopeA2m.IsEnabled = true;
                txtMicroscopeA2t.IsEnabled = true;
                txtMicroscopeB2m.IsEnabled = true;
                txtMicroscopeB2t.IsEnabled = true;
                txtMicroscopeD.IsEnabled = true;
                txtMicroscopeB.IsEnabled = true;
                TEMbox.Visibility = Visibility.Visible;
                STEMbox.Visibility = Visibility.Hidden;
                CBEDbox.Visibility = Visibility.Hidden;

            }
            else if (STEMRadioButton.IsChecked == true)
            {
                txtMicroscopeA2m.IsEnabled = false;
                txtMicroscopeA2t.IsEnabled = false;
                txtMicroscopeB2m.IsEnabled = false;
                txtMicroscopeB2t.IsEnabled = false;
                txtMicroscopeD.IsEnabled = false;
                txtMicroscopeB.IsEnabled = false;

                STEMbox.Visibility = Visibility.Visible;
                TEMbox.Visibility = Visibility.Hidden;
                CBEDbox.Visibility = Visibility.Hidden;
            }
            else if (CBEDRadioButton.IsChecked == true)
            {
                txtMicroscopeA2m.IsEnabled = false;
                txtMicroscopeA2t.IsEnabled = false;
                txtMicroscopeB2m.IsEnabled = false;
                txtMicroscopeB2t.IsEnabled = false;
                txtMicroscopeD.IsEnabled = false;
                txtMicroscopeB.IsEnabled = false;

                STEMbox.Visibility = Visibility.Hidden;
                TEMbox.Visibility = Visibility.Hidden;
                CBEDbox.Visibility = Visibility.Visible;
            }
        }

        private void STEM_TDStoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) doTDS_STEM = chk.IsChecked == true;
        }

        private void CBED_TDStoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) doTDS_CBED = chk.IsChecked == true;
        }

        private void Full3Dtoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) doFull3D = chk.IsChecked == true;

            if (ToggleFD != null && doFull3D)
            {
                ToggleFD.IsChecked = false;
                doFD = false;
            }
        }

        private void FDtoggled(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null) doFD = chk.IsChecked == true;

            if (ToggleFull3D != null && doFD)
            {
                ToggleFull3D.IsChecked = false;
                doFull3D = false;
            }
        }

        private void Show_detectors(object sender, RoutedEventArgs e)
        {
            foreach (var i in Detectors)
            {
                i.SetVisibility(true);
            }
            DetectorVis = true;
        }

        private void Hide_Detectors(object sender, RoutedEventArgs e)
        {
            foreach (var i in Detectors)
            {
                i.SetVisibility(false);
            }
            DetectorVis = false;
        }

        private void OpenSTEMDetDlg(object sender, RoutedEventArgs e)
        {
            // open the window here
            var window = new STEMDetectorDialog(Detectors) {Owner = this};
            window.AddDetectorEvent += STEM_AddDetector;
            window.RemDetectorEvent += STEM_RemoveDetector;
            window.ShowDialog();
        }

        private void OpenSTEMAreaDlg(object sender, RoutedEventArgs e)
        {
            var window = new GPUTEMSTEMSimulation.Dialogs.STEMAreaDialog(STEMRegion, SimRegion) {Owner = this};
            window.AddSTEMAreaEvent += STEM_AddArea;
            window.ShowDialog();
        }

        void STEM_AddDetector(object sender, DetectorArgs evargs)
        {
            var added = evargs.Detector;
            LeftTab.Items.Add(added.Tab);
            added.AddToCanvas(_diffDisplay.tCanvas);
            if(HaveMaxMrad)
                added.SetEllipse(_lockedResolution, _lockedPixelScale, _lockedWavelength, DetectorVis);
        }

        void STEM_RemoveDetector(object sender, DetectorArgs evargs)
        {
            foreach (var i in evargs.DetectorList)
            {
                i.RemoveFromCanvas(_diffDisplay.tCanvas);
                LeftTab.Items.Remove(i.Tab);
            }

            foreach (var i in Detectors)
            {
                i.SetColour();
            }
        }

        void STEM_AddArea(object sender, StemAreaArgs evargs)
        {
            userSTEMarea = true;
            STEMRegion = evargs.AreaParams;
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
            var window = new SimAreaDialog(SimRegion) {Owner = this};
            window.SetAreaEvent += SetArea;
            window.Show();
        }

        void SetArea(object sender, SimAreaArgs evargs)
        {
            var changedx = false;
            var changedy = false;
            userSIMarea = true;
            SimRegion = evargs.AreaParams;

            var xscale = (STEMRegion.StartX - STEMRegion.EndX) / STEMRegion.xPixels;
            var yscale = (STEMRegion.StartY - STEMRegion.EndY) / STEMRegion.yPixels;

            if (STEMRegion.StartX < SimRegion.StartX || STEMRegion.StartX > SimRegion.EndX)
            {
                STEMRegion.StartX = SimRegion.StartX;
                changedx = true;
            }

            if (STEMRegion.EndX > SimRegion.EndX || STEMRegion.EndX < SimRegion.StartX)
            {
                STEMRegion.EndX = SimRegion.EndX;
                changedx = true;
            }

            if (STEMRegion.StartY < SimRegion.StartY || STEMRegion.StartY > SimRegion.EndY)
            {
                STEMRegion.StartY = SimRegion.StartY;
                changedy = true;
            }

            if (STEMRegion.EndY > SimRegion.EndY || STEMRegion.EndY < SimRegion.StartY)
            {
                STEMRegion.EndY = SimRegion.EndY;
                changedy = true;
            }

            if (changedx)
                STEMRegion.xPixels = (int)Math.Ceiling((STEMRegion.StartX - STEMRegion.EndX) / xscale);

            if (changedy)
                STEMRegion.yPixels = (int)Math.Ceiling((STEMRegion.StartY - STEMRegion.EndY) / yscale);

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
		    var ErrorMsg = new List<string>();
		    var WarnMsg = new List<string>();

            // At the moment easiest to check this here
            if (_lockedDetectors.Count == 0)
                ErrorMessage.AddCode(42);
            else
                ErrorMessage.RemoveCode(42);

			if (select_TEM)
			{
			    ErrorMsg = ErrorMessage.GetCTEMCodes();
                WarnMsg = WarningMessage.GetCTEMCodes();
			}
            else if (select_CBED)
            {
                ErrorMsg = ErrorMessage.GetCBEDCodes();
                WarnMsg = WarningMessage.GetCBEDCodes();
            }
            else if (select_STEM)
            {
                ErrorMsg = ErrorMessage.GetSTEMCodes();
                WarnMsg = WarningMessage.GetSTEMCodes();
            }

            if (ErrorMsg.Count == 0 && WarnMsg.Count == 0)
                return true;

		    var message = ErrorMsg.Aggregate("", (current, msg) => current + ("Error: " + msg + "\n"));
		    message = WarnMsg.Aggregate(message, (current, msg) => current + ("Warning: " + msg + "\n"));

		    if (ErrorMsg.Count == 0)
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
		    var ErrorMsg = ErrorMessage.GetImageCodes();
            var WarnMsg = WarningMessage.GetImageCodes();

            if (ErrorMsg.Count == 0 && WarnMsg.Count == 0)
                return true;

            var message = ErrorMsg.Aggregate("", (current, msg) => current + ("Error: " + msg + "\n"));

            message = WarnMsg.Aggregate(message, (current, msg) => current + ("Warning: " + msg + "\n"));

            if (ErrorMsg.Count == 0)
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

        private void cboResolutionChanged(object sender, SelectionChangedEventArgs e)
        {
            int.TryParse(ResolutionCombo.SelectedValue.ToString(), out _resolution);

            ErrorMessage.ToggleCode(2, true);

            IsResolutionSet = true;

            if (!userSTEMarea)
            {
                STEMRegion.xPixels = _resolution;
                STEMRegion.yPixels = _resolution;
            }

            UpdatePixelScale();
        }
    }
}
