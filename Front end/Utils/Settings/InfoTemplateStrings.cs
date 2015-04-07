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

            general = general.Replace("{{volts}}", tab.SimParams.Microscope.kv.Val.ToString());

            var microscopeString = "";

            if (tab.SimParams.SimMode != 0 || (tab.SimParams.SimMode == 0 && tab.SimParams.TEMMode == 0))
            {
                microscopeString = MicroscopeSettings;

                microscopeString = microscopeString.Replace("{{aperture}}", tab.SimParams.Microscope.ap.Val.ToString());
                microscopeString = microscopeString.Replace("{{beta}}", tab.SimParams.Microscope.b.Val.ToString());
                microscopeString = microscopeString.Replace("{{delta}}", tab.SimParams.Microscope.d.Val.ToString());
                microscopeString = microscopeString.Replace("{{defocus}}", tab.SimParams.Microscope.df.Val.ToString());
                microscopeString = microscopeString.Replace("{{cs}}", tab.SimParams.Microscope.cs.Val.ToString());
                microscopeString = microscopeString.Replace("{{A1m}}", tab.SimParams.Microscope.a1m.Val.ToString());
                microscopeString = microscopeString.Replace("{{A1t}}", tab.SimParams.Microscope.a1t.Val.ToString());
                microscopeString = microscopeString.Replace("{{A2m}}", tab.SimParams.Microscope.a2m.Val.ToString());
                microscopeString = microscopeString.Replace("{{A2t}}", tab.SimParams.Microscope.a2t.Val.ToString());
                microscopeString = microscopeString.Replace("{{B2m}}", tab.SimParams.Microscope.b2m.Val.ToString());
                microscopeString = microscopeString.Replace("{{B2t}}", tab.SimParams.Microscope.b2t.Val.ToString());
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

Simulation Range (UNITS)
  x: {{simareaxstart}} - {{simareaxend}}
  y: {{simareaystart}} - {{simareayend}}

Resolution
  {{resolution}}

Full 3D
  {{full3d}}
{{Full3Dopt}}
Finite Difference 
  {{fd}}

Slice Thickness (UNITS)
  {{slicethickness}}
{{modesettings}}
Microscope Settings

  Voltage (kV)
    {{volts}}
{{microscopesettings}}"
;

public static readonly string MicroscopeSettings =
@"
  Objective Aperture (UNITS)
    {{aperture}}

  Convergence Angle (UNITS)
    {{beta}}

  Defocus Spread (UNITS)
    {{delta}}

  Defocus ***
    {{defocus}}

  Cs ***
    {{cs}}

  A1 (magnitude, angle) (UNITS)
    {{A1m}}, {{A1t}}

  A2 (magnitude, angle) (UNITS)
    {{A2m}}, {{A2t}}

  B2 (magnitude, angle) (UNITS)
    {{B2m}}, {{B2t}}
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
Dose (UNITS)
  {{dose}}

CCD
  {{ccd}}

Binning
  {{binning}}
"
;

        public static readonly string CBEDSettings =
@"
Probe Position (x, y) (UNITS)
  {{cbedx}}, {{cbedy}}

TDS Runs
  {{cbedtds}}
"
;

        public static readonly string STEMSettings =
@"
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

  Inner Radius (UNITS)
    {{inner}}

  Outer Radius (UNITS)
    {{outer}}

  Center (x, y) (UNITS)
    {{centx}}, {{centy}}
"
;
    }
}

