namespace SimulationGUI.Utils.Settings
{
    /// <summary>
    /// Holds the settings of the microscope
    /// Currently no error checking takes place here as it is assumed regexs will handle that
    /// </summary>
    public class MicroscopeSettings
    {
        public MicroscopeSettings(MicroscopeSettings old)
        {
            df = new FParam(old.df.Val);
            cs = new FParam(old.cs.Val);
            a1m = new FParam(old.a1m.Val);
            a1t = new FParam(old.a1t.Val);
            kv = new FParam(old.kv.Val);
            b = new FParam(old.b.Val);
            d = new FParam(old.d.Val);
            ap = new FParam(old.ap.Val);
            a2m = new FParam(old.a2m.Val);
            a2t = new FParam(old.a2t.Val);
            b2m = new FParam(old.b2m.Val);
            b2t = new FParam(old.b2t.Val);

        }

        ///// <summary>
        ///// Defocus (Å)
        ///// </summary>
        public FParam df;

        /// <summary>
        /// Spherical aberration (Å)
        /// </summary>
        public FParam cs;

        /// <summary>
        /// Two-fold astigmatism magnitude (Å)
        /// </summary>
        public FParam a1m;

        /// <summary>
        /// Two-fold astigmatism phase (°)
        /// </summary>
        public FParam a1t;

        /// <summary>
        /// Voltage (kV)
        /// </summary>
        public FParam kv;

        /// <summary>
        /// Convergence angle (mRad)
        /// </summary>
        public FParam b;

        /// <summary>
        /// Defocus spread (nm)
        /// </summary>
        public FParam d;

        /// <summary>
        /// Aperture (mRad)
        /// </summary>
        public FParam ap;

        /// <summary>
        /// Three-fold astigmatism magnitude (Å)
        /// </summary>
        public FParam a2m;

        /// <summary>
        /// Three-fold astigmatism phase (°)
        /// </summary>
        public FParam a2t;

        /// <summary>
        /// Coma magnitude (Å)
        /// </summary>
        public FParam b2m;

        /// <summary>
        /// Coma phase (°)
        /// </summary>
        public FParam b2t;

        /// <summary>
        /// Default constructor, sets the datacontext for textboxes on the microscope to this
        /// (so they auto update each other)
        /// </summary>
        public MicroscopeSettings(MainWindow app)
        {
            df = new FParam();
            cs = new FParam();
            a1m = new FParam();
            a1t = new FParam();
            kv = new FParam();
            b = new FParam();
            d = new FParam();
            ap = new FParam();
            a2m = new FParam();
            a2t = new FParam();
            b2m = new FParam();
            b2t = new FParam();
            app.txtMicroscopeDf.DataContext = df;
            app.txtMicroscopeCs.DataContext = cs;
            app.txtMicroscopeA1m.DataContext = a1m;
            app.txtMicroscopeA1t.DataContext = a2m;
            app.txtMicroscopeKv.DataContext = kv;
            app.txtMicroscopeB.DataContext = b;
            app.txtMicroscopeD.DataContext = d;
            app.txtMicroscopeAp.DataContext = ap;
            app.txtMicroscopeA2m.DataContext = a2m;
            app.txtMicroscopeA2t.DataContext = a2t;
            app.txtMicroscopeB2m.DataContext = b2m;
            app.txtMicroscopeB2t.DataContext = b2t;
        }

        public void SetDefaults()
        {
            df.Val = 0;
            cs.Val = 10000;
            a1m.Val = 0;
            a1t.Val = 0;
            kv.Val = 200;
            b.Val = 0.5f;
            d.Val = 3;
            ap.Val = 30;
            a2m.Val = 0;
            a2t.Val = 0;
            b2m.Val = 0;
            b2t.Val = 0;
        }

    }
}
