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
using System.Windows.Media.Animation;
using GPU_TEM_STEM_Simulation;

namespace GPUTEMSTEMSimulation
{
    public partial class resourceDictionary
    {
        public resourceDictionary()
        {
            InitializeComponent();
        }

        private void tBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tBox = sender as TextBox;
            tBox.SelectAll();
        }
    }
}