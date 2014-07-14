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

        public DisplayTab(string tName)
        {
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

            xCoord.Content = p.X.ToString();
            yCoord.Content = p.Y.ToString();
        }

        public void SetPositionReadoutElements(ref Label tb1, ref Label tb2)
        {
            xCoord = tb1;
            yCoord = tb2;
        }
	}
}