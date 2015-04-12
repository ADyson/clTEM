using System.ComponentModel;
using System.Globalization;

namespace SimulationGUI.Utils
{
    /// <summary>
    /// Class used to hold a variable and also keep the value in sync with a textbox
    /// </summary>
    public class FParam : INotifyPropertyChanged
    {
        public FParam()
        {
            Val = 1.0f;
        }

        public FParam(float v)
        {
            Val = v;
        }

        /// <summary>
        /// Actual parameter value
        /// </summary>
        private float _val;

        /// <summary>
        /// Public value that Notifies of property changed
        /// </summary>
        public float Val
        {
            get { return _val; }
            set
            {
                if (_val == value)
                    return;
                _val = value;
                NotifyPropertyChanged("SVal");
            }
        }

        /// <summary>
        /// String value used to convert strings from textboxes.
        /// </summary>
        public string SVal
        {
            get { return _val.ToString(); }
            set
            {
                float temp;
                string temps;

                if (value.EndsWith("E"))
                    temps = value + "0";
                else
                    temps = value;

                float.TryParse(temps, out temp);
                Val = temp;
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
    public class IParam : INotifyPropertyChanged
    {
        public IParam()
        {
            Val = 1;
        }

        public IParam(int v)
        {
            Val = v;
        }

        /// <summary>
        /// Actual parameter value
        /// </summary>
        private int _val;

        /// <summary>
        /// Public value that Notifies of property changed
        /// </summary>
        public int Val
        {
            get { return _val; }
            set
            {
                if (_val == value)
                    return;
                _val = value;
                NotifyPropertyChanged("SVal");
            }
        }

        /// <summary>
        /// String value used to convert strings from textboxes.
        /// </summary>
        public string SVal
        {
            get { return _val.ToString(); }
            set
            {
                int temp;
                int.TryParse(value, out temp);
                Val = temp;
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

    public class SParam : INotifyPropertyChanged
    {
        public SParam()
        {
            Val = "";
        }

        public SParam(string v)
        {
            Val = v;
        }

        /// <summary>
        /// Actual parameter value
        /// </summary>
        private string _val;

        /// <summary>
        /// Public value that Notifies of property changed
        /// </summary>
        public string Val
        {
            get { return _val; }
            set
            {
                if (_val == value)
                    return;
                _val = value;
                NotifyPropertyChanged("SVal");
            }
        }

        /// <summary>
        /// String value used to convert strings from textboxes.
        /// Here for compatibility with other Paams.
        /// </summary>
        public string SVal
        {
            get { return _val; }
            set { Val = value; }
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
