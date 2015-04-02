using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationGUI.Utils
{
    /// <summary>
    /// Class designed to store all settings, mostly for convenience
    /// </summary>
    class SimulationSettings
    {

        private readonly string[] ModeNames = { "TEM", "CBED", "STEM" };

        public SimulationSettings() { }

        public void UpdateWindow(MainWindow app)
        {
            Microscope = new MicroscopeParams(app);
            Microscope.SetDefaults();

            SliceThickness = new fParam();
            app.txtSliceThickness.DataContext = SliceThickness;

            Integrals = new iParam();
            app.txt3DIntegrals.DataContext = Integrals;

            CBED = new CBEDParams(app);

            TEM = new TEMParams(app);

            STEM = new STEMParams(app);
           
            SliceThickness.val = 1;
            Integrals.val = 20;
        }

        public string FileName;

        public SimulationArea SimArea = new SimulationArea { StartX = 0, EndX = 10, StartY = 0, EndY = 10};

        public bool UserSetArea = false;

        public int SimMode;

        public TEMParams TEM;

        public CBEDParams CBED;

        public STEMParams STEM;

        public MicroscopeParams Microscope;

        public float ImageVoltage;

        public fParam SliceThickness;

        public iParam Integrals;

        public bool IsFull3D;

        public bool IsFiniteDiff;

        public int Resolution;

        public float PixelScale;

        public float Wavelength;

    }

    class TEMParams
    {
        public TEMParams(MainWindow app)
        {
            Dose = new fParam();
            app.txtDose.DataContext = Dose;
            Dose.val = 10000;
        }

        public fParam Dose;

        public Int32 Binning;

        public int CCD;
    }

    class CBEDParams
    {
        public CBEDParams(MainWindow app)
        {
            x = new fParam();
            app.txtCBEDx.DataContext = x;
            x.val = 0;

            y = new fParam();
            app.txtCBEDy.DataContext = y;
            y.val = 0;

            TDSRuns = new iParam();
            app.txtCBEDruns.DataContext = TDSRuns;
            TDSRuns.val = 10;
        }

        public fParam x;

        public fParam y;

        public bool DoTDS;

        public iParam TDSRuns;
    }

    class STEMParams
    {
        public STEMParams(MainWindow app)
        {
            TDSRuns = new iParam();
            app.txtSTEMruns.DataContext = TDSRuns;
            TDSRuns.val = 10;

            ConcurrentPixels = new iParam();
            app.txtSTEMmulti.DataContext = ConcurrentPixels;
            ConcurrentPixels.val = 10;
        }

        public STEMArea ScanArea = new STEMArea { StartX = 0, EndX = 1, StartY = 0, EndY = 1, xPixels = 1, yPixels = 1 };

        public bool UserSetArea = false;

        public List<DetectorItem> Detectors = new List<DetectorItem>();

        public bool DoTDS;

        public iParam TDSRuns;

        public iParam ConcurrentPixels;
    }
}