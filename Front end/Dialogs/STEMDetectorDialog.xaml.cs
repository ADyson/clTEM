using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimulationGUI.Utils;

namespace SimulationGUI.Dialogs
{
    /// <summary>
    /// Interaction logic for STEMDialog.xaml
    /// </summary>

    public partial class STEMDetectorDialog
    {
        public event EventHandler<DetectorArgs> AddDetectorEvent;
        public event EventHandler<DetectorArgs> RemDetectorEvent;

        private List<DetectorItem> _mainDetectors;
        private int _numDet;

        // these are used for text input validation
        private readonly sParam _name;
        private readonly fParam _inner;
        private readonly fParam _outer;
        private readonly fParam _centerX;
        private readonly fParam _centerY;

        private bool _goodName = true;
        private bool _goodRadii = true;

        public STEMDetectorDialog(List<DetectorItem> MainDet)
        {
            InitializeComponent();

            _name = new sParam();
            _inner = new fParam();
            _outer = new fParam();
            _centerX = new fParam();
            _centerY = new fParam();

            txtName.DataContext = _name;
            txtInner.DataContext = _inner;
            txtOuter.DataContext = _outer;
            txtCenterX.DataContext = _centerX;
            txtCenterY.DataContext = _centerY;

            // make copy of the given detectors and add the to listview
            _mainDetectors = MainDet;
            DetectorListView.ItemsSource = _mainDetectors;
            _numDet = _mainDetectors.Count;

            _name.val = "Detector" + (_numDet + 1);
            _inner.val = 0;
            _outer.val = 30;
            _centerX.val = 0;
            _centerY.val = 0;

            // add event handlers
            txtName.TextChanged += CheckNameValid;
            txtInner.TextChanged += CheckRadiiValid;
            txtOuter.TextChanged += CheckRadiiValid;
        }

        public void ClickOk(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            if (!_goodRadii || !_goodName)
            {
                // show some sort of error
                return;
            }

            // add everything to detector class
            var temp = new DetectorItem(_name.val) { Name = _name.val, Inner = _inner.val, Outer = _outer.val, 
                xCentre = _centerX.val, yCentre = _centerY.val, Min = float.MaxValue, Max = 0, ColourIndex = _mainDetectors.Count };

            // add to the listview
            _mainDetectors.Add(temp);
            DetectorListView.Items.Refresh();
            _numDet = _mainDetectors.Count;
            _name.val = "Detector" + (_numDet+1);

            // modify the mainWindow List by creating event
            if (AddDetectorEvent != null) AddDetectorEvent(this, new DetectorArgs(temp));
        }

        private void ClickDelete(object sender, RoutedEventArgs e)
        {
            // get list of the selected items
            var selected = DetectorListView.SelectedItems.Cast<Object>().OfType<DetectorItem>().ToList();

            // check if anything was selected
            if (selected.Count <= 0) return;

            // remove from listview
            foreach (var item in selected) _mainDetectors.Remove(item);//DetectorListView.Items.Remove(item);

            // used for resetting the colour index
            var i = 0;
            foreach (var item in _mainDetectors)
            {
                item.ColourIndex = i;
                i++;
            }

            // update the listview
            DetectorListView.Items.Refresh();

            // send changes to mainwindow
            RemDetectorEvent(this, new DetectorArgs(selected));
        }

        private void CheckNameValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            
            var dname = tbox.Text;
            _goodName = dname.Length > 0;

            if (_mainDetectors.Any(i => i.Name.Equals(dname)))
                _goodName = false;

            if (!_goodName)
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
        }

        private void CheckRadiiValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            var text = tbox.Text;

            float newVal;
            float.TryParse(text, out newVal);


            if (Equals(tbox, txtInner))
                _goodRadii = newVal < _outer.val;
            else if (Equals(tbox, txtOuter))
                _goodRadii = _inner.val < newVal;


            if (!_goodRadii)
            {
                txtInner.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                txtOuter.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }
            else
            {
                txtInner.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                txtOuter.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            }
        }
    }
}