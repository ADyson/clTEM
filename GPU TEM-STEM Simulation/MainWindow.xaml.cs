using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Win32;
using ManagedOpenCLWrapper;
using BitMiracle.LibTiff.Classic;

namespace GPUTEMSTEMSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsResolutionSet;
        bool HaveStructure;
        bool IsSorted;
        int Resolution;

        TEMParams ImagingParameters;
        TEMParams ProbeParameters;

        /// <summary>
        /// Cancel event to halt calculation.
        /// </summary>
        public event EventHandler Cancel = delegate { };

        /// <summary>
        /// Worker to perform calculations in Non UI Thread.
        /// </summary>
        BackgroundWorker SimWorker;
        BackgroundWorker AtomSortWorker;
        ManagedOpenCL mCL;

        private static WriteableBitmap CTEMImg;
        public static WriteableBitmap _CTEMImg
        {
            get { return MainWindow.CTEMImg; }
            set { MainWindow.CTEMImg = value; }
        }

        private static WriteableBitmap EWImg;
        public static WriteableBitmap _EWImg
        {
            get { return MainWindow.EWImg; }
            set { MainWindow.EWImg = value; }
        }


        private static WriteableBitmap DiffImg;
        public static WriteableBitmap _DiffImg
        {
            get { return MainWindow.DiffImg; }
            set { MainWindow.DiffImg = value; }
        }

        private static WriteableBitmap STEMADFImg;
        public static WriteableBitmap _STEMADFImg
        {
            get { return MainWindow.STEMADFImg; }
            set { MainWindow.STEMADFImg = value; }
        }

        private static WriteableBitmap STEMBFImg;
        public static WriteableBitmap _STEMBFImg
        {
            get { return MainWindow.STEMBFImg; }
            set { MainWindow.STEMBFImg = value; }
        }

        private static WriteableBitmap STEMHAADFImg;
        public static WriteableBitmap _STEMHAADFImg
        {
            get { return MainWindow.STEMHAADFImg; }
            set { MainWindow.STEMHAADFImg = value; }
        }


        // Arrays to store image data
        float[] CTEMImage;
        float[] EWImage;
        float[] DiffImage;




        ///<summary>
        /// hides some other PreviewTextInput with new?
        /// </summary>
        new private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowedFloatNumber(e.Text);
        }

        private static bool IsTextAllowedFloatNumber(string text)
        {
           //Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
           Regex regex = new Regex(@"^[-]?(0|[1-9][0-9]*)?(\.[0-9]*)?([eE][+-]?[0-9]+)?$");
           //return !regex.IsMatch(text);
           return regex.IsMatch(text);
            
        }


        public MainWindow()
        {
            InitializeComponent();
            // Start in TEM mode.
            TEMRadioButton.IsChecked = true;

            ImagingParameters = new TEMParams();
            ProbeParameters = new TEMParams();

            // Setup Managed Wrapper and Upload Atom Parameterisation ready for Multislice calculations.
            // Moved parameterisation, will be redone each time we get new structure now :(
            mCL = new ManagedOpenCL();
            
            IsResolutionSet = false;
            HaveStructure = false;
            IsSorted = false;
        }

        private void ImportStructureButton(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();

               // Set defaults for file dialog.
            openDialog.FileName = "file name";                  // Default file name
            openDialog.DefaultExt = ".xyz";                     // Default file extension
            openDialog.Filter = "XYZ Coordinates (.xyz)|*.xyz"; // Filter files by extension

            Nullable<bool> result = openDialog.ShowDialog();

            if (result == true)
            {
                fileNameLabel.Content = openDialog.FileName;
                
                // Now pass filename through to unmanaged where atoms can be imported inside structure class...
                mCL.ImportStructure(openDialog.FileName);
                mCL.UploadParameterisation();

                // Update some dialogs if everything went OK.
                Int32 Len = 0;
                float MinX = 0;
                float MinY = 0;
                float MinZ = 0;
                float MaxX = 0;
                float MaxY = 0;
                float MaxZ = 0;

                mCL.GetStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

                HaveStructure = true;

                WidthLabel.Content = "Width (A): " + (MaxX - MinX).ToString();
                HeightLabel.Content = "Height (A): " + (MaxY - MinY).ToString();
                DepthLabel.Content = "Depth (A): " + (MaxZ - MinZ).ToString();
                atomNumberLabel.Content = Len.ToString() + " Atoms";

                if (IsResolutionSet)
                {
                    float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
                    PixelScaleLabel.Content = "Pixel Size (A): " + (BiggestSize / Resolution).ToString();
                }

                // Now we want to sorting the atoms ready for the simulation process do this in a background worker...

                Cancel += CancelProcess;
                //progressBar1.Minimum = 0;
               // progressBar1.Maximum = 100;

                System.Windows.Threading.Dispatcher mwDispatcher = this.Dispatcher;
                AtomSortWorker = new BackgroundWorker();

                // Changed to alternate model of progress reporting
                //worker.WorkerReportsProgress = true;
                AtomSortWorker.WorkerSupportsCancellation = true;

                

                AtomSortWorker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    // This is where we start sorting the atoms in the background ready to be processed later...
                    mCL.SortStructure();
                    //System.Threading.Thread.Sleep(2);
                    //UpdateProgressDelegate update = new UpdateProgressDelegate(UpdateProgress);
                    //Dispatcher.BeginInvoke(update, i, managedMultislice.GetSlices(conventional));
                };

                // This runs on UI Thread so can access UI, probably better way of doing image though.
                AtomSortWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    IsSorted = true;
                };

                AtomSortWorker.RunWorkerAsync();

            }
        }
        
        public delegate void UpdateProgressDelegate(int iteration, int maxIts);

        public void UpdateProgress(int iteration, int maxIts)
        {

        }

        void CancelProcess(object sender, EventArgs e)
        {
            AtomSortWorker.CancelAsync();
        }


        private void ImportUnitCellButton(object sender, RoutedEventArgs e)
        {
            // No idea what to do here just yet, will just have to programatically make potentials based on unit cell and number of unit cells in each direction.
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            Resolution = Convert.ToInt32(ResolutionCombo.SelectedValue.ToString());

            IsResolutionSet = true;

            if (HaveStructure)
            {
                // Update some dialogs if everything went OK.
                Int32 Len = 0;
                float MinX = 0;
                float MinY = 0;
                float MinZ = 0;
                float MaxX = 0;
                float MaxY = 0;
                float MaxZ = 0;

                mCL.GetStructureDetails(ref Len, ref MinX, ref MinY, ref MinZ, ref MaxX, ref MaxY, ref MaxZ);

                HaveStructure = true;

                WidthLabel.Content = "Width (A): " + (MaxX - MinX).ToString();
                HeightLabel.Content = "Height (A): " + (MaxY - MinY).ToString();
                DepthLabel.Content = "Depth (A): " + (MaxZ - MinZ).ToString();
                atomNumberLabel.Content = Len.ToString() + " Atoms";

                float BiggestSize = Math.Max(MaxX - MinX, MaxY - MinY);
                PixelScaleLabel.Content ="Pixel Size (A): "+ (BiggestSize / Resolution).ToString();
            }
        }

        // Simulation Button
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Check We Have Structure
            if (HaveStructure == false)
            {
                var result = MessageBox.Show("No Structure Loaded", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Check parameters are set
            if (IsResolutionSet == false)
            {
                var result = MessageBox.Show("Resolution Not Set", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Do Simulation in a background worker
            Cancel += CancelProcess;
            //progressBar1.Minimum = 0;
            // progressBar1.Maximum = 100;

            System.Windows.Threading.Dispatcher mwDispatcher = this.Dispatcher;
            SimWorker = new BackgroundWorker();

            // Changed to alternate model of progress reporting
            //worker.WorkerReportsProgress = true;
            SimWorker.WorkerSupportsCancellation = true;



            SimWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                // Upload Simulation Parameters to c++ class
                mCL.SetTemParams(ImagingParameters.df, ImagingParameters.astigmag, ImagingParameters.astigang, ImagingParameters.kilovoltage, ImagingParameters.spherical,
                                    ImagingParameters.beta, ImagingParameters.delta, ImagingParameters.aperturemrad);

                mCL.SetStemParams(ProbeParameters.df, ProbeParameters.astigmag, ProbeParameters.astigang, ProbeParameters.kilovoltage, ProbeParameters.spherical,
                                    ProbeParameters.beta, ProbeParameters.delta, ProbeParameters.aperturemrad);

                // Will call different functions depending on type of simulation required, or just send flags to allow subsections to be performed differently
           
                
                
                // Setup pre simulation (make frequencies and create databuffers and kernels
                mCL.InitialiseSimulation(Resolution);

                // Use Background worker to progress through each step
                int NumberOfSlices = 0;
                mCL.GetNumberSlices(ref NumberOfSlices);
                // Seperate into setup, loop over slices and final steps to allow for progress reporting.

                for (int i = 1; i <= NumberOfSlices; i++)
                {
                    mCL.MultisliceStep(i, NumberOfSlices);

                }
                // Cleanup

                //System.Threading.Thread.Sleep(2);
                //UpdateProgressDelegate update = new UpdateProgressDelegate(UpdateProgress);
                //Dispatcher.BeginInvoke(update, i, managedMultislice.GetSlices(conventional));
            };

            // This runs on UI Thread so can access UI, probably better way of doing image though.
            SimWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                _EWImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                EWImageDisplay.Source = _EWImg;

                // When its completed we want to get data to c# for displaying in an image...
                EWImage = new float[Resolution * Resolution];
                mCL.GetCTEMImage(EWImage, Resolution);

                // Calculate the number of bytes per pixel (should be 4 for this format). 
                var bytesPerPixel = (_EWImg.Format.BitsPerPixel + 7) / 8;

                // Stride is bytes per pixel times the number of pixels.
                // Stride is the byte width of a single rectangle row.
                var stride = _EWImg.PixelWidth * bytesPerPixel;

                // Create a byte array for a the entire size of bitmap.
                var arraySize = stride * _EWImg.PixelHeight;
                var pixelArray = new byte[arraySize];

                float min = mCL.GetIMMin();
                float max = mCL.GetIMMax();

                for (int row = 0; row < _EWImg.PixelHeight; row++)
                    for (int col = 0; col < _EWImg.PixelWidth; col++)
                    {
                        pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 0] = Convert.ToByte(Math.Ceiling(((EWImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                        pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 1] = Convert.ToByte(Math.Ceiling(((EWImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                        pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 2] = Convert.ToByte(Math.Ceiling(((EWImage[col + row * Resolution] - min) / (max - min)) * 254.0f));
                        pixelArray[(row * _EWImg.PixelWidth + col) * bytesPerPixel + 3] = 0;
                    }


                Int32Rect rect = new Int32Rect(0, 0, _EWImg.PixelWidth, _EWImg.PixelHeight);

                _EWImg.WritePixels(rect, pixelArray, stride, 0);


                _DiffImg = new WriteableBitmap(Resolution, Resolution, 96, 96, PixelFormats.Bgr32, null);
                DiffImageDisplay.Source = _DiffImg;

                // When its completed we want to get data to c# for displaying in an image...
                DiffImage = new float[Resolution * Resolution];
                mCL.GetDiffImage(DiffImage, Resolution);

                // Calculate the number of bytes per pixel (should be 4 for this format). 
                var bytesPerPixel2 = (_DiffImg.Format.BitsPerPixel + 7) / 8;

                // Stride is bytes per pixel times the number of pixels.
                // Stride is the byte width of a single rectangle row.
                var stride2 = _DiffImg.PixelWidth * bytesPerPixel;

                // Create a byte array for a the entire size of bitmap.
                var arraySize2 = stride2 * _DiffImg.PixelHeight;
                var pixelArray2 = new byte[arraySize2];

                float min2 = mCL.GetDiffMin();
                float max2 = mCL.GetDiffMax();

                for (int row = 0; row < _DiffImg.PixelHeight; row++)
                    for (int col = 0; col < _DiffImg.PixelWidth; col++)
                    {
                        pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 0] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                        pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 1] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                        pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 2] = Convert.ToByte(Math.Ceiling(((DiffImage[col + row * Resolution] - min2) / (max2 - min2)) * 254.0f));
                        pixelArray2[(row * _DiffImg.PixelWidth + col) * bytesPerPixel2 + 3] = 0;
                    }


                Int32Rect rect2 = new Int32Rect(0, 0, _DiffImg.PixelWidth, _DiffImg.PixelHeight);

                _DiffImg.WritePixels(rect2, pixelArray2, stride2, 0);

                SaveImageButton.IsEnabled = true;
            };

            SimWorker.RunWorkerAsync();

           
        }

        private void ImagingDf_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingDf.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.df);
        }

        private void ImagingCs_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingCs.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.spherical);
        }

        private void ImagingA2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingA2.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astigmag);
        }

        private void ImagingA2theta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingA2theta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.astigang);
        }

        private void ImagingkV_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingkV.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.kilovoltage);
        }

        private void Imagingbeta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Imagingbeta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.beta);
        }

        private void Imagingdelta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Imagingdelta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.delta);
        }

        private void ImagingAperture_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ImagingAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ImagingParameters.aperturemrad);
        }

        private void ProbeDf_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ProbeDf.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.df);
        }

        private void ProbeCs_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ProbeCs.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.spherical);
        }

        private void ProbeA2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ProbeA2.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.astigmag);
        }

        private void ProbeA2theta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ProbeA2theta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.astigang);
        }

        private void ProbekV_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ProbekV.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.kilovoltage);
        }

        private void Probebeta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Probebeta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.beta);
        }

        private void Probedelta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = Probedelta.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.delta);
        }

        private void ProbeAperture_TextChanged(object sender, TextChangedEventArgs e)
        {
            string temporarytext = ProbeAperture.Text;
            float.TryParse(temporarytext, NumberStyles.Float, null, out ProbeParameters.aperturemrad);
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Ideally want to check tab and use information to save either EW or CTEM....

              // File saving dialog
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
            
            saveDialog.Title = "Save Output Image";
            saveDialog.DefaultExt = ".tiff";                     // Default file extension
            saveDialog.Filter = "TIFF Image (.tiff)|*.tiff"; // Filter files by extension

            Nullable<bool> result = saveDialog.ShowDialog();

            if (result == true)
            {
                string filename = saveDialog.FileName;
                using (Tiff output = Tiff.Open(filename, "w"))
                {
                    output.SetField(TiffTag.IMAGEWIDTH, Resolution);
                    output.SetField(TiffTag.IMAGELENGTH, Resolution);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    output.SetField(TiffTag.SAMPLEFORMAT, 3);
                    output.SetField(TiffTag.BITSPERSAMPLE, 32);
                    output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
                    output.SetField(TiffTag.ROWSPERSTRIP, Resolution);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                    for (int i = 0; i < Resolution; ++i)
                    {
                        float[] buf = new float[Resolution];
                        byte[] buf2 = new byte[4 * Resolution];
                        for (int j = 0; j < Resolution; ++j)
                        {
                              buf[j] = EWImage[j+Resolution*i];
                        }
                        Buffer.BlockCopy(buf, 0, buf2, 0, buf2.Length);
                        output.WriteScanline(buf2, i);
                    }
                }
            }

        }
    }
}
