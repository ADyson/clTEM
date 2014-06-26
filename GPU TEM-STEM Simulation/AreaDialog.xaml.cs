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
using System.Windows.Shapes;

namespace GPUTEMSTEMSimulation
{
    /// <summary>
    /// Interaction logic for AreaDialog.xaml
    /// </summary>
    public partial class AreaDialog : Window
    {

        public event EventHandler<AreaArgs> SetAreaEvent;

        public AreaDialog(SimArea Area)
        {
            InitializeComponent();

            xStartBox.Text = Area.xStart.ToString("f2");
            xEndBox.Text = Area.xFinish.ToString("f2");
            yStartBox.Text = Area.yStart.ToString("f2");
            yEndBox.Text = Area.yFinish.ToString("f2");

        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            float xs, xf, ys, yf;
            bool valid = true;

            try
            {
                // needed?
                xs = Convert.ToSingle(xStartBox.Text);
                ys = Convert.ToSingle(yStartBox.Text);
                xf = Convert.ToSingle(xEndBox.Text);
                yf = Convert.ToSingle(yEndBox.Text);
            }
            catch
            {
                return;
            }

            if (xs == xf)
            {
                xStartBox.RaiseTapEvent();
                xEndBox.RaiseTapEvent();
                valid = false;
            }
            if (ys == yf)
            {
                yStartBox.RaiseTapEvent();
                yEndBox.RaiseTapEvent();
                valid = false;
            }

            if (!valid)
                return;

            // Need to decide whether to just use max/min or prompt user.
            float xmin = Math.Min(xs, xf);
            float xmax = Math.Max(xs, xf);
            float ymin = Math.Min(ys, yf);
            float ymax = Math.Max(ys, yf);

            SimArea temp = new SimArea { xStart = xmin, xFinish = xmax, yStart = ymin, yFinish = ymax };

            SetAreaEvent(this, new AreaArgs(temp));

            this.Close();
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }

    }

    public class AreaArgs : EventArgs
    {
        private SimArea msg;
        public AreaArgs(SimArea s)
        {
            msg = s;
        }
        public SimArea AreaParams
        {
            get { return msg; }
        }
    }

}

public class SimArea
{
    public float xStart { get; set; }

    public float xFinish { get; set; }

    public float yStart { get; set; }

    public float yFinish { get; set; }
}