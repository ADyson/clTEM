using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;

using System.Collections.ObjectModel;
using System.ComponentModel;

using PanAndZoom;

namespace GPUTEMSTEMSimulation
{
    /// <summary>
    /// Interaction logic for STEMDialog.xaml
    /// </summary>

    public partial class STEMDialog : Window
    {
        public event EventHandler<DetectorArgs> AddDetectorEvent;
        public event EventHandler<DetectorArgs> RemDetectorEvent;

        public List<DetectorItem> mainDetectors;

        public STEMDialog(List<DetectorItem> MainDet)
        {
            InitializeComponent();

            // make copy of the given detectors and add the to listview
            mainDetectors = MainDet;
            DetectorListView.ItemsSource = mainDetectors;

            // needed so it doesnt default to on when too many items are selected
            ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Hidden);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // to check if entry is valid
            bool valid = true;

            // get the strings from the textboxes
            string Sname = NameTxtbx.Text;
            string Sin = InnerTxtbx.Text;
            string Sout = OuterTxtbx.Text;

            float Fin = 0;
            float Fout = 0;

            // must have name
            if (Sname.Length == 0)
            {
                NameTxtbx.RaiseTapEvent();
                valid = false;
            }

            // check for duplicate names
            foreach (DetectorItem i in mainDetectors)
                if (i.Name.Equals(Sname))
                {
                    valid = false;
                    NameTxtbx.RaiseTapEvent();
                    break;
                }

            // convert inputs to floats (error checking should be handled by regular expression)
            Fout = Convert.ToSingle(Sout);
            Fin = Convert.ToSingle(Sin);

            // check the outer radii is bigger than the inner
            // could just auto place the larger number as outer?
            if (Fin >= Fout)
            {
                InnerTxtbx.RaiseTapEvent();
                OuterTxtbx.RaiseTapEvent();
                valid = false;
            }

            // if anything went wrong above, exit here
            if (!valid)
                return;

            // create brush converter (used to get brush colours)
            BrushConverter bc = new BrushConverter();

            // create the tab and all children
            TabItem tempTab = new TabItem();
            tempTab.Header = Sname;
            Grid tempGrid = new Grid();
            tempGrid.Background = (Brush)bc.ConvertFrom("#FFE5E5E5");
            ZoomBorder tempZoom = new ZoomBorder();
            tempZoom.ClipToBounds = true;
            Image tImage = new Image();

            // set children
            tempZoom.Child = tImage;
            tempGrid.Children.Add(tempZoom);
            tempTab.Content = tempGrid;

            // add everything to detector class
            DetectorItem temp = new DetectorItem { Name = Sname, Inner = Fin, Outer = Fout, Tab = tempTab, Min = float.MaxValue, Max = 0, Image = tImage, ColourIndex = mainDetectors.Count };

            // modify the mainWindow List by creating event
            //AddDetectorEvent(this, new DetectorArgs(temp));

            // add to the listview
            mainDetectors.Add(temp);

            DetectorListView.Items.Refresh();


        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // get list of the selected items
            List<DetectorItem> selected = DetectorListView.SelectedItems.Cast<Object>().OfType<DetectorItem>().ToList();

            // check if anything was selected
            if (selected.Count > 0)
            {
                // remove from listview
                foreach (var item in selected) mainDetectors.Remove(item);//DetectorListView.Items.Remove(item);

                // used for resetting the colour index
                int i = 0;
                foreach (var item in mainDetectors)
                {
                    item.ColourIndex = i;
                    i++;
                }

                DetectorListView.Items.Refresh();

                //RemDetectorEvent(this, new DetectorArgs(current));
            }
        }


        // To try and hide the scrollbar, maybe could be animated later?
        private void DetectorListView_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DetectorListView.Items.Count > 7) // bodged and hard coded
            {
                ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Visible);
            }
        }

        private void DetectorListView_MouseLeave(object sender, MouseEventArgs e)
        {
            ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Hidden);
            
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }

    }

    public class DetectorArgs : EventArgs
    {
        private DetectorItem msg;
        public DetectorArgs(DetectorItem s)
        {
            msg = s;
        }
        public DetectorItem Detector
        {
            get { return msg; }
        }

        private Array msgArr;
        public DetectorArgs(Array sArr)
        {
            msgArr = sArr;
        }
        public Array DetectorArr
        {
            get { return msgArr; }
        }

        private List<DetectorItem> msgList;
        public DetectorArgs(List<DetectorItem> sList)
        {
            msgList = sList;
        }
        public List<DetectorItem> DetectorList
        {
            get { return msgList; }
        }
    }
}

public class DetectorItem
{
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
            Brush tits = (Brush)bc.ConvertFromString("#FF" + cgen.IndexColour(value));
            ColBrush = tits;
        }
    }

    public string Name { get; set; }

    public float Inner { get; set; }

    public float Outer { get; set; }

    public float Min { get; set; }

    public float Max { get; set; }

    public float[] ImageData { get; set; }

    public Image Image { get; set; }

    public TabItem Tab { get; set; }

    public Ellipse innerEllipse { get; set; }

    public Ellipse outerEllipse { get; set; }

    public void updateEllipse(int res, float pxScale, float wavelength)
    {
        float innerRad = (res * pxScale) * Inner / (1000 * wavelength);
        float outerRad = (res * pxScale) * Outer / (1000 * wavelength);

        float innerShift = (res) / 2 - innerRad;
        float outerShift = (res) / 2 - outerRad;

        innerEllipse = new Ellipse();
        innerEllipse.Width = innerRad * 2;
        innerEllipse.Height = innerRad * 2;
        Canvas.SetTop(innerEllipse, innerShift);
        Canvas.SetLeft(innerEllipse, innerShift);
        innerEllipse.Stroke = Brushes.Red;
        //innerEllipse.Visibility = System.Windows.Visibility.Hidden;

        outerEllipse = new Ellipse();
        outerEllipse.Width = outerRad * 2;
        outerEllipse.Height = outerRad * 2;
        Canvas.SetTop(outerEllipse, outerShift);
        Canvas.SetLeft(outerEllipse, outerShift);
        innerEllipse.Stroke = Brushes.Red;
        //outerEllipse.Visibility = System.Windows.Visibility.Hidden;
    }

    private WriteableBitmap ImgBMP;

    public WriteableBitmap _ImgBMP
    {
        get { return ImgBMP; }
        set { ImgBMP = value; }
    }

    public float GetClampedPixel(int index)
    {
        return Math.Max(Math.Min(ImageData[index], Max), Min);
    }

}

namespace FixedWidthColumnSample
{
    public class FixedWidthColumn : GridViewColumn
    {
        static FixedWidthColumn()
        {
            WidthProperty.OverrideMetadata(typeof(FixedWidthColumn),
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
        }

        public double FixedWidth
        {
            get { return (double)GetValue(FixedWidthProperty); }
            set { SetValue(FixedWidthProperty, value); }
        }

        public static readonly DependencyProperty FixedWidthProperty =
            DependencyProperty.Register(
                "FixedWidth",
                typeof(double),
                typeof(FixedWidthColumn),
                new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnFixedWidthChanged)));

        private static void OnFixedWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            FixedWidthColumn fwc = o as FixedWidthColumn;
            if (fwc != null)
                fwc.CoerceValue(WidthProperty);
        }

        private static object OnCoerceWidth(DependencyObject o, object baseValue)
        {
            FixedWidthColumn fwc = o as FixedWidthColumn;
            if (fwc != null)
                return fwc.FixedWidth;
            return baseValue;
        }
    }
}

namespace ErrTextBoxSample
{
    public class ErrTextBox : TextBox
    {
        public static readonly RoutedEvent TapEvent = EventManager.RegisterRoutedEvent(
        "Tap", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ErrTextBox));

        public event RoutedEventHandler Tap
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        public void RaiseTapEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ErrTextBox.TapEvent);
            RaiseEvent(newEventArgs);
        }
    }
}