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

    public partial class STEMDetectorDialog : Window
    {
        public event EventHandler<DetectorArgs> AddDetectorEvent;
        public event EventHandler<DetectorArgs> RemDetectorEvent;

        public List<DetectorItem> mainDetectors;
        int numDet;

        public STEMDetectorDialog(List<DetectorItem> MainDet)
        {
            InitializeComponent();

            // make copy of the given detectors and add the to listview
            mainDetectors = MainDet;
            DetectorListView.ItemsSource = mainDetectors;

            numDet = mainDetectors.Count;
			NameTxtbx.Text = "Detector" + (numDet + 1).ToString();

            // needed so it doesnt default to on when too many items are selected
            ScrollViewer.SetVerticalScrollBarVisibility(DetectorListView, ScrollBarVisibility.Hidden);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // to check if entry is valid
            var valid = true;

            // get the strings from the textboxes
            var Sname = NameTxtbx.Text;
            var Sin = InnerTxtbx.Text;
            var Sout = OuterTxtbx.Text;

            float Fin = 0;
            float Fout = 0;

            // must have name
            if (Sname.Length == 0)
            {
                NameTxtbx.RaiseTapEvent();
                valid = false;
            }

            // check for duplicate names
            if (mainDetectors.Any(i => i.Name.Equals(Sname)))
            {
                valid = false;
                NameTxtbx.RaiseTapEvent();
            }

            // convert inputs to floats (error checking should be handled by regular expression)
            if (Sout.Length == 0)
            {
                OuterTxtbx.RaiseTapEvent();
                valid = false;
            }

            if (Sin.Length == 0)
            {
                InnerTxtbx.RaiseTapEvent();
                valid = false;
            }

            if (!valid)
                return;

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

            // add everything to detector class
            var temp = new DetectorItem(Sname) { Name = Sname, Inner = Fin, Outer = Fout, Min = float.MaxValue, Max = 0, ColourIndex = mainDetectors.Count };

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