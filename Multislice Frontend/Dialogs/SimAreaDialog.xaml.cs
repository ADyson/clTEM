using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimulationGUI;
using SimulationGUI.Utils;

namespace GPUTEMSTEMSimulation.Dialogs
{
    /// <summary>
    /// Interaction logic for SimAreaDialog.xaml
    /// </summary>
    public partial class SimAreaDialog
    {
        private readonly fParam _startX;
        private readonly fParam _endX;
        private readonly fParam _startY;
        private readonly fParam _endY;


        private bool _goodXrange = true;
        private bool _goodYrange = true;

        public event EventHandler<SimAreaArgs> SetAreaEvent;

        public SimAreaDialog(SimArea area)
        {
            InitializeComponent();

            _startX = new fParam();
            _startY = new fParam();
            _endX = new fParam();
            _endY = new fParam();

            txtStartX.DataContext = _startX;
            txtEndX.DataContext = _endX;
            txtStartY.DataContext = _startY;
            txtEndY.DataContext = _endY;

            _startX.val = area.StartX;
            _startY.val = area.StartY;
            _endX.val = area.EndX;
            _endY.val = area.EndY;

            txtStartX.TextChanged += CheckXRangeValid;
            txtStartY.TextChanged += CheckYRangeValid;
            txtEndX.TextChanged += CheckXRangeValid;
            txtEndY.TextChanged += CheckYRangeValid;
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {

            if (!(_goodXrange && _goodYrange))
                return;

            var temp = new SimArea { StartX = _startX.val, EndX = _endX.val, StartY = _startY.val, EndY = _endY.val };

            if (SetAreaEvent != null) SetAreaEvent(this, new SimAreaArgs(temp));

            Close();
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CheckXRangeValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            var text = tbox.Text;

            float newVal;
            float.TryParse(text, out newVal);

            if (Equals(tbox, txtStartX))
                _goodXrange = newVal < _endX.val;
            else if (Equals(tbox, txtEndX))
                _goodXrange = _startX.val < newVal;

            if (!_goodXrange) 
            {
                txtStartX.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                txtEndX.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }
            else
            {
                txtStartX.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                txtEndX.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            }
        }

        private void CheckYRangeValid(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            if (tbox == null) return;
            var text = tbox.Text;

            float newVal;
            float.TryParse(text, out newVal);

            if (Equals(tbox, txtStartY))
                _goodYrange = newVal < _endY.val;
            else if (Equals(tbox, txtEndY))
                _goodYrange = _startY.val < newVal;

            if (!_goodYrange)
            {
                txtStartY.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                txtEndY.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
            }
            else
            {
                txtStartY.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                txtEndY.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            }
        }
        
    }
}