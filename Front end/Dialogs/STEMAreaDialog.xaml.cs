using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimulationGUI;
using SimulationGUI.Utils;

namespace SimulationGUI.Dialogs
{
    /// <summary>
    /// Interaction logic for STEMAreaDialog.xaml
    /// </summary>
    public partial class STEMAreaDialog
    {
        public event EventHandler<StemAreaArgs> AddSTEMAreaEvent;

        private fParam _startX;
        private fParam _endX;
        private iParam _pixelX;

        private fParam _startY;
        private fParam _endY;
        private iParam _pixelY;

        private readonly float _simStartX;
        private readonly float _simStartY;
        private readonly float _simEndX;
        private readonly float _simEndY;

        private bool _goodXrange = true;
        private bool _goodYrange = true;

        public STEMAreaDialog(STEMArea Area, SimArea simArea)
        {
            InitializeComponent();

            _startX = new fParam();
            _endX = new fParam();
            _pixelX = new iParam();

            _startY = new fParam();
            _endY = new fParam();
            _pixelY = new iParam();

            txtStartX.DataContext = _startX;
            txtEndX.DataContext = _endX;
            txtPixelX.DataContext = _pixelX;

            txtStartY.DataContext = _startY;
            txtEndY.DataContext = _endY;
            txtPixelY.DataContext = _pixelY;

            _startX.val = Area.StartX;
            _endX.val = Area.EndX;
            _pixelX.val = Area.xPixels;

            _startY.val = Area.StartY;
            _endY.val = Area.EndY;
            _pixelY.val = Area.yPixels;

            _simStartX = simArea.StartX;
            _simEndX = simArea.EndX;
            _simStartY = simArea.StartY;
            _simEndY = simArea.EndY;

            txtPixelX.TextChanged += CheckPixelsValid;
            txtPixelY.TextChanged += CheckPixelsValid;
            txtStartX.TextChanged += CheckXRangeValid;
            txtStartY.TextChanged += CheckXRangeValid;
            txtEndX.TextChanged += CheckXRangeValid;
            txtEndY.TextChanged += CheckXRangeValid;
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {
            if (!_goodXrange || !_goodYrange)
            {
                //some sort error
                return;
            }

            var temp = new STEMArea { StartX = _startX.val, EndX = _endX.val, StartY = _startY.val, EndY = _endY.val, xPixels = _pixelX.val, yPixels = _pixelY.val };

            if (AddSTEMAreaEvent != null) AddSTEMAreaEvent(this, new StemAreaArgs(temp));

            Close();
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void CheckPixelsValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            var text = tbox.Text;

            int val;
            int.TryParse(text, out val);

            if (val == 0)
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

        }

        private void CheckXRangeValid(object sender, TextChangedEventArgs e)
        {
            float val;
            float otherVal;
            float.TryParse(txtStartX.Text, out val);
            float.TryParse(txtEndX.Text, out otherVal);

            var withinSimUpper = val <= _simEndX;
            var withinSimLower = val >= _simStartX;

            // add error about start being in range here

            _goodXrange = withinSimLower && withinSimUpper;

            if (!(withinSimLower && withinSimUpper))
                txtStartX.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                txtStartX.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

            withinSimUpper = otherVal <= _simEndX;
            withinSimLower = otherVal >= _simStartX;

            // add error about end being in range here

            _goodXrange = _goodXrange && withinSimLower && withinSimUpper;

            if (!(withinSimLower && withinSimUpper))
                txtEndX.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                txtEndX.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

            var goodOrder = val < otherVal;

            if (!goodOrder)
            {
                txtStartX.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                txtEndX.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }
 
            // add error about end before start here.

            _goodXrange = _goodXrange && goodOrder;
        }

        private void CheckYRangeValid(object sender, TextChangedEventArgs e)
        {
            float val;
            float otherVal;
            float.TryParse(txtStartY.Text, out val);
            float.TryParse(txtEndY.Text, out otherVal);

            var withinSimUpper = val <= _simEndY;
            var withinSimLower = val >= _simStartY;

            // add error about start being in range here

            _goodYrange = withinSimLower && withinSimUpper;

            if (!(withinSimLower && withinSimUpper))
                txtStartY.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                txtStartY.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

            withinSimUpper = otherVal <= _simEndY;
            withinSimLower = otherVal >= _simStartY;

            // add error about end being in range here

            _goodYrange = _goodYrange && withinSimLower && withinSimUpper;

            if (!(withinSimLower && withinSimUpper))
                txtEndY.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                txtEndY.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

            var goodOrder = val < otherVal;

            if (!goodOrder)
            {
                txtStartY.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                txtEndY.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }

            // add error about end before start here.

            _goodYrange = _goodYrange && goodOrder;
        }

    }
}