using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using BitMiracle.LibTiff.Classic;
using SimulationGUI.Controls;
using SimulationGUI.Utils.Settings;

namespace SimulationGUI.Dialogs
{
    /// <summary>
    /// Interaction logic for InfoSaveDialog.xaml
    /// </summary>
    public partial class InfoSaveDialog
    {
        private DisplayTab tab;

        public InfoSaveDialog(DisplayTab inTab)
        {
            InitializeComponent();
            tab = inTab;
            img.Source = tab.ImgBmp;
            SettingsText.Text = InfoTemplateStrings.GenerateInfoString(tab);
        }

        public void ClickOk(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveSimulationInfo(string filename)
        {
            var general = InfoTemplateStrings.GenerateInfoString(tab);

            File.WriteAllText(filename, general);
        }

        private void SaveImage(string filename)
        {
            // for setings stuff (test)
            var extension = Path.GetExtension(filename);
            if (extension == null)
                return;
            //var result = filename.Substring(0, filename.Length - extension.Length);
            //result = result + ".txt";

            //SaveSimulationInfo(dt, result);


            if (filename.EndsWith(".tiff"))
            {
                using (var output = Tiff.Open(filename, "w"))
                {
                    output.SetField(TiffTag.IMAGEWIDTH, tab.xDim);
                    output.SetField(TiffTag.IMAGELENGTH, tab.yDim);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    output.SetField(TiffTag.SAMPLEFORMAT, 3);
                    output.SetField(TiffTag.BITSPERSAMPLE, 32);
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                    output.SetField(TiffTag.ROWSPERSTRIP, tab.yDim);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                    for (var i = 0; i < tab.yDim; ++i)
                    {
                        var buf = new float[tab.xDim];
                        var buf2 = new byte[4 * tab.xDim];

                        for (var j = 0; j < tab.yDim; ++j)
                        {
                            buf[j] = tab.ImageData[j + tab.xDim * i];
                        }

                        Buffer.BlockCopy(buf, 0, buf2, 0, buf2.Length);
                        output.WriteScanline(buf2, i);
                    }
                }
            }
            else if (filename.EndsWith(".png"))
            {
                using (var stream = new FileStream(filename, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(tab.ImgBmp.Clone()));
                    encoder.Save(stream);
                    stream.Close();
                }
            }
            else if (filename.EndsWith(".jpeg"))
            {
                using (var stream = new FileStream(filename, FileMode.Create))
                {
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(tab.ImgBmp.Clone()));
                    encoder.Save(stream);
                    stream.Close();
                }
            }
        }

        private void SaveImageButton(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Image",
                DefaultExt = ".tiff",
                Filter = "TIFF (*.tiff)|*.tiff|PNG (*.png)|*.png|JPEG (*.jpeg)|*.jpeg"
            };

            var result = saveDialog.ShowDialog();

            if (result == false) return;

            var filename = saveDialog.FileName;

            SaveImage(filename);
        }

        private void SaveSettingsBtton(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Simulation Information",
                DefaultExt = ".txt",
                Filter = "Text (*.txt)|*.txt"
            };

            var result = saveDialog.ShowDialog();

            if (result == false) return;

            var filename = saveDialog.FileName;

            SaveSimulationInfo(filename);
        }

        private void SaveBothButton(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Image With Info",
                DefaultExt = ".tiff",
                Filter = "TIFF (*.tiff)|*.tiff|PNG (*.png)|*.png|JPEG (*.jpeg)|*.jpeg"
            };

            var result = saveDialog.ShowDialog();

            if (result == false) return;

            var filename = saveDialog.FileName;

            // for setings stuff 
            var extension = Path.GetExtension(filename);
            if (extension == null)
                return;
            var filenametxt = filename.Substring(0, filename.Length - extension.Length);
            filenametxt = filenametxt + ".txt";

            SaveImage(filename);
            SaveSimulationInfo(filenametxt);
        }
    }
}
