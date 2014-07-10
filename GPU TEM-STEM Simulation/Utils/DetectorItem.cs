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
            innerEllipse = new Ellipse();
            outerEllipse = new Ellipse();
            ringEllipse = new Ellipse();
        }

        public Brush ColBrush { get; set; }

        public int ColourIndex
        {
            get 
            {
                return ColourIndex;
            }
            set
            {
                BrushConverter bc = new BrushConverter();
                ColourGenerator.ColourGenerator cgen = new ColourGenerator.ColourGenerator();
                ColBrush = (Brush)bc.ConvertFromString("#FF" + cgen.IndexColour(value));
            }
        }

        public string Name { get; set; }

        public float Inner { get; set; }

        public float Outer { get; set; }

        public float Min { get; set; }

        public float Max { get; set; }

        public Ellipse innerEllipse { get; set; }

        public Ellipse outerEllipse { get; set; }

        public Ellipse ringEllipse { get; set; }

        private bool Added { get; set; }

        public float GetClampedPixel(int index)
        {
            return Math.Max(Math.Min(ImageData[index], Max), Min);
        }

        public void setEllipse(int res, float pxScale, float wavelength, bool vis)
        {

            if(res == 0 || pxScale == 0 || wavelength == 0)
                return;

            DoubleCollection dashes = new DoubleCollection();
            dashes.Add(4); //on
            dashes.Add(4); //off
            //dashes.Add(2); //on
            //dashes.Add(4); //off

            float innerRad = (res * pxScale) * Inner / (1000 * wavelength);
            float outerRad = (res * pxScale) * Outer / (1000 * wavelength);

            float innerShift = (res) / 2 - innerRad;
            float outerShift = (res) / 2 - outerRad;

            innerEllipse.Width = (innerRad * 2) + 0.5;
            innerEllipse.Height = (innerRad * 2) + 0.5;
            Canvas.SetTop(innerEllipse, innerShift + 0.25);
            Canvas.SetLeft(innerEllipse, innerShift + 0.25);
            innerEllipse.Stroke = ColBrush;
            innerEllipse.StrokeDashArray = dashes;

            outerEllipse.Width = (outerRad * 2) + 0.5;
            outerEllipse.Height = (outerRad * 2) + 0.5;
            Canvas.SetTop(outerEllipse, outerShift + 0.25);
            Canvas.SetLeft(outerEllipse, outerShift + 0.25);
            outerEllipse.Stroke = ColBrush;
            outerEllipse.StrokeDashArray = dashes;

            ringEllipse.Width = outerRad * 2;
            ringEllipse.Height = outerRad * 2;
            Canvas.SetTop(ringEllipse, outerShift + 0.5);
            Canvas.SetLeft(ringEllipse, outerShift + 0.5);
            ringEllipse.Fill = ColBrush;

            float ratio = innerRad / outerRad;
            RadialGradientBrush LGB = new RadialGradientBrush();
            LGB.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#22000000"), ratio));
            LGB.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), ratio - 0.00001)); // small difference to give impression of sharp edge.
            ringEllipse.OpacityMask = LGB;

            setVisibility(vis);
        }

        public void setVisibility(bool show)
        {
            if (!Added)
                return;

            if(show)
            {
                ringEllipse.Visibility = System.Windows.Visibility.Visible;
                outerEllipse.Visibility = System.Windows.Visibility.Visible;
                innerEllipse.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                ringEllipse.Visibility = System.Windows.Visibility.Hidden;
                outerEllipse.Visibility = System.Windows.Visibility.Hidden;
                innerEllipse.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public void AddToCanvas(Canvas destination)
        {
            destination.Children.Add(innerEllipse);
            destination.Children.Add(outerEllipse);
            destination.Children.Add(ringEllipse);
            Added = true;
        }

        public void RemoveFromCanvas(Canvas destination)
        {
            destination.Children.Remove(innerEllipse);
            destination.Children.Remove(outerEllipse);
            destination.Children.Remove(ringEllipse);
            Added = false;
        }
    }
}