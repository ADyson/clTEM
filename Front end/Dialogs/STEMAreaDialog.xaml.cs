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

        private FParam _startX;
        private FParam _endX;
        private IParam _pixelX;

        private FParam _startY;
        private FParam _endY;
        private IParam _pixelY;

        private readonly float _simStartX;
        private readonly float _simStartY;
        private readonly float _simEndX;
        private readonly float _simEndY;

        private bool _goodXrange = true;
        private bool _goodYrange = true;

        public STEMAreaDialog(STEMArea Area, SimulationArea simArea)
        {
            InitializeComponent();

            _startX = new FParam();
            _endX = new FParam();
            _pixelX = new IParam();

            _startY = new FParam();
            _endY = new FParam();
            _pixelY = new IParam();

            txtStartX.DataContext = _startX;
            txtEndX.DataContext = _endX;
            txtPixelX.DataContext = _pixelX;

            txtStartY.DataContext = _startY;
            txtEndY.DataContext = _endY;
            txtPixelY.DataContext = _pixelY;

            _startX.Val = Area.StartX;
            _endX.Val = Area.EndX;
            _pixelX.Val = Area.xPixels;

            _startY.Val = Area.StartY;
            _endY.Val = Area.EndY;
            _pixelY.Val = Area.yPixels;

            _simStartX = simArea.StartX;
            _simEndX = simArea.EndX;
            _simStartY = simArea.StartY;
            _simEndY = simArea.EndY;

            txtPixelX.TextChanged += CheckPixelsValid;
            txtPixelY.TextChanged += CheckPixelsValid;
            txtStartX.TextChanged += CheckXRangeValid;
            txtStartY.TextChanged += CheckYRangeValid;
            txtEndX.TextChanged += CheckXRangeValid;
            txtEndY.TextChanged += CheckYRangeValid;
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {
            if (!_goodXrange || !_goodYrange)
            {
                //some sort error
                return;
            }

            var temp = new STEMArea { StartX = _startX.Val, EndX = _endX.Val, StartY = _startY.Val, EndY = _endY.Val, xPixels = _pixelX.Val, yPixels = _pixelY.Val };

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
            var good = int.TryParse(text, out val);

            if (!good || val == 0)
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            else
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];

        }

        private void CheckXRangeValid(object sender, TextChangedEventArgs e)
        {
            float val;
            float otherVal;
            var goodstart = float.TryParse(txtStartX.Text, out val);
            var goodend = float.TryParse(txtEndX.Text, out otherVal);

            var withinSimUpper = val <= _simEndX;
            var withinSimLower = val >= _simStartX;

            // add error about start being in range here

            _goodXrange = withinSimLower && withinSimUpper && goodstart && goodend;

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
            var goodstart = float.TryParse(txtStartY.Text, out val);
            var goodend = float.TryParse(txtEndY.Text, out otherVal);

            var withinSimUpper = val <= _simEndY;
            var withinSimLower = val >= _simStartY;

            // add error about start being in range here

            _goodYrange = withinSimLower && withinSimUpper && goodstart && goodend;

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