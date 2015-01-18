using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PanAndZoom;
using Window = Elysium.Controls.Window;

namespace GPUTEMSTEMSimulation
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Check CBED positions are in simulation range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckCBEDValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;

            float val;

            if (Equals(tbox, txtCBEDx))
            {
                float.TryParse(txtCBEDx.Text, out val);
                CBED_posValid = val >= SimRegion.xStart && val <= SimRegion.xFinish;
            }
            else if (Equals(tbox, txtCBEDy))
            {
                float.TryParse(txtCBEDy.Text, out val);
                CBED_posValid = val >= SimRegion.yStart && val <= SimRegion.yFinish;
            }
            else
                return;

            if (!CBED_posValid)
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
        }

        /// <summary>
        /// Updates the pixel scale and also the maximum milliradians
        /// </summary>
        private void UpdatePixelScale()
        {
            if (HaveStructure && IsResolutionSet)
            {
                var BiggestSize = Math.Max(SimRegion.xFinish - SimRegion.xStart, SimRegion.yFinish - SimRegion.yStart);
                pixelScale = BiggestSize / Resolution;
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

            var MinX = SimRegion.xStart;
            var MinY = SimRegion.yStart;

            var MaxX = SimRegion.xFinish;
            var MaxY = SimRegion.yFinish;

            var BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
            // Determine max mrads for reciprocal space, (need wavelength)...
            var MaxFreq = 1 / (2 * BiggestSize / Resolution);

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
            var window = new STEMAreaDialog(STEMRegion, SimRegion) {Owner = this};
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
        }

        private void OpenAreaDlg(object sender, RoutedEventArgs e)
        {
            var window = new AreaDialog(SimRegion) {Owner = this};
            window.SetAreaEvent += SetArea;
            window.ShowDialog();
        }

        void SetArea(object sender, AreaArgs evargs)
        {
            var changedx = false;
            var changedy = false;
            userSIMarea = true;
            SimRegion = evargs.AreaParams;

            var xscale = (STEMRegion.xStart - STEMRegion.xFinish) / STEMRegion.xPixels;
            var yscale = (STEMRegion.yStart - STEMRegion.yFinish) / STEMRegion.yPixels;

            if (STEMRegion.xStart < SimRegion.xStart || STEMRegion.xStart > SimRegion.xFinish)
            {
                STEMRegion.xStart = SimRegion.xStart;
                changedx = true;
            }

            if (STEMRegion.xFinish > SimRegion.xFinish || STEMRegion.xFinish < SimRegion.xStart)
            {
                STEMRegion.xFinish = SimRegion.xFinish;
                changedx = true;
            }

            if (STEMRegion.yStart < SimRegion.yStart || STEMRegion.yStart > SimRegion.yFinish)
            {
                STEMRegion.yStart = SimRegion.yStart;
                changedy = true;
            }

            if (STEMRegion.yFinish > SimRegion.yFinish || STEMRegion.yFinish < SimRegion.yStart)
            {
                STEMRegion.yFinish = SimRegion.yFinish;
                changedy = true;
            }

            if (changedx)
                STEMRegion.xPixels = (int)Math.Ceiling((STEMRegion.xStart - STEMRegion.xFinish) / xscale);

            if (changedy)
                STEMRegion.yPixels = (int)Math.Ceiling((STEMRegion.yStart - STEMRegion.yFinish) / yscale);

			UpdatePixelScale();
        }

        // TODO: implement more of this into regex, or do similar highlighting
        // TODO: implement an error system so the user knows what went wrong.
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
			// Check We Have Structure
			if (HaveStructure == false)
			{
				var result = MessageBox.Show("No Structure Loaded", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			// Check parameters are set
			if (IsResolutionSet == false)
			{
				var result = MessageBox.Show("Resolution Not Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			// Check for OpenCL device.
			if (DeviceSelector.SelectedIndex == -1)
			{
				var result = MessageBox.Show("OpenCL Device Not Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			// Check we have sensible parameters.
			if (_microscopeParams.kv.val == 0)
			{
				var result = MessageBox.Show("Voltage cannot be zero", "", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
            if (_microscopeParams.ap.val == 0)
			{
				var result = MessageBox.Show("Aperture should not be zero, do you want to continue?", "Continue?", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
				return result.Equals(MessageBoxResult.Yes);
			}
            if (!CBED_posValid && select_CBED)
            {
                var result = MessageBox.Show("CBED probe position outside simulated region", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (ToggleFD.IsChecked == true && !goodfinite)
            {
                var result = MessageBox.Show("Incorrect finite difference settings", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
			return true;
		}

		private bool TestImagePrerequisites()
		{
            if (_microscopeParams.ap.val == 0)
			{
				var result = MessageBox.Show("Aperture should not be zero, do you want to continue?", "Continue?", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
				return result.Equals(MessageBoxResult.Yes);
			}
		    return true;
		}

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        // TODO: chec what values FD can have. (e.g. they must have to be greater than 0?)
        private void CheckFDValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            var text = tbox.Text;

            float val;
            if (!float.TryParse(text, out val))
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodfinite = false;
                return;
            }

            if (val <= 0)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodfinite = false;
            }
            else
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                goodfinite = true;
            }
        }

        private void ComboBoxSelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            Resolution = Convert.ToInt32(ResolutionCombo.SelectedValue.ToString());

            IsResolutionSet = true;

            if (!userSTEMarea)
            {
                STEMRegion.xPixels = Resolution;
                STEMRegion.yPixels = Resolution;
            }

            UpdatePixelScale();
        }
    }
}
