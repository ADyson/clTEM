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
            String Sxs, Sxf, Sys, Syf;
            float xs, xf, ys, yf;
            bool xvalid, yvalid;

            xs = xf = ys = yf = 0;
            xvalid = yvalid = true;


            Sxs = xStartBox.Text;
            Sys = yStartBox.Text;
            Sxf = xEndBox.Text;
            Syf = yEndBox.Text;

            if (Sxs.Length == 0)
            {
                xStartBox.RaiseTapEvent();
                xvalid = false;
            }

            if (Sxf.Length == 0)
            {
                xEndBox.RaiseTapEvent();
                xvalid = false;
            }

            if (xvalid)
            {
                xs = Convert.ToSingle(Sxs);
                xf = Convert.ToSingle(Sxf);

                if (xs == xf)
                {
                    xStartBox.RaiseTapEvent();
                    xEndBox.RaiseTapEvent();
                    xvalid = false;
                }
            }

            if (Sys.Length == 0)
            {
                yStartBox.RaiseTapEvent();
                yvalid = false;
            }

            if (Syf.Length == 0)
            {
                yEndBox.RaiseTapEvent();
                yvalid = false;
            }

            if (yvalid)
            {
                ys = Convert.ToSingle(Sys);
                yf = Convert.ToSingle(Syf);

                if (ys == yf)
                {
                    yStartBox.RaiseTapEvent();
                    yEndBox.RaiseTapEvent();
                    yvalid = false;
                }
            }

            if (!(xvalid && yvalid))
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