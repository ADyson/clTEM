using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PanAndZoom;

namespace SimulationGUI.Utils
{
	public class DisplayTab
	{
		public int xDim { get; set; }

		public int yDim { get; set; }

		public Image tImage { get; set; }

	    public float[] ImageData { get; set; }

        /// <summary>
        /// Maximum value of image data
        /// </summary>
        public float Max { get; set; }

        /// <summary>
        /// Minimum value of image data
        /// </summary>
        public float Min { get; set; }

        /// <summary>
        /// Sets images sizes with the square dimensions
        /// </summary>
        /// <param name="sz">The size of x and y</param>
        public void SetSize(int sz)
        {
            xDim = sz;
            yDim = sz;
            ImageData = new float[sz * sz];
        }

        /// <summary>
        /// Sets image sizes with size x by y
        /// </summary>
        /// <param name="x">width of image</param>
        /// <param name="y">height of image</param>
	    public void SetSize(int x, int y)
	    {
	        xDim = x;
	        yDim = y;
            ImageData = new float[x*y];
	    }

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
            var panelCol = (SolidColorBrush)Application.Current.Resources["PanelDark"];
            var tempGrid = new Grid { Background = panelCol };
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
                yCoord.Content = ((1 / (yDim * PixelScaleY)) * (yDim / 2 - p.Y)).ToString("f2") + " 1/Å";
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