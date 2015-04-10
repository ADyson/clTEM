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
            CopySettings(old);
        }

        public void CopySettings(MicroscopeSettings old)
        {
            C10 = new FParam(old.C10.Val);
            C30 = new FParam(old.C30.Val);
            C12Mag = new FParam(old.C12Mag.Val);
            C12Ang = new FParam(old.C12Ang.Val);
            Voltage = new FParam(old.Voltage.Val);
            Alpha = new FParam(old.Alpha.Val);
            Delta = new FParam(old.Delta.Val);
            Aperture = new FParam(old.Aperture.Val);
            C23Mag = new FParam(old.C23Mag.Val);
            C23Ang = new FParam(old.C23Ang.Val);
            C21Mag = new FParam(old.C21Mag.Val);
            C21Ang = new FParam(old.C21Ang.Val);

            C32Mag = new FParam(old.C32Mag.Val);
            C32Ang = new FParam(old.C32Ang.Val);
            C34Mag = new FParam(old.C34Mag.Val);
            C34Ang = new FParam(old.C34Ang.Val);

            C41Mag = new FParam(old.C41Mag.Val);
            C41Ang = new FParam(old.C41Ang.Val);
            C43Mag = new FParam(old.C43Mag.Val);
            C43Ang = new FParam(old.C43Ang.Val);
            C45Mag = new FParam(old.C45Mag.Val);
            C45Ang = new FParam(old.C45Ang.Val);

            C50 = new FParam(old.C50.Val);
            C52Mag = new FParam(old.C52Mag.Val);
            C52Ang = new FParam(old.C52Ang.Val);
            C54Mag = new FParam(old.C54Mag.Val);
            C54Ang = new FParam(old.C54Ang.Val);
            C56Mag = new FParam(old.C56Mag.Val);
            C56Ang = new FParam(old.C56Ang.Val);

        }

        ///// <summary>
        ///// Defocus (Å)
        ///// </summary>
        public FParam C10;

        /// <summary>
        /// Spherical aberration (Å)
        /// </summary>
        public FParam C30;

        /// <summary>
        /// Two-fold astigmatism magnitude (Å)
        /// </summary>
        public FParam C12Mag;

        /// <summary>
        /// Two-fold astigmatism phase (°)
        /// </summary>
        public FParam C12Ang;

        /// <summary>
        /// Voltage (kV)
        /// </summary>
        public FParam Voltage;

        /// <summary>
        /// Convergence angle (mRad)
        /// </summary>
        public FParam Alpha;

        /// <summary>
        /// Defocus spread (nm)
        /// </summary>
        public FParam Delta;

        /// <summary>
        /// Aperture (mRad)
        /// </summary>
        public FParam Aperture;

        /// <summary>
        /// Three-fold astigmatism magnitude (Å)
        /// </summary>
        public FParam C23Mag;

        /// <summary>
        /// Three-fold astigmatism phase (°)
        /// </summary>
        public FParam C23Ang;

        /// <summary>
        /// Coma magnitude (Å)
        /// </summary>
        public FParam C21Mag;

        /// <summary>
        /// Coma phase (°)
        /// </summary>
        public FParam C21Ang;


        public FParam C32Mag;
        public FParam C32Ang;

        public FParam C34Mag;
        public FParam C34Ang;

        public FParam C41Mag;
        public FParam C41Ang;

        public FParam C43Mag;
        public FParam C43Ang;

        public FParam C45Mag;
        public FParam C45Ang;

        public FParam C50;

        public FParam C52Mag;
        public FParam C52Ang;

        public FParam C54Mag;
        public FParam C54Ang;

        public FParam C56Mag;
        public FParam C56Ang;

        /// <summary>
        /// Default constructor, sets the datacontext for textboxes on the microscope to this
        /// (so they auto update each other)
        /// </summary>
        public MicroscopeSettings(MainWindow app)
        {
            C10 = new FParam();
            C30 = new FParam();
            C12Mag = new FParam();
            C12Ang = new FParam();
            Voltage = new FParam();
            Alpha = new FParam();
            Delta = new FParam();
            Aperture = new FParam();
            C23Mag = new FParam();
            C23Ang = new FParam();
            C21Mag = new FParam();
            C21Ang = new FParam();

            C32Mag = new FParam();
            C32Ang = new FParam();

            C34Mag = new FParam();
            C34Ang = new FParam();

            C41Mag = new FParam();
            C41Ang = new FParam();

            C43Mag = new FParam();
            C43Ang = new FParam();

            C45Mag = new FParam();
            C45Ang = new FParam();

            C50 = new FParam();

            C52Mag = new FParam();
            C52Ang = new FParam();

            C54Mag = new FParam();
            C54Ang = new FParam();

            C56Mag = new FParam();
            C56Ang = new FParam();

            app.txtMicroscopeDf.DataContext = C10;
            app.txtMicroscopeCs.DataContext = C30;
            app.txtMicroscopeA1m.DataContext = C12Mag;
            app.txtMicroscopeA1t.DataContext = C12Ang;
            app.txtMicroscopeKv.DataContext = Voltage;
            app.txtMicroscopeB.DataContext = Alpha;
            app.txtMicroscopeD.DataContext = Delta;
            app.txtMicroscopeAp.DataContext = Aperture;
        }

        public MicroscopeSettings()
        {
            C10 = new FParam();
            C30 = new FParam();
            C12Mag = new FParam();
            C12Ang = new FParam();
            Voltage = new FParam();
            Alpha = new FParam();
            Delta = new FParam();
            Aperture = new FParam();
            C23Mag = new FParam();
            C23Ang = new FParam();
            C21Mag = new FParam();
            C21Ang = new FParam();

            C32Mag = new FParam();
            C32Ang = new FParam();

            C34Mag = new FParam();
            C34Ang = new FParam();

            C41Mag = new FParam();
            C41Ang = new FParam();

            C43Mag = new FParam();
            C43Ang = new FParam();

            C45Mag = new FParam();
            C45Ang = new FParam();

            C50 = new FParam();

            C52Mag = new FParam();
            C52Ang = new FParam();

            C54Mag = new FParam();
            C54Ang = new FParam();

            C56Mag = new FParam();
            C56Ang = new FParam();
        }

        public void SetDefaults()
        {
            C10.Val = 0;
            C30.Val = 10000;
            C12Mag.Val = 0;
            C12Ang.Val = 0;
            Voltage.Val = 200;
            Alpha.Val = 0.5f;
            Delta.Val = 3;
            Aperture.Val = 30;
            C23Mag.Val = 0;
            C23Ang.Val = 0;
            C21Mag.Val = 0;
            C21Ang.Val = 0;

            C32Mag.Val = 0;
            C32Ang.Val = 0;
            C34Mag.Val = 0;
            C34Ang.Val = 0;

            C41Mag.Val = 0;
            C41Ang.Val = 0;
            C43Mag.Val = 0;
            C43Ang.Val = 0;
            C45Mag.Val = 0;
            C45Ang.Val = 0;

            C50.Val = 0;
            C52Mag.Val = 0;
            C52Ang.Val = 0;
            C54Mag.Val = 0;
            C54Ang.Val = 0;
            C56Mag.Val = 0;
            C56Ang.Val = 0;


        }

    }
}
