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
    public partial class MainWindow : Window
    {

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
                ImagingParameters.astigang /= Convert.ToSingle((180 / Math.PI));
                ProbeParameters.astigang /= Convert.ToSingle((180 / Math.PI));
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
                ImagingParameters.b2ang /= Convert.ToSingle((180 / Math.PI));
                ProbeParameters.b2ang /= Convert.ToSingle((180 / Math.PI));
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
                ImagingParameters.astig2ang /= Convert.ToSingle((180 / Math.PI));
                ProbeParameters.astig2ang /= Convert.ToSingle((180 / Math.PI));
            }
        }

        private void ImagingA2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astig2mag);
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
            var window = new STEMDetectorDialog(Detectors);
            window.Owner = this;
            window.AddDetectorEvent += new EventHandler<DetectorArgs>(STEM_AddDetector);
            window.RemDetectorEvent += new EventHandler<DetectorArgs>(STEM_RemoveDetector);
            window.ShowDialog();
        }

        private void STEMArea_Click(object sender, RoutedEventArgs e)
        {
            var window = new STEMAreaDialog(STEMRegion, SimRegion);
            window.Owner = this;
            window.AddSTEMAreaEvent += new EventHandler<StemAreaArgs>(STEM_AddArea);
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

        void STEM_AddArea(object sender, StemAreaArgs evargs)
        {
            userSTEMarea = true;
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

        private void SetAreaButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AreaDialog(SimRegion);
            window.Owner = this;
            window.SetAreaEvent += new EventHandler<AreaArgs>(SetArea);
            window.ShowDialog();
        }

        void SetArea(object sender, AreaArgs evargs)
        {
            bool changedx = false;
            bool changedy = false;
            userSIMarea = true;
            SimRegion = evargs.AreaParams;

            float xscale = (STEMRegion.xStart - STEMRegion.xFinish) / STEMRegion.xPixels;
            float yscale = (STEMRegion.yStart - STEMRegion.yFinish) / STEMRegion.yPixels;

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

            //var result = MessageBox.Show("STEM limits now out of bounds and have been rescaled", "", MessageBoxButton.OK, MessageBoxImage.Error);
            
            UpdateMaxMrad();
        }

        private void gridZoom_reset(object sender, MouseButtonEventArgs e)
        {
            Grid tempGrid = sender as Grid;

            var child = VisualTreeHelper.GetChild(tempGrid, 0) as ZoomBorder;
            if (child != null)
                child.Reset();
        }

    }
}
