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
        public STEMAreaDialog(StemArea Area)
        {
            InitializeComponent();

            xStartBox.Text = Area.xStart.ToString("f2");
            xFinishBox.Text = Area.xFinish.ToString("f2");
            yStartBox.Text = Area.yStart.ToString("f2");
            yFinishBox.Text = Area.yFinish.ToString("f2");
        }
    }


    public class StemArea
    {
        public float xStart { get; set; }

        public float xFinish { get; set; }

        public float yStart { get; set; }

        public float yFinish { get; set; }

        public int xPixels { get; set; }

        public int yPixels { get; set; }
    }
}
