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

		// Set labels to be updated on mouse event.
        public Label xCoord { get; set; }
        public Label yCoord { get; set; }

		// To display mouseover coordinates w.r.t original reference.
		public float xStartPosition;
		public float yStartPosition;

		// For displaying mouseover coordinates scaled correctly

	    public float PixelScaleX { get; set; }

	    public float PixelScaleY { get; set; }

	    // Mouseover coordinates displayed in reciprocal space if true.
        public bool Reciprocal;

        public DisplayTab(string tName)
        {
            // Assume LeftTab by default
            // Could just make 2 classes for Left and RightTab...
            Reciprocal = false;

            PixelScaleX = PixelScaleY = 1;
			xStartPosition = yStartPosition = 0;
            xDim = yDim = 0;

            var bc = new BrushConverter();

            Tab = new TabItem {Header = tName};
            var tempGrid = new Grid {Background = (Brush) bc.ConvertFrom("#FFE5E5E5")};
            var tempZoom = new ZoomBorder {ClipToBounds = true};
            tImage = new Image();

            var tvBox = new Viewbox();
            var temptempGrid = new Grid();
            tCanvas = new Canvas();

            tempGrid.PreviewMouseRightButtonDown += new MouseButtonEventHandler(tempZoom.public_PreviewMouseRightButtonDown);

            temptempGrid.Children.Add(tImage);
            temptempGrid.Children.Add(tCanvas);
            tvBox.Child = temptempGrid;
            tempZoom.Child = tvBox;
            tempGrid.Children.Add(tempZoom);
            Tab.Content = tempGrid;

			// To get mouseover over detectors
			tCanvas.MouseEnter += new MouseEventHandler(MouseEnter);
			tCanvas.MouseLeave += new MouseEventHandler(MouseLeave);
			tCanvas.MouseMove += new MouseEventHandler(MouseMove);
            
			// To get mouseover over image
			tImage.MouseMove += new MouseEventHandler(MouseMove);
			tImage.MouseEnter += new MouseEventHandler(MouseEnter);
			tImage.MouseLeave += new MouseEventHandler(MouseLeave);
        }

	    public WriteableBitmap ImgBmp { get; set; }

	    public void MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(tImage);
			            
			if (Reciprocal)
            {
                xCoord.Content = ((1 / (xDim*PixelScaleX))*(p.X - xDim / 2)).ToString("f2") + "1/Å";
                yCoord.Content = ((1 / (yDim*PixelScaleY))*(yDim / 2 - p.Y)).ToString("f2") + " 1/Å";
            }
            else
            {
				xCoord.Content = (xStartPosition + PixelScaleX * p.X).ToString("f2") + " Å";
				yCoord.Content = (yStartPosition + PixelScaleY * p.Y).ToString("f2") + " Å";
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