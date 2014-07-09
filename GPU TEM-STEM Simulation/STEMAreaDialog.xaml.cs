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
    /// Interaction logic for STEMAreaDialog.xaml
    /// </summary>
    public partial class STEMAreaDialog : Window
    {
        public event EventHandler<StemAreaArgs> AddSTEMAreaEvent;

        float simxStart, simyStart, simxFinish, simyFinish;

        public STEMAreaDialog(STEMArea Area, SimArea simArea)
        {
            InitializeComponent();

            xStartBox.Text = Area.xStart.ToString("f2");
            xFinishBox.Text = Area.xFinish.ToString("f2");
            yStartBox.Text = Area.yStart.ToString("f2");
            yFinishBox.Text = Area.yFinish.ToString("f2");

            xPxBox.Text = Area.xPixels.ToString();
            yPxBox.Text = Area.yPixels.ToString();

            simxStart = simArea.xStart;
            simxFinish = simArea.xFinish;
            simyStart = simArea.yStart;
            simyFinish = simArea.yFinish;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            float xs, xf, ys, yf;
            int xp, yp;
            bool valid = true;

            try
            {
                // needed?
                xs = Convert.ToSingle(xStartBox.Text);
                ys = Convert.ToSingle(yStartBox.Text);
                xf = Convert.ToSingle(xFinishBox.Text);
                yf = Convert.ToSingle(yFinishBox.Text);
            }
            catch
            {
                return;
            }

            if (xs == xf)
            {
                xStartBox.RaiseTapEvent();
                xFinishBox.RaiseTapEvent();
                valid = false;
            }
            if (ys == yf)
            {
                yStartBox.RaiseTapEvent();
                yFinishBox.RaiseTapEvent();
                valid = false;
            }

            if (!valid)
                return;

            xp = Convert.ToInt32(xPxBox.Text);
            yp = Convert.ToInt32(yPxBox.Text);

            // Need to decide whether to just use max/min or prompt user.
            float xmin = Math.Min(xs, xf);
            float xmax = Math.Max(xs, xf);
            float ymin = Math.Min(ys, yf);
            float ymax = Math.Max(ys, yf);

            xStartBox.Text = xmin.ToString();
            xFinishBox.Text = xmax.ToString();
            yStartBox.Text = ymin.ToString();
            yFinishBox.Text = ymax.ToString();

            if (xmin < simxStart || xmin > simxFinish)
            {
                xStartBox.RaiseTapEvent();
                valid = false;
            }

            if (xmax < simxStart || xmax > simxFinish)
            {
                xFinishBox.RaiseTapEvent();
                valid = false;
            }

            if (ymin < simyStart || ymin > simyFinish)
            {
                yStartBox.RaiseTapEvent();
                valid = false;
            }

            if (ymax < simyStart || ymax > simyFinish)
            {
                yFinishBox.RaiseTapEvent();
                valid = false;
            }

            if (!valid)
                return;

            STEMArea temp = new STEMArea { xStart = xmin, xFinish = xmax, yStart = ymin, yFinish = ymax, xPixels = xp, yPixels = yp };

            AddSTEMAreaEvent(this, new StemAreaArgs(temp));

            this.Close();
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }
    }

    public class StemAreaArgs : EventArgs
    {
        private STEMArea msg;
        public StemAreaArgs(STEMArea s)
        {
            msg = s;
        }
        public STEMArea AreaParams
        {
            get { return msg; }
        }
    }
}