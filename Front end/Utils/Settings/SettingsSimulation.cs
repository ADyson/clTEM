using System;

namespace SimulationGUI.Utils.Settings
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

            Microscope = new MicroscopeSettings(old.Microscope);

            SliceThickness = new FParam(old.SliceThickness.Val);
            Integrals = new IParam(old.Integrals.Val);
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
            Microscope = new MicroscopeSettings(app);
            Microscope.SetDefaults();

            SliceThickness = new FParam();
            app.txtSliceThickness.DataContext = SliceThickness;

            Integrals = new IParam();
            app.txt3DIntegrals.DataContext = Integrals;

            CBED = new CBEDParams(app);

            TEM = new TEMParams(app);

            STEM = new STEMParams(app);

            SliceThickness.Val = 1;
            Integrals.Val = 20;
        }

        public void UpdateImageParameters(SimulationSettings old)
        {
            TEM = new TEMParams(old.TEM);
            var temp = old.Microscope.Voltage.Val;
            Microscope = new MicroscopeSettings(old.Microscope);
            Microscope.Voltage.Val = temp;
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

        public TEMParams TEM { get; set; }

        public CBEDParams CBED { get; set; }

        public STEMParams STEM { get; set; }

        public MicroscopeSettings Microscope;

        public FParam SliceThickness;

        public IParam Integrals;

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
            Dose = new FParam();
        }

        public TEMParams(TEMParams old)
        {
            Dose = new FParam(old.Dose.Val);
            Binning = old.Binning;
            CCD = old.CCD;
            CCDName = old.CCDName;
        }

        public TEMParams(MainWindow app)
        {
            Dose = new FParam();
            app.txtDose.DataContext = Dose;
            Dose.Val = 10000;
        }

        public bool IsDoseUsed() { return CCD != 0; }

        public FParam Dose;

        public Int32 Binning;

        public int CCD = 0; // want way relate this to string?

        public string CCDName;
    }

    public class CBEDParams
    {
        public CBEDParams()
        {
            x = new FParam();
            y = new FParam();
            TDSRuns = new IParam();
        }

        public CBEDParams(CBEDParams old)
        {
            x = new FParam(old.x.Val);
            y = new FParam(old.y.Val);
            DoTDS = old.DoTDS;
            TDSRuns = new IParam(old.TDSRuns.Val);
        }

        public CBEDParams(MainWindow app)
        {
            x = new FParam();
            app.txtCBEDx.DataContext = x;
            x.Val = 0;

            y = new FParam();
            app.txtCBEDy.DataContext = y;
            y.Val = 0;

            TDSRuns = new IParam();
            app.txtCBEDruns.DataContext = TDSRuns;
            TDSRuns.Val = 10;
        }

        public FParam x;

        public FParam y;

        public bool DoTDS;

        public IParam TDSRuns;
    }

    public class STEMParams
    {
        public STEMParams()
        {
            TDSRuns = new IParam();
            ConcurrentPixels = new IParam();
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
            TDSRuns = new IParam(old.TDSRuns.Val);
            ConcurrentPixels = new IParam(old.ConcurrentPixels.Val);
        }

        public STEMParams(MainWindow app)
        {
            TDSRuns = new IParam();
            app.txtSTEMruns.DataContext = TDSRuns;
            TDSRuns.Val = 10;

            ConcurrentPixels = new IParam();
            app.txtSTEMmulti.DataContext = ConcurrentPixels;
            ConcurrentPixels.Val = 10;
        }

        public STEMArea ScanArea = new STEMArea { StartX = 0, EndX = 1, StartY = 0, EndY = 1, xPixels = 1, yPixels = 1 };

        public bool UserSetArea = false;

        public string Name { get; set; }

        public float Inner { get; set; }

        public float Outer { get; set; }

        public float x { get; set; }

        public float y { get; set; }

        public bool DoTDS;

        public IParam TDSRuns;

        public IParam ConcurrentPixels;
    }
}