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
	public class DisplayTab
	{
		public int xDim { get; set; }

		public int yDim { get; set; }

		public Image tImage { get; set; }

	    public float[] ImageData { get; set; }

		public TabItem Tab { get; set; }

	    public Canvas tCanvas { get; set; }

        public Label xCoord { get; set; }
        public Label yCoord { get; set; }

        private float _PixelScaleX;

        public float PixelScaleX
        {
            get { return _PixelScaleX; }
            set { _PixelScaleX = value; }
        }

        private float _PixelScaleY;

        public float PixelScaleY
        {
            get { return _PixelScaleY; }
            set { _PixelScaleY = value; }
        }

        public bool Reciprocal;

        public DisplayTab(string tName)
        {
            // Assume LeftTab by default
            // Could just make 2 classes for Left and RightTab...
            Reciprocal = false;
            PixelScaleX = PixelScaleY = 1;

            xDim = yDim = 0;
            BrushConverter bc = new BrushConverter();

            Tab = new TabItem();
            Tab.Header = tName;
            Grid tempGrid = new Grid();
            tempGrid.Background = (Brush)bc.ConvertFrom("#FFE5E5E5");
            ZoomBorder tempZoom = new ZoomBorder();
            tempZoom.ClipToBounds = true;
            tImage = new Image();

            Viewbox tvBox = new Viewbox();
            Grid temptempGrid = new Grid();
            tCanvas = new Canvas();

            tempGrid.PreviewMouseRightButtonDown += new MouseButtonEventHandler(tempZoom.public_PreviewMouseRightButtonDown);

            temptempGrid.Children.Add(tImage);
            temptempGrid.Children.Add(tCanvas);
            tvBox.Child = temptempGrid;
            tempZoom.Child = tvBox;
            tempGrid.Children.Add(tempZoom);
            Tab.Content = tempGrid;

            tImage.MouseMove += new MouseEventHandler(MouseMove);
			tImage.MouseEnter += new MouseEventHandler(MouseEnter);
			tImage.MouseLeave += new MouseEventHandler(MouseLeave);
        }

	    private WriteableBitmap ImgBMP;

	    public WriteableBitmap _ImgBMP
	    {
	        get { return ImgBMP; }
	        set { ImgBMP = value; }
	    }

        public void MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(tImage);
            if (Reciprocal)
            {
                xCoord.Content = ((2 / (xDim*PixelScaleX))*(p.X - xDim / 2)).ToString();
                yCoord.Content = ((2 / (yDim*PixelScaleY))*(yDim / 2 - p.Y)).ToString();

            }
            else
            {
                xCoord.Content = (PixelScaleX * p.X).ToString();
                yCoord.Content = (PixelScaleY * p.Y).ToString();
            }
        }

		public void MouseEnter(object sender, MouseEventArgs e)
		{
			xCoord.Visibility = Visibility.Visible;
			yCoord.Visibility = Visibility.Visible;
		}

		public void MouseLeave(object sender, MouseEventArgs e)
		{
			xCoord.Visibility = Visibility.Hidden;
			yCoord.Visibility = Visibility.Hidden;
		}

        public void SetPositionReadoutElements(ref Label tb1, ref Label tb2)
        {
            xCoord = tb1;
            yCoord = tb2;
        }
	}
}