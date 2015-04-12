using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimulationGUI.Controls;
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
        private readonly SParam _name;
        private readonly FParam _inner;
        private readonly FParam _outer;
        private readonly FParam _centerX;
        private readonly FParam _centerY;

        private bool _goodName = true;
        private bool _goodRadii = true;

        public STEMDetectorDialog(List<DetectorItem> MainDet)
        {
            InitializeComponent();

            _name = new SParam();
            _inner = new FParam();
            _outer = new FParam();
            _centerX = new FParam();
            _centerY = new FParam();

            txtName.DataContext = _name;
            txtInner.DataContext = _inner;
            txtOuter.DataContext = _outer;
            txtCenterX.DataContext = _centerX;
            txtCenterY.DataContext = _centerY;

            // make copy of the given detectors and add the to listview
            _mainDetectors = MainDet;
            DetectorListView.ItemsSource = _mainDetectors;
            _numDet = _mainDetectors.Count;

            _name.Val = "Detector" + (_numDet + 1);
            _inner.Val = 0;
            _outer.Val = 30;
            _centerX.Val = 0;
            _centerY.Val = 0;

            // add event handlers
            txtName.TextChanged += CheckNameValid;
            txtInner.TextChanged += CheckRadiiValid;
            txtOuter.TextChanged += CheckRadiiValid;
            txtCenterX.TextChanged += CheckCentreValid;
            txtCenterY.TextChanged += CheckCentreValid;
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
            var temp = new DetectorItem(_name.Val, _inner.Val, _outer.Val, _centerX.Val, _centerY.Val, _mainDetectors.Count );

            // add to the listview
            _mainDetectors.Add(temp);
            DetectorListView.Items.Refresh();
            _numDet = _mainDetectors.Count;
            _name.Val = "Detector" + (_numDet+1);

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

            if (_mainDetectors.Any(i => i.SimParams.STEM.Name.Equals(dname)))
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
            _goodRadii = float.TryParse(text, out newVal);


            if (Equals(tbox, txtInner))
                _goodRadii = _goodRadii && newVal < _outer.Val;
            else if (Equals(tbox, txtOuter))
                _goodRadii = _goodRadii && _inner.Val < newVal;


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

        private void CheckCentreValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            var text = tbox.Text;

            float tempVal;
            var _goodCent = float.TryParse(text, out tempVal);

            if (!_goodCent)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }
            else
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            }
        }
    }
}