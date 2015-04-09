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
using SimulationGUI.Utils.Settings;

namespace SimulationGUI.Dialogs
{
    /// <summary>
    /// Interaction logic for AberrationsDialog.xaml
    /// </summary>
    public partial class AberrationsDialog
    {

        public AberrationsDialog(SimulationSettings Settings)
        {
            InitializeComponent();

            txtVoltage.DataContext = Settings.Microscope.Voltage;
            txtAperture.DataContext = Settings.Microscope.Aperture;
            txtBeta.DataContext = Settings.Microscope.Alpha;
            txtDelta.DataContext = Settings.Microscope.Delta;

            txtC10.DataContext = Settings.Microscope.C10;

            txtC12Mag.DataContext = Settings.Microscope.C12Mag;
            txtC12Ang.DataContext = Settings.Microscope.C12Ang;

            txtC21Mag.DataContext = Settings.Microscope.C21Mag;
            txtC21Ang.DataContext = Settings.Microscope.C21Ang;

            txtC23Mag.DataContext = Settings.Microscope.C23Mag;
            txtC23Ang.DataContext = Settings.Microscope.C23Ang;

            txtC30.DataContext = Settings.Microscope.C30;

            txtC32Mag.DataContext = Settings.Microscope.C32Mag;
            txtC32Ang.DataContext = Settings.Microscope.C32Ang;

            txtC34Mag.DataContext = Settings.Microscope.C34Mag;
            txtC34Ang.DataContext = Settings.Microscope.C34Ang;

            txtC41Mag.DataContext = Settings.Microscope.C41Mag;
            txtC41Ang.DataContext = Settings.Microscope.C41Ang;

            txtC43Mag.DataContext = Settings.Microscope.C43Mag;
            txtC43Ang.DataContext = Settings.Microscope.C43Ang;

            txtC45Mag.DataContext = Settings.Microscope.C45Mag;
            txtC45Ang.DataContext = Settings.Microscope.C45Ang;

            txtC50.DataContext = Settings.Microscope.C50;

            txtC52Mag.DataContext = Settings.Microscope.C52Mag;
            txtC52Ang.DataContext = Settings.Microscope.C52Ang;

            txtC54Mag.DataContext = Settings.Microscope.C54Mag;
            txtC54Ang.DataContext = Settings.Microscope.C54Ang;

            txtC56Mag.DataContext = Settings.Microscope.C56Mag;
            txtC56Ang.DataContext = Settings.Microscope.C56Ang;
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
