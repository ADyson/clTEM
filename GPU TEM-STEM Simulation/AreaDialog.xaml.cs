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
    public partial class AreaDialog : Elysium.Controls.Window
    {

        private bool goodxrange = true;
        private bool goodyrange = true;

        private float xstart, ystart, xfinish, yfinish;

        public event EventHandler<AreaArgs> SetAreaEvent;

        public AreaDialog(SimArea Area)
        {
            InitializeComponent();

            xStartBox.Text = Area.xStart.ToString("f2");
            xFinishBox.Text = Area.xFinish.ToString("f2");
            yStartBox.Text = Area.yStart.ToString("f2");
            yFinishBox.Text = Area.yFinish.ToString("f2");

            xstart = Area.xStart;
            ystart = Area.yStart;
            xfinish = Area.xFinish;
            yfinish = Area.yFinish;

            xStartBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
            yStartBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
            xFinishBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
            yFinishBox.TextChanged += new TextChangedEventHandler(RangeValidCheck);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (!goodxrange || !goodyrange)
                return;

            SimArea temp = new SimArea { xStart = xstart, xFinish = xfinish, yStart = ystart, yFinish = yfinish };

            SetAreaEvent(this, new AreaArgs(temp));

            this.Close();
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }


        private void RangeValidCheck(object sender, TextChangedEventArgs e)
        {
            var tbox = sender as TextBox;
            var text = tbox.Text;

            if (tbox == xStartBox)
            {
                doRangeTest(text, ref xstart, ref xstart, ref xfinish, ref tbox, ref xFinishBox, ref goodxrange);
            }
            else if (tbox == xFinishBox)
            {
                doRangeTest(text, ref xfinish, ref xstart, ref xfinish, ref tbox, ref xStartBox, ref goodxrange);
            }
            else if (tbox == yStartBox)
            {
                doRangeTest(text, ref ystart, ref ystart, ref yfinish, ref tbox, ref yFinishBox, ref goodyrange);
            }
            else if (tbox == yFinishBox)
            {
                doRangeTest(text, ref yfinish, ref ystart, ref yfinish, ref tbox, ref yStartBox, ref goodyrange);
            }
        }

        private void doRangeTest(string text, ref float val, ref float start, ref float finish, ref TextBox tbox, ref TextBox otherbox, ref bool goodrange)
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

            var valid = (start < finish);

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