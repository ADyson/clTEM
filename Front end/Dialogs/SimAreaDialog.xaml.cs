using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimulationGUI;
using SimulationGUI.Utils;

namespace SimulationGUI.Dialogs
{
    /// <summary>
    /// Interaction logic for SimAreaDialog.xaml
    /// </summary>
    public partial class SimAreaDialog
    {
        private readonly FParam _startX;
        private readonly FParam _endX;
        private readonly FParam _startY;
        private readonly FParam _endY;


        private bool _goodXrange = true;
        private bool _goodYrange = true;

        public event EventHandler<SimAreaArgs> SetAreaEvent;

        public SimAreaDialog(SimulationArea area)
        {
            InitializeComponent();

            _startX = new FParam();
            _startY = new FParam();
            _endX = new FParam();
            _endY = new FParam();

            txtStartX.DataContext = _startX;
            txtEndX.DataContext = _endX;
            txtStartY.DataContext = _startY;
            txtEndY.DataContext = _endY;

            _startX.Val = area.StartX;
            _startY.Val = area.StartY;
            _endX.Val = area.EndX;
            _endY.Val = area.EndY;

            txtStartX.TextChanged += CheckXRangeValid;
            txtStartY.TextChanged += CheckYRangeValid;
            txtEndX.TextChanged += CheckXRangeValid;
            txtEndY.TextChanged += CheckYRangeValid;
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {

            if (!(_goodXrange && _goodYrange))
                return;

            var temp = new SimulationArea { StartX = _startX.Val, EndX = _endX.Val, StartY = _startY.Val, EndY = _endY.Val };

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
            _goodXrange = float.TryParse(text, out newVal);

            if (Equals(tbox, txtStartX))
                _goodXrange = _goodXrange && newVal < _endX.Val;
            else if (Equals(tbox, txtEndX))
                _goodXrange = _goodXrange && _startX.Val < newVal;

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
            _goodYrange = float.TryParse(text, out newVal);

            if (Equals(tbox, txtStartY))
                _goodYrange = _goodYrange && newVal < _endY.Val;
            else if (Equals(tbox, txtEndY))
                _goodYrange = _goodYrange && _startY.Val < newVal;

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