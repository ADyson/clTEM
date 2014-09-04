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
using System.Windows.Media.Animation;
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

    public partial class STEMDetectorDialog : Elysium.Controls.Window
    {
        public event EventHandler<DetectorArgs> AddDetectorEvent;
        public event EventHandler<DetectorArgs> RemDetectorEvent;

        public List<DetectorItem> mainDetectors;
        int numDet;

        // these are used for text input validation
        private string dname;
        private float din;
        private float dout;
        private float dxc;
        private float dyc;

        private bool isname = true;
        private bool uniquename = true;
        private bool goodradii = true;
        private bool goodcent = true;


        public STEMDetectorDialog(List<DetectorItem> MainDet)
        {
            InitializeComponent();

            // make copy of the given detectors and add the to listview
            mainDetectors = MainDet;
            DetectorListView.ItemsSource = mainDetectors;

            numDet = mainDetectors.Count;

			dname = "Detector" + (numDet + 1).ToString();
            din = 0;
            dout = 30;
            dxc = 0;
            dyc = 0;

            NameTxtbx.Text = dname.ToString();
            InnerTxtbx.Text = din.ToString();
            OuterTxtbx.Text = dout.ToString();
            xcTxtbx.Text = dxc.ToString();
            ycTxtbx.Text = dyc.ToString();

            // add event handlers
            NameTxtbx.TextChanged += new TextChangedEventHandler(NameValidCheck);
            InnerTxtbx.TextChanged += new TextChangedEventHandler(RadValidCheck);
            OuterTxtbx.TextChanged += new TextChangedEventHandler(RadValidCheck);
            xcTxtbx.TextChanged += new TextChangedEventHandler(CentValidCheck);
            ycTxtbx.TextChanged += new TextChangedEventHandler(CentValidCheck);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

            if (!goodradii || !isname || !uniquename)
            {
                // show some sort of error
                return;
            }

            // add everything to detector class
            var temp = new DetectorItem(dname) { Name = dname, Inner = din, Outer = dout, xCentre = dxc, yCentre = dyc, Min = float.MaxValue, Max = 0, ColourIndex = mainDetectors.Count };

            // add to the listview
            mainDetectors.Add(temp);
            DetectorListView.Items.Refresh();
            numDet = mainDetectors.Count;

            NameTxtbx.Text = "Detector" + (numDet+1).ToString();

            // modify the mainWindow List by creating event
            AddDetectorEvent(this, new DetectorArgs(temp));
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // get list of the selected items
            var selected = DetectorListView.SelectedItems.Cast<Object>().OfType<DetectorItem>().ToList();

            // check if anything was selected
            if (selected.Count > 0)
            {
                // remove from listview
                foreach (var item in selected) mainDetectors.Remove(item);//DetectorListView.Items.Remove(item);

                // used for resetting the colour index
                var i = 0;
                foreach (var item in mainDetectors)
                {
                    item.ColourIndex = i;
                    i++;
                }

                // update the listview
                DetectorListView.Items.Refresh();

                // send changes to mainwindow
                RemDetectorEvent(this, new DetectorArgs(selected));
            }
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }

        private void NameValidCheck(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox != NameTxtbx)
                return;

            var valid = true;

            dname = tbox.Text;
            isname = dname.Length > 0;

            if (mainDetectors.Any(i => i.Name.Equals(dname)))
                uniquename = false;
            else
                uniquename = true;

            valid = valid && isname && uniquename;

            if (!valid)
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

        }

        private void RadValidCheck(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            var text = tbox.Text;

            if (text.Length < 1 || text == ".")
            {
                goodradii = false;
                InnerTxtbx.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                OuterTxtbx.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                return;
            }
            else
                goodradii = true;

            if (tbox == InnerTxtbx)
                din = Convert.ToSingle(text);
            else if (tbox == OuterTxtbx)
                dout = Convert.ToSingle(text);

            goodradii = din < dout;

            if (!goodradii)
            {
                InnerTxtbx.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                OuterTxtbx.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }
            else
            {
                InnerTxtbx.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                OuterTxtbx.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            }
        }

        private void CentValidCheck(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            var text = tbox.Text;

            if (text.Length < 1 || text == "-" || text == ".")
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodcent = false;
                return;
            }
            else
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                goodcent = true;
            }

            if (tbox == xcTxtbx)
                dxc = Convert.ToSingle(text);
            else if (tbox == ycTxtbx)
                dyc = Convert.ToSingle(text);
        }
    }


    public class DetectorArgs : EventArgs
    {
        public DetectorArgs(DetectorItem s)
        {
            Detector = s;
        }

        public DetectorItem Detector { get; private set; }

        public DetectorArgs(List<DetectorItem> sList)
        {
            DetectorList = sList;
        }

        public List<DetectorItem> DetectorList { get; private set; }
    }
}