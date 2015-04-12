using SimulationGUI.Controls;

namespace SimulationGUI.Utils.Settings
{
    public static class InfoTemplateStrings
    {
        //Verbatim string literals cannot be intented (the tabs get included in the string)

        static public string GenerateInfoString(DisplayTab tab)
        {
            if (tab.SimParams.SimArea == null)
                return ""; // might want other checks? this one is definitely used after a simulation?

            var general = UniversalSettings;
            general = general.Replace("{{filename}}", tab.SimParams.FileName); // save this beforehand somewhere and lock it?
            general = general.Replace("{{simareaxstart}}", tab.SimParams.SimArea.StartX.ToString());
            general = general.Replace("{{simareaxend}}", tab.SimParams.SimArea.EndX.ToString());
            general = general.Replace("{{simareaystart}}", tab.SimParams.SimArea.StartY.ToString());
            general = general.Replace("{{simareayend}}", tab.SimParams.SimArea.EndY.ToString());

            general = general.Replace("{{xpxscale}}", tab.PixelScaleX.ToString());
            general = general.Replace("{{ypxscale}}", tab.PixelScaleY.ToString());

            if(tab.Reciprocal)
                general = general.Replace("{{scaleunits}}", "1/Å");
            else
                general = general.Replace("{{scaleunits}}", "Å");

            general = general.Replace("{{resolution}}", tab.SimParams.Resolution.ToString());

            general = general.Replace("{{mode}}", tab.SimParams.GetModeString());

            general = general.Replace("{{full3d}}", tab.SimParams.IsFull3D.ToString());

            if (tab.SimParams.IsFull3D)
            {
                var full3DoptString = Full3dSettings;
                full3DoptString = full3DoptString.Replace("{{3dint}}", tab.SimParams.Integrals.Val.ToString());

                general = general.Replace("{{Full3Dopt}}", full3DoptString);
            }
            else
            {
                general = general.Replace("{{Full3Dopt}}", "");
            }

            general = general.Replace("{{fd}}", tab.SimParams.IsFiniteDiff.ToString());
            general = general.Replace("{{slicethickness}}", tab.SimParams.SliceThickness.Val.ToString());

            // Microscope

            general = general.Replace("{{volts}}", tab.SimParams.Microscope.Voltage.Val.ToString());

            var microscopeString = "";

            if (tab.SimParams.SimMode != 0 || (tab.SimParams.SimMode == 0 && tab.SimParams.TEMMode == 0))
            {
                microscopeString = MicroscopeSettings;

                microscopeString = microscopeString.Replace("{{aperture}}", tab.SimParams.Microscope.Aperture.Val.ToString());
                microscopeString = microscopeString.Replace("{{beta}}", tab.SimParams.Microscope.Alpha.Val.ToString());
                microscopeString = microscopeString.Replace("{{delta}}", tab.SimParams.Microscope.Delta.Val.ToString());

                microscopeString = microscopeString.Replace("{{C10}}", tab.SimParams.Microscope.C10.Val.ToString());
                microscopeString = microscopeString.Replace("{{C12m}}", tab.SimParams.Microscope.C12Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C12a}}", tab.SimParams.Microscope.C12Ang.Val.ToString());

                microscopeString = microscopeString.Replace("{{C21m}}", tab.SimParams.Microscope.C21Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C21a}}", tab.SimParams.Microscope.C21Ang.Val.ToString());
                microscopeString = microscopeString.Replace("{{C23m}}", tab.SimParams.Microscope.C23Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C23a}}", tab.SimParams.Microscope.C23Ang.Val.ToString());

                microscopeString = microscopeString.Replace("{{C30}}", tab.SimParams.Microscope.C30.Val.ToString());
                microscopeString = microscopeString.Replace("{{C32m}}", tab.SimParams.Microscope.C32Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C32a}}", tab.SimParams.Microscope.C32Ang.Val.ToString());
                microscopeString = microscopeString.Replace("{{C34m}}", tab.SimParams.Microscope.C34Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C34a}}", tab.SimParams.Microscope.C34Ang.Val.ToString());

                microscopeString = microscopeString.Replace("{{C41m}}", tab.SimParams.Microscope.C41Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C41a}}", tab.SimParams.Microscope.C41Ang.Val.ToString());
                microscopeString = microscopeString.Replace("{{C43m}}", tab.SimParams.Microscope.C43Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C43a}}", tab.SimParams.Microscope.C43Ang.Val.ToString());
                microscopeString = microscopeString.Replace("{{C45m}}", tab.SimParams.Microscope.C45Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C45a}}", tab.SimParams.Microscope.C45Ang.Val.ToString());

                microscopeString = microscopeString.Replace("{{C50}}", tab.SimParams.Microscope.C50.Val.ToString());
                microscopeString = microscopeString.Replace("{{C52m}}", tab.SimParams.Microscope.C52Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C52a}}", tab.SimParams.Microscope.C52Ang.Val.ToString());
                microscopeString = microscopeString.Replace("{{C54m}}", tab.SimParams.Microscope.C54Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C54a}}", tab.SimParams.Microscope.C54Ang.Val.ToString());
                microscopeString = microscopeString.Replace("{{C56m}}", tab.SimParams.Microscope.C56Mag.Val.ToString());
                microscopeString = microscopeString.Replace("{{C56a}}", tab.SimParams.Microscope.C56Ang.Val.ToString());
            }

            general = general.Replace("{{microscopesettings}}", microscopeString);

            // mode settings
            var modeString = "";

            if (tab.SimParams.SimMode == 0 && tab.SimParams.TEM != null && tab.SimParams.TEM.CCD != 0)
            {
                modeString = DoseSettings;

                modeString = modeString.Replace("{{dose}}", tab.SimParams.TEM.Dose.Val.ToString());
                modeString = modeString.Replace("{{ccd}}", tab.SimParams.TEM.CCDName);
                modeString = modeString.Replace("{{binning}}", tab.SimParams.TEM.Binning.ToString());
            }
            else switch (tab.SimParams.SimMode)
                {
                    case 1:
                        modeString = CBEDSettings;

                        modeString = modeString.Replace("{{cbedx}}", tab.SimParams.CBED.x.Val.ToString());
                        modeString = modeString.Replace("{{cbedy}}", tab.SimParams.CBED.y.Val.ToString());
                        modeString = modeString.Replace("{{cbedtds}}", tab.SimParams.CBED.DoTDS ? tab.SimParams.CBED.TDSRuns.Val.ToString() : "1");
                        break;
                    case 2:
                        modeString = STEMSettings;

                        modeString = modeString.Replace("{{areaxstart}}", tab.SimParams.STEM.ScanArea.StartX.ToString());
                        modeString = modeString.Replace("{{areaxend}}", tab.SimParams.STEM.ScanArea.EndX.ToString());
                        modeString = modeString.Replace("{{areaystart}}", tab.SimParams.STEM.ScanArea.StartY.ToString());
                        modeString = modeString.Replace("{{areayend}}", tab.SimParams.STEM.ScanArea.EndY.ToString());
                        modeString = modeString.Replace("{{xpixels}}", tab.SimParams.STEM.ScanArea.xPixels.ToString());
                        modeString = modeString.Replace("{{ypixels}}", tab.SimParams.STEM.ScanArea.yPixels.ToString());
                        
                        modeString = modeString.Replace("{{multistem}}", tab.SimParams.STEM.ConcurrentPixels.Val.ToString());
                        modeString = modeString.Replace("{{stemtds}}", tab.SimParams.STEM.DoTDS ? tab.SimParams.STEM.TDSRuns.Val.ToString() : "1");

                        var detInfoString = STEMDetectors;

                        detInfoString = detInfoString.Replace("{{detectorname}}", tab.SimParams.STEM.Name);
                        detInfoString = detInfoString.Replace("{{inner}}", tab.SimParams.STEM.Inner.ToString());
                        detInfoString = detInfoString.Replace("{{outer}}", tab.SimParams.STEM.Outer.ToString());
                        detInfoString = detInfoString.Replace("{{centx}}", tab.SimParams.STEM.x.ToString());
                        detInfoString = detInfoString.Replace("{{centy}}", tab.SimParams.STEM.y.ToString());

                        modeString = modeString.Replace("{{stemdetectors}}", detInfoString);
                        break;
                }

            return general.Replace("{{modesettings}}", modeString);
        }

        public static readonly string UniversalSettings =
@"Filename
  {{filename}}

Simulation Mode
  {{mode}}

Simulation Range (Å)
  x: {{simareaxstart}}, {{simareaxend}}
  y: {{simareaystart}}, {{simareayend}}

Pixel Scale ({{scaleunits}})
  x: {{xpxscale}}
  y: {{ypxscale}}

Simulation Resolution
  {{resolution}}

Full 3D
  {{full3d}}
{{Full3Dopt}}
Finite Difference 
  {{fd}}

Slice Thickness (Å)
  {{slicethickness}}
{{modesettings}}
Microscope Settings

  Voltage (kV)
    {{volts}}
{{microscopesettings}}"
;

public static readonly string MicroscopeSettings =
@"
  Objective Aperture (mrad)
    {{aperture}}

  Convergence Angle (mrad)
    {{beta}}

  Defocus Spread (nm)
    {{delta}}

  C10 (Å)
    {{C10}}

  C12 (Å, °)
    {{C12m}}, {{C12a}}

  C21 (Å, °)
    {{C21m}}, {{C21a}}

  C23 (Å, °)
    {{C23m}}, {{C23a}}

  C30 (Å)
    {{C30}}

  C32 (Å, °)
    {{C32m}}, {{C32a}}

  C34 (Å, °)
    {{C34m}}, {{C34a}}

  C41 (Å, °)
    {{C41m}}, {{C41a}}

  C43 (Å, °)
    {{C43m}}, {{C43a}}

  C45 (Å, °)
    {{C45m}}, {{C45a}}

  C50 (Å)
    {{C50}}

  C52 (Å, °)
    {{C52m}}, {{C52a}}

  C54 (Å, °)
    {{C54m}}, {{C54a}}

  C56 (Å, °)
    {{C56m}}, {{C56a}}
"
;

public static readonly string Full3dSettings =
@"
3D Integrals
  {{3dint}}
"
;

public static readonly string DoseSettings =
@"
Dose (e/Å²)
  {{dose}}

CCD
  {{ccd}}

Binning
  {{binning}}
"
;

        public static readonly string CBEDSettings =
@"
Probe Position (x, y) (Å)
  {{cbedx}}, {{cbedy}}

TDS Runs
  {{cbedtds}}
"
;

        public static readonly string STEMSettings =
@"
Scan Range (Å)
  x: {{areaxstart}}, {{areaxend}}
  y: {{areaystart}}, {{areayend}}

Scan Dimensions (px)
  x: {{xpixels}}
  y: {{ypixels}}

Concurrent Pixels
  {{multistem}}

TDS Runs
  {{stemtds}}

Detector
  {{stemdetectors}}
"
;

        public static readonly string STEMDetectors =
@"
  Name
    {{detectorname}}

  Inner Radius (mrad)
    {{inner}}

  Outer Radius (mrad)
    {{outer}}

  Center (x, y) (Å)
    {{centx}}, {{centy}}
"
;
    }
}

