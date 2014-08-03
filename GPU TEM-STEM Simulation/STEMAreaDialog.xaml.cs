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

            string Sxs, Sxf, Sys, Syf, Sxp, Syp;
            float xs, xf, ys, yf, xmin, xmax, ymin, ymax;
            int xp, yp;
            bool valid, xvalid, yvalid;

            valid = xvalid = yvalid = true;
            xmin = xmax = ymin = ymax = 0;
            xp = yp = 0;
            

            Sxs = xStartBox.Text;
            Sys = yStartBox.Text;
            Sxf = xFinishBox.Text;
            Syf = yFinishBox.Text;
            Sxp = xPxBox.Text;
            Syp = yPxBox.Text;

            if (Sxp.Length == 0)
            {
                xPxBox.RaiseTapEvent();
                valid = false;
            }

            if (Syp.Length == 0)
            {
                yPxBox.RaiseTapEvent();
                valid = false;
            }

            if (valid)
            {
                xp = Convert.ToInt32(Sxp);
                yp = Convert.ToInt32(Syp);

                if ( xp == 0 )
                {
                    xPxBox.RaiseTapEvent();
                    valid = false;
                }

                if ( yp == 0 ) 
                {
                    yPxBox.RaiseTapEvent();
                    valid = false;
                }
            }

            if (Sxs.Length == 0)
            {
                xStartBox.RaiseTapEvent();
                xvalid = false;
            }

            if (Sxf.Length == 0)
            {
                xFinishBox.RaiseTapEvent();
                xvalid = false;
            }

            if (xvalid)
            {
                xs = Convert.ToSingle(Sxs);
                xf = Convert.ToSingle(Sxf);

                xmin = Math.Min(xs, xf);
                xmax = Math.Max(xs, xf);

                if (xs == xf)
                {
                    xStartBox.RaiseTapEvent();
                    xFinishBox.RaiseTapEvent();
                    xvalid = false;
                }

                if (xmin < simxStart || xmin > simxFinish)
                {
					if(xmin==xs)
						xStartBox.RaiseTapEvent();
					else if(xmin==xf)
						xFinishBox.RaiseTapEvent();
                    xvalid = false;
                }

                if (xmax < simxStart || xmax > simxFinish)
                {
					if (xmax == xs)
						xStartBox.RaiseTapEvent();
					else if (xmax == xf)
						xFinishBox.RaiseTapEvent();

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
                yFinishBox.RaiseTapEvent();
                yvalid = false;
            }

            if (yvalid)
            {
                ys = Convert.ToSingle(Sys);
                yf = Convert.ToSingle(Syf);

                ymin = Math.Min(ys, yf);
                ymax = Math.Max(ys, yf);

                if (ys == yf)
                {
                    yStartBox.RaiseTapEvent();
                    yFinishBox.RaiseTapEvent();
                    yvalid = false;
                }

                if (ymin < simyStart || ymin > simyFinish)
                {
					if (ymin == ys)
						yStartBox.RaiseTapEvent();
					else if (ymax == yf)
						yFinishBox.RaiseTapEvent();
                    yvalid = false;
                }

                if (ymax < simyStart || ymax > simyFinish)
                {
					if (ymax == ys)
						yStartBox.RaiseTapEvent();
					else if (ymax == yf)
						yFinishBox.RaiseTapEvent();
                    yvalid = false;
                }
            }

            if (!(valid && xvalid && yvalid))
                return;

            xStartBox.Text = xmin.ToString();
            xFinishBox.Text = xmax.ToString();
            yStartBox.Text = ymin.ToString();
            yFinishBox.Text = ymax.ToString();

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