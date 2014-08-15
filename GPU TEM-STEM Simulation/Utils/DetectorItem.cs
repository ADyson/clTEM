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
    public class DetectorItem : DisplayTab
    {
        public DetectorItem(string tabName) : base(tabName)
        {
            InnerEllipse = new Ellipse();
            OuterEllipse = new Ellipse();
            RingEllipse = new Ellipse();

            CurrentResolution = 0;
            CurrentPixelScale = 0;
            CurrentWaveLength = 0;
        }

        private int CurrentResolution;

        private float CurrentPixelScale;

        private float CurrentWaveLength;

        public Brush ColBrush { get; set; }

        public int ColourIndex
        {
            get 
            {
                return ColourIndex;
            }
            set
            {
                var bc = new BrushConverter();
                var cgen = new ColourGenerator.ColourGenerator();
                ColBrush = (Brush)bc.ConvertFromString("#FF" + cgen.IndexColour(value));
            }
        }

        public string Name { get; set; }

        public float Inner { get; set; }

        public float Outer { get; set; }

        public float Min { get; set; }

        public float Max { get; set; }

        public Ellipse InnerEllipse { get; set; }

        public Ellipse OuterEllipse { get; set; }

        public Ellipse RingEllipse { get; set; }

        private bool Added { get; set; }

        public float GetClampedPixel(int index)
        {
            return Math.Max(Math.Min(ImageData[index], Max), Min);
        }

        public void SetColour()
        {
            InnerEllipse.Stroke = ColBrush;
            OuterEllipse.Stroke = ColBrush;
            RingEllipse.Fill = ColBrush;
        }

        public void SetColour(Brush userBrush)
        {
            InnerEllipse.Stroke = userBrush;
            OuterEllipse.Stroke = userBrush;
            RingEllipse.Fill = userBrush;
        }


        public void SetEllipse(int res, float pxScale, float wavelength, bool vis)
        {
            // check image has been created, values arent constructor values
            if(res == 0 || pxScale == 0 || wavelength == 0)
                return;

            // check if detector needs to be redrawn
            if(CurrentResolution == res && CurrentPixelScale == pxScale && CurrentWaveLength == wavelength)
                return;

            CurrentResolution = res;
            CurrentPixelScale = pxScale;
            CurrentWaveLength = wavelength;

            var dashes = new DoubleCollection {4, 4};
            
            var innerRad = (res * pxScale) * Inner / (1000 * wavelength);
            var outerRad = (res * pxScale) * Outer / (1000 * wavelength);

            var innerShift = (res) / 2 - innerRad;
            var outerShift = (res) / 2 - outerRad;

            InnerEllipse.Width = (innerRad * 2) + 0.5;
            InnerEllipse.Height = (innerRad * 2) + 0.5;
            Canvas.SetTop(InnerEllipse, innerShift + 0.25);
            Canvas.SetLeft(InnerEllipse, innerShift + 0.25);
            InnerEllipse.StrokeDashArray = dashes;

            OuterEllipse.Width = (outerRad * 2) + 0.5;
            OuterEllipse.Height = (outerRad * 2) + 0.5;
            Canvas.SetTop(OuterEllipse, outerShift + 0.25);
            Canvas.SetLeft(OuterEllipse, outerShift + 0.25);
            OuterEllipse.StrokeDashArray = dashes;

            RingEllipse.Width = outerRad * 2;
            RingEllipse.Height = outerRad * 2;
            Canvas.SetTop(RingEllipse, outerShift + 0.5);
            Canvas.SetLeft(RingEllipse, outerShift + 0.5);

            var ratio = innerRad / outerRad;
            var LGB = new RadialGradientBrush();
            LGB.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#22000000"), ratio));
            LGB.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), ratio - 0.00001)); // small difference to give impression of sharp edge.
            RingEllipse.OpacityMask = LGB;

            SetColour();
            SetVisibility(vis);
        }

        public void SetVisibility(bool show)
        {
            if(show)
            {
                RingEllipse.Visibility = System.Windows.Visibility.Visible;
                OuterEllipse.Visibility = System.Windows.Visibility.Visible;
                InnerEllipse.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                RingEllipse.Visibility = System.Windows.Visibility.Hidden;
                OuterEllipse.Visibility = System.Windows.Visibility.Hidden;
                InnerEllipse.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public void AddToCanvas(Canvas destination)
        {
            destination.Children.Add(InnerEllipse);
            destination.Children.Add(OuterEllipse);
            destination.Children.Add(RingEllipse);
        }

        public void RemoveFromCanvas(Canvas destination)
        {
            destination.Children.Remove(InnerEllipse);
            destination.Children.Remove(OuterEllipse);
            destination.Children.Remove(RingEllipse);
        }
    }
}