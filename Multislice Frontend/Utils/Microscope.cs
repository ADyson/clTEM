using System.ComponentModel;

namespace SimulationGUI.Utils
{
    /// <summary>
    /// Holds the settings of the microscope
    /// Currently no error checking takes place here as it is assumed regexs will handle that
    /// </summary>
    public class Microscope
    {
        ///// <summary>
        ///// Defocus (Å)
        ///// </summary>
        public fParam df;

        /// <summary>
        /// Spherical aberration (Å)
        /// </summary>
        public fParam cs;

        /// <summary>
        /// Two-fold astigmatism magnitude (Å)
        /// </summary>
        public fParam a1m;

        /// <summary>
        /// Two-fold astigmatism phase (°)
        /// </summary>
        public fParam a1t;

        /// <summary>
        /// Voltage (kV)
        /// </summary>
        public fParam kv;

        /// <summary>
        /// Convergence angle (mRad)
        /// </summary>
        public fParam b;

        /// <summary>
        /// Defocus spread (nm)
        /// </summary>
        public fParam d;

        /// <summary>
        /// Aperture (mRad)
        /// </summary>
        public fParam ap;

        /// <summary>
        /// Three-fold astigmatism magnitude (Å)
        /// </summary>
        public fParam a2m;

        /// <summary>
        /// Three-fold astigmatism phase (°)
        /// </summary>
        public fParam a2t;

        /// <summary>
        /// Coma magnitude (Å)
        /// </summary>
        public fParam b2m;

        /// <summary>
        /// Coma phase (°)
        /// </summary>
        public fParam b2t;

        /// <summary>
        /// Default constructor, sets the datacontext for textboxes on the microscope to this
        /// (so they auto update each other)
        /// </summary>
        public Microscope(MainWindow app)
        {
            df = new fParam();
            cs = new fParam();
            a1m = new fParam();
            a1t = new fParam();
            kv = new fParam();
            b = new fParam();
            d = new fParam();
            ap = new fParam();
            a2m = new fParam();
            a2t = new fParam();
            b2m = new fParam();
            b2t = new fParam();
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
            df.val = 0;
            cs.val = 10000;
            a1m.val = 0;
            a1t.val = 0;
            kv.val = 200;
            b.val = 0.1f;
            d.val = 5;
            ap.val = 30;
            a2m.val = 0;
            a2t.val = 0;
            b2m.val = 0;
            b2t.val = 0;
        }

    }
}
