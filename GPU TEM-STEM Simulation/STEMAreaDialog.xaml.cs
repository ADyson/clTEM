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
    public partial class STEMAreaDialog : Elysium.Controls.Window
    {
        public event EventHandler<StemAreaArgs> AddSTEMAreaEvent;

        private float simxStart, simyStart, simxFinish, simyFinish;

        private bool goodxpx;
        private bool goodypx;
        private bool goodxrange;
        private bool goodyrange;

        private int xpx;
        private int ypx;
        private float xstart;
        private float xfinish;
        private float ystart;
        private float yfinish;

        public STEMAreaDialog(STEMArea Area, SimArea simArea)
        {
            InitializeComponent();

            xStartBox.Text = Area.xStart.ToString("f2");
            xFinishBox.Text = Area.xFinish.ToString("f2");
            yStartBox.Text = Area.yStart.ToString("f2");
            yFinishBox.Text = Area.yFinish.ToString("f2");

            xPxBox.Text = Area.xPixels.ToString();
            yPxBox.Text = Area.yPixels.ToString();

            xstart = Area.xStart;
            ystart = Area.yStart;
            xfinish = Area.xFinish;
            yfinish = Area.yFinish;

            xpx = Area.xPixels;
            ypx = Area.yPixels;

            simxStart = simArea.xStart;
            simxFinish = simArea.xFinish;
            simyStart = simArea.yStart;
            simyFinish = simArea.yFinish;

            xPxBox.TextChanged += new TextChangedEventHandler(PixelValidCheck);
            yPxBox.TextChanged += new TextChangedEventHandler(PixelValidCheck);
            xStartBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
            yStartBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
            xFinishBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
            yFinishBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!goodxpx || !goodxrange || !goodypx || !goodyrange)
            {
                //some sort error
                return;
            }

            var temp = new STEMArea { xStart = xstart, xFinish = xfinish, yStart = ystart, yFinish = yfinish, xPixels = xpx, yPixels = ypx };

            AddSTEMAreaEvent(this, new StemAreaArgs(temp));

            this.Close();
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }

        private void PixelValidCheck(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            var text = tbox.Text;

            var goodpx = false;

            if (text.Length < 1 || Convert.ToInt32(text) == 0)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodpx = false;
            }
            else
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                goodpx = true;
            }

            if (tbox == xPxBox)
            {
                if (!goodpx)
                {
                    goodxpx = false;
                    return;
                }
                else
                    goodxpx = true;
                xpx = Convert.ToInt32(text);
            }
            else if (tbox == yPxBox)
            {
                if (!goodpx)
                {
                    goodypx = false;
                    return;
                }
                else
                    goodypx = true;
                ypx = Convert.ToInt32(text);
            }
        }

        private void RangeValidCheck(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            var text = tbox.Text;

            if (tbox == xStartBox)
            {
                bool lt = true;
                doRangeTest(text, ref xstart, ref xstart, ref xfinish, ref tbox, ref xFinishBox, simxStart, simxFinish, ref goodxrange, lt);
            }
            else if (tbox == xFinishBox)
            {
                bool lt = false;
                doRangeTest(text, ref xfinish, ref xstart, ref xfinish, ref tbox, ref xStartBox, simxStart, simxFinish, ref goodxrange, lt);
            }
            else if (tbox == yStartBox)
            {
                bool lt = true;
                doRangeTest(text, ref ystart, ref ystart, ref yfinish, ref tbox, ref yFinishBox, simyStart, simyFinish, ref goodyrange, lt);
            }
            else if (tbox == yFinishBox)
            {
                bool lt = false;
                doRangeTest(text, ref yfinish, ref ystart, ref yfinish, ref tbox, ref yStartBox, simyStart, simyFinish, ref goodyrange, lt);
            }
            
        }

        private void doRangeTest(string text, ref float val, ref float start, ref float finish, ref TextBox tbox, ref TextBox otherbox, float min, float max, ref bool goodrange, bool lt)
        {
            tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            otherbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            if (text.Length < 1 || text == ".")
            {
                goodrange = false;
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                return;
            }
            else
                goodrange = true;

            val = Convert.ToSingle(text);

            if (lt && val < min)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodrange = false;
            }
            else if (!lt && val > max)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodrange = false;
            }

            var valid = (start < finish) && !(finish > max && start > max) && !(start < min && finish < min);

            if (!valid)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                otherbox.Background = (SolidColorBrush)Application.Current.Resources["ErrorCol"];
                goodrange = false;
            }
            else if (goodrange)
            {
                tbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
                otherbox.Background = (SolidColorBrush)Application.Current.Resources["TextBoxBackground"];
            }
        }

    }

    public class StemAreaArgs : EventArgs
    {
        public StemAreaArgs(STEMArea s)
        {
            AreaParams = s;
        }

        public STEMArea AreaParams { get; private set; }
    }
}