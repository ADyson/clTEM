using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Elysium.Parameters;

namespace SimulationGUI.Dialogs
{

    public enum WarningColour
    {
        None,
        Error,
        Warning
    }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WarningDialog : INotifyPropertyChanged
    {
        private int _myResult;

        private string _messageText;

        public string MessageText
        {
            get { return _messageText; }
            set
            {
                _messageText = value;
                NotifyPropertyChanged("MessageText");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This calls the event when setters are called
        /// </summary>
        private void NotifyPropertyChanged(string id)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(id));
            }
        }

        public WarningDialog(string test = "Unknown error :(", MessageBoxButton buttons = MessageBoxButton.OKCancel, WarningColour colour = WarningColour.None)
        {
            InitializeComponent();
            MessageBlock.DataContext = this;
            MessageText = test;

            if(buttons == MessageBoxButton.OK)
                btnCancel.Visibility = Visibility.Hidden;

            switch (colour)
            {
                case WarningColour.Error:
                    Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                    SetButtonCol();
                    break;
                case WarningColour.Warning:
                    Background = (SolidColorBrush)Application.Current.Resources["Accent"];
                    SetButtonCol();
                    break;
            }
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {
            _myResult = 1;
            Close();
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            _myResult = 0;
            Close();
        }

        private void SetButtonCol()
        {
            Design.SetAccentBrush(this, (SolidColorBrush)Application.Current.Resources["PanelDark"]);
            Manager.SetAccentBrush(this, (SolidColorBrush)Application.Current.Resources["PanelDark"]);
        }

        // I can't work out why, but this gets called twice and sets the DialogResults to false, so i use this to reset teh value.
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            DialogResult = _myResult == 1;
        }
    }
}
