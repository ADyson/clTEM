using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationGUI.Utils
{
    public enum CopyType { All, Base, TEM, STEM, CBED }

    /// <summary>
    /// Class designed to store all settings, mostly for convenience
    /// </summary>
    public class SimulationSettings
    {

        private readonly string[] ModeNames = { "TEM", "CBED", "STEM" };

        private readonly string[] TEMModeNames = { "Image", "Exit wave amplitde", "Exit wave phase", "Diffraction" };

        public SimulationSettings()
        {
            TEM = new TEMParams();
            CBED = new CBEDParams();
            STEM = new STEMParams();
        }

        public void CopyBase(SimulationSettings old)
        {
            FileName = old.FileName;
            SimArea = old.SimArea;
            UserSetArea = old.UserSetArea;
            SimMode = old.SimMode;

            Microscope = new MicroscopeParams(old.Microscope);

            SliceThickness = new fParam(old.SliceThickness.val);
            Integrals = new iParam(old.Integrals.val);
            IsFull3D = old.IsFull3D;
            IsFiniteDiff = old.IsFiniteDiff;
            Resolution = old.Resolution;
            PixelScale = old.PixelScale;
            Wavelength = old.Wavelength;
        }

        public SimulationSettings(SimulationSettings old, CopyType t)
        {
            CopyBase(old);
            if (t == CopyType.All)
            {
                TEM = new TEMParams(old.TEM);
                CBED = new CBEDParams(old.CBED);
                STEM = new STEMParams(old.STEM);
            }
            else if (t == CopyType.CBED)
                CBED = new CBEDParams(old.CBED);
            else if (t == CopyType.STEM)
                STEM = new STEMParams(old.STEM);
            else if (t == CopyType.TEM)
                TEM = new TEMParams(old.TEM);
        }

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

        public void UpdateImageParameters(SimulationSettings old)
        {
            TEM = new TEMParams(old.TEM);
            var temp = old.Microscope.kv.val;
            Microscope = new MicroscopeParams(old.Microscope);
            Microscope.kv.val = temp;
        }

        public string GetModeString()
        {
            string retVal;

            if (SimMode == 0)
            {
                retVal = ModeNames[SimMode] + " - " + TEMModeNames[TEMMode];
            }
            else
                retVal = ModeNames[SimMode];

            return retVal;
        }

        public string FileName;

        public SimulationArea SimArea = new SimulationArea { StartX = 0, EndX = 10, StartY = 0, EndY = 10};

        public bool UserSetArea = false;

        public int SimMode;

        public int TEMMode;

        public TEMParams TEM;

        public CBEDParams CBED;

        public STEMParams STEM;

        public MicroscopeParams Microscope;

        public fParam SliceThickness;

        public iParam Integrals;

        public bool IsFull3D;

        public bool IsFiniteDiff;

        public int Resolution;

        public float PixelScale;

        public float Wavelength;

    }

    public class TEMParams
    {
        public TEMParams()
        {
            Dose = new fParam();
        }

        public TEMParams(TEMParams old)
        {
            Dose = new fParam(old.Dose.val);
            Binning = old.Binning;
            CCD = old.CCD;
            CCDName = old.CCDName;
        }

        public TEMParams(MainWindow app)
        {
            Dose = new fParam();
            app.txtDose.DataContext = Dose;
            Dose.val = 10000;
        }

        public bool IsDoseUsed() { return CCD != 0; }

        public fParam Dose;

        public Int32 Binning;

        public int CCD = 0; // want way relate this to string?

        public string CCDName;
    }

    public class CBEDParams
    {
        public CBEDParams()
        {
            x = new fParam();
            y = new fParam();
            TDSRuns = new iParam();
        }

        public CBEDParams(CBEDParams old)
        {
            x = new fParam(old.x.val);
            y = new fParam(old.y.val);
            DoTDS = old.DoTDS;
            TDSRuns = new iParam(old.TDSRuns.val);
        }

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

    public class STEMParams
    {
        public STEMParams()
        {
            TDSRuns = new iParam();
            ConcurrentPixels = new iParam();
        }

        public STEMParams(STEMParams old)
        {
            ScanArea = old.ScanArea;
            UserSetArea = old.UserSetArea;
            Name = old.Name;
            Inner = old.Inner;
            Outer = old.Outer;
            x = old.x;
            y = old.y;
            DoTDS = old.DoTDS;
            TDSRuns = new iParam(old.TDSRuns.val);
            ConcurrentPixels = new iParam(old.ConcurrentPixels.val);
        }

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

        public string Name { get; set; }

        public float Inner { get; set; }

        public float Outer { get; set; }

        public float x { get; set; }

        public float y { get; set; }

        public bool DoTDS;

        public iParam TDSRuns;

        public iParam ConcurrentPixels;
    }
}