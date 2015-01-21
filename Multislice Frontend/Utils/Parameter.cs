using System.ComponentModel;
using System.Globalization;

namespace SimulationGUI.Utils
{
    /// <summary>
    /// Class used to hold a variable and also keep the value in sync with a textbox
    /// </summary>
    public class fParam : INotifyPropertyChanged
    {
        public fParam()
        {
            val = 1;
        }

        public fParam(float v)
        {
            val = v;
        }

        /// <summary>
        /// Actual parameter value
        /// </summary>
        private float _val;

        /// <summary>
        /// Public value that Notifies of property changed
        /// </summary>
        public float val
        {
            get { return _val; }
            set
            {
                if (_val == value)
                    return;
                _val = value;
                NotifyPropertyChanged("sVal");
            }
        }

        /// <summary>
        /// String value used to convert strings from textboxes.
        /// </summary>
        public string sVal
        {
            get { return _val.ToString(CultureInfo.CurrentCulture); }
            set
            {
                float temp;
                float.TryParse(value, out temp);
                val = temp;
            }
        }

        /// <summary>
        /// Event when setters are called
        /// </summary>
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

    }




    /// <summary>
    /// Class used to hold a variable and also keep the value in sync with a textbox
    /// </summary>
    public class iParam : INotifyPropertyChanged
    {
        public iParam()
        {
            val = 1;
        }

        public iParam(int v)
        {
            val = v;
        }

        /// <summary>
        /// Actual parameter value
        /// </summary>
        private int _val;

        /// <summary>
        /// Public value that Notifies of property changed
        /// </summary>
        public int val
        {
            get { return _val; }
            set
            {
                if (_val == value)
                    return;
                _val = value;
                NotifyPropertyChanged("sVal");
            }
        }

        /// <summary>
        /// String value used to convert strings from textboxes.
        /// </summary>
        public string sVal
        {
            get { return _val.ToString(CultureInfo.CurrentCulture); }
            set
            {
                int temp;
                int.TryParse(value, out temp);
                val = temp;
            }
        }

        /// <summary>
        /// Event when setters are called
        /// </summary>
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

    }
}
