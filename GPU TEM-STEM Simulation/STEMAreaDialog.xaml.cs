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
        public event EventHandler<AreaArgs> AddAreaEvent;

        public STEMAreaDialog(STEMArea Area)
        {
            InitializeComponent();

            xStartBox.Text = Area.xStart.ToString("f2");
            xFinishBox.Text = Area.xFinish.ToString("f2");
            yStartBox.Text = Area.yStart.ToString("f2");
            yFinishBox.Text = Area.yFinish.ToString("f2");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            float xs, xf, ys, yf;
            int xp, yp;

            try
            {
                xs = Convert.ToSingle(xStartBox.Text);
                ys = Convert.ToSingle(yStartBox.Text);
                xf = Convert.ToSingle(xFinishBox.Text);
                yf = Convert.ToSingle(yFinishBox.Text);
            }
            catch
            {
                return;
            }

            xp = Convert.ToInt32(xPxBox.Text);
            yp = Convert.ToInt32(yPxBox.Text);

            STEMArea temp = new STEMArea { xStart = xs, xFinish = xf, yStart = ys, yFinish = yf, xPixels = xp, yPixels = yp };

            AddAreaEvent(this, new AreaArgs(temp));

            this.Close();
        }
    }





    public class AreaArgs : EventArgs
    {
        private STEMArea msg;
        public AreaArgs(STEMArea s)
        {
            msg = s;
        }
        public STEMArea AreaParams
        {
            get { return msg; }
        }
    }
}


public class STEMArea
{
    public float xStart { get; set; }

    public float xFinish { get; set; }

    public float yStart { get; set; }

    public float yFinish { get; set; }

    public int xPixels { get; set; }

    public int yPixels { get; set; }
}