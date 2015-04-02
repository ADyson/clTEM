using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Framework.UI.Controls;
using Framework.UI.Input;
using SimulationGUI.Dialogs;
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
            if (HaveStructure && IsResolutionSet)
            {
                var BiggestSize = Math.Max(Settings.SimArea.EndX - Settings.SimArea.StartX, Settings.SimArea.EndY - Settings.SimArea.StartY);
                Settings.PixelScale = BiggestSize / Settings.Resolution;
                PixelScaleLabel.Content = Settings.PixelScale.ToString("f2") + " Å";

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

            var MinX = Settings.SimArea.StartX;
            var MinY = Settings.SimArea.StartY;

            var MaxX = Settings.SimArea.EndX;
            var MaxY = Settings.SimArea.EndY;

            var BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
            // Determine max mrads for reciprocal space, (need wavelength)...
            var MaxFreq = 1 / (2 * BiggestSize / Settings.Resolution);

            if (Settings.Microscope.kv.val != 0 && IsResolutionSet)
            {
                const float echarge = 1.6e-19f;
                Settings.Wavelength = Convert.ToSingle(6.63e-034 * 3e+008 / Math.Sqrt((echarge * Settings.Microscope.kv.val * 1000 *
                    (2 * 9.11e-031 * 9e+016 + echarge * Settings.Microscope.kv.val * 1000))) * 1e+010);

                var mrads = (1000 * MaxFreq * Settings.Wavelength) / 2; //divide by two to get mask limits

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

                Settings.SimMode = 0;

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

                Settings.SimMode = 2;
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
            foreach (var i in Settings.STEM.Detectors)
            {
                i.SetVisibility(true);
            }
            DetectorVis = true;
        }

        private void Hide_Detectors(object sender, RoutedEventArgs e)
        {
            foreach (var i in Settings.STEM.Detectors)
            {
                i.SetVisibility(false);
            }
            DetectorVis = false;
        }

        private void OpenSTEMDetDlg(object sender, RoutedEventArgs e)
        {
            // open the window here
            var window = new Dialogs.STEMDetectorDialog(Settings.STEM.Detectors) { Owner = this };
            window.AddDetectorEvent += STEM_AddDetector;
            window.RemDetectorEvent += STEM_RemoveDetector;
            window.ShowDialog();
        }

        private void OpenSTEMAreaDlg(object sender, RoutedEventArgs e)
        {
            var window = new SimulationGUI.Dialogs.STEMAreaDialog(Settings.STEM.ScanArea, Settings.SimArea) { Owner = this };
            window.AddSTEMAreaEvent += STEM_AddArea;
            window.ShowDialog();
        }

        void STEM_AddDetector(object sender, DetectorArgs evargs)
        {
            var added = evargs.Detector;
            LeftTab.Items.Add(added.Tab);
            added.AddToCanvas(_diffDisplay.tCanvas);
            if(HaveMaxMrad)
                added.SetEllipse(_lockedSettings.Resolution, _lockedSettings.PixelScale, _lockedSettings.Wavelength, DetectorVis);
        }

        void STEM_RemoveDetector(object sender, DetectorArgs evargs)
        {
            foreach (var i in evargs.DetectorList)
            {
                i.RemoveFromCanvas(_diffDisplay.tCanvas);
                LeftTab.Items.Remove(i.Tab);
            }

            foreach (var i in Settings.STEM.Detectors)
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
            var window = new SimAreaDialog(Settings.SimArea) {Owner = this};
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
		    var ErrorMsg = new List<string>();
		    var WarnMsg = new List<string>();

            // At the moment easiest to check this here
            if (Settings.STEM.Detectors.Count == 0)
                ErrorMessage.AddCode(42);
            else
                ErrorMessage.RemoveCode(42);

			if (Settings.SimMode == 0)
			{
			    ErrorMsg = ErrorMessage.GetCTEMCodes();
                WarnMsg = WarningMessage.GetCTEMCodes();
			}
            else if (Settings.SimMode == 1)
            {
                ErrorMsg = ErrorMessage.GetCBEDCodes();
                WarnMsg = WarningMessage.GetCBEDCodes();
            }
            else if (Settings.SimMode == 2)
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
            int.TryParse(ResolutionCombo.SelectedValue.ToString(), out Settings.Resolution);

            ErrorMessage.ToggleCode(2, true);

            IsResolutionSet = true;

            if (!Settings.STEM.UserSetArea)
            {
                Settings.STEM.ScanArea.xPixels = Settings.Resolution;
                Settings.STEM.ScanArea.yPixels = Settings.Resolution;
            }

            UpdatePixelScale();
        }
    }
}
