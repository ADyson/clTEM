using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SimulationGUI.Utils
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

        public float xCentre { get; set; }

        public float yCentre { get; set; }

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

            var dashes = new DoubleCollection {4, 4}; // {on, off, on, etc}
            
            var innerRad = (res * pxScale) * Inner / (1000 * wavelength);
            var outerRad = (res * pxScale) * Outer / (1000 * wavelength);
            var xcRad = (res * pxScale) * xCentre / (1000 * wavelength);
            var ycRad = (res * pxScale) * yCentre / (1000 * wavelength);

            var innerShift = (res) / 2 - innerRad;
            var outerShift = (res) / 2 - outerRad;

            InnerEllipse.Width = (innerRad * 2) + 0.5;
            InnerEllipse.Height = (innerRad * 2) + 0.5;
            Canvas.SetTop(InnerEllipse, innerShift + 0.25 - ycRad);
            Canvas.SetLeft(InnerEllipse, innerShift + 0.25 + xcRad);
            InnerEllipse.StrokeDashArray = dashes;

            OuterEllipse.Width = (outerRad * 2) + 0.5;
            OuterEllipse.Height = (outerRad * 2) + 0.5;
            Canvas.SetTop(OuterEllipse, outerShift + 0.25 - ycRad);
            Canvas.SetLeft(OuterEllipse, outerShift + 0.25 + xcRad);
            OuterEllipse.StrokeDashArray = dashes;

            RingEllipse.Width = outerRad * 2;
            RingEllipse.Height = outerRad * 2;
            Canvas.SetTop(RingEllipse, outerShift + 0.5 - ycRad);
            Canvas.SetLeft(RingEllipse, outerShift + 0.5 + xcRad);

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