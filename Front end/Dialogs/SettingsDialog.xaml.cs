using System.Windows;
using SimulationGUI.Controls;
using SimulationGUI.Utils.Settings;

namespace SimulationGUI.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog
    {
        public SettingsDialog(DisplayTab tab)
        {
            InitializeComponent();
            img.Source = tab.ImgBmp;
            var test = SettingsFileStrings.GenerateSettingsString(tab);
            test += test;
            SettingsText.Text = test;
        }

        public void ClickOk(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
