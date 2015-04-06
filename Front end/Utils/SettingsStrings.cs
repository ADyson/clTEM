namespace SimulationGUI.Utils
{
    public static class SettingsStrings
    {
        //Verbatim string literals cannot be intented (the tabs get included in the string)

        public static readonly string UniversalSettings =
@"*** Filename ***
{{filename}}

*** Simulation Area ***
x: {{simareaxstart}} -> {{simareaxend}}
y: {{simareaystart}} -> {{simareayend}}

*** Resolution ***
{{resolution}}

*** Simulation Mode ***
{{mode}}

*** Full 3D ***
{{full3d}}

*** Finite Difference ***
{{fd}}
{{Full3Dopt}}
*** Slice Thickness ***
{{slicethickness}}

*** Microscope Settings ***
*** Voltage (kV)***
{{volts}}
{{microscopesettings}}
{{modesettings}}"
;

public static readonly string MicroscopeSettings =
@"
*** Objective Aperture ***
{{aperture}}

*** Convergence Angle ***
{{beta}}

*** Defocus Spread ***
{{delta}}

*** Defocus ***
{{defocus}}

*** Cs ***
{{cs}}

*** A1 (magnitude, angle)***
{{A1m}}, {{A1t}}

*** A2 (magnitude, angle)***
{{A2m}}, {{A2t}}

*** B2 (magnitude, angle)***
{{B2m}}, {{B2t}}
"
;

public static readonly string Full3dSettings =
@"
*** 3D Integrals ***
{{3dint}}
"
;

public static readonly string DoseSettings =
@"
*** Dose ***
{{dose}}

*** CCD ***
{{ccd}}

*** Binning ***
{{binning}}
"
;

        public static readonly string CBEDSettings =
@"
*** CBED Position (x, y)***
{{cbedx}}, {{cbedy}}

*** CBED TDS Runs ***
{{cbedtds}}
"
;

        public static readonly string STEMSettings =
@"
*** Concurrent Pixels ***
{{multistem}}

*** STEM TDS Runs ***
{{stemtds}}

*** Detectors ***
{{stemdetectors}}
"
;

        public static readonly string STEMDetectors =
@"
*** Name ***
{{detectorname}}

*** Inner Radius ***
{{inner}}

*** Outer Radius ***
{{outer}}

*** Center (x, y)***
{{centx}}, {{centy}}
"
;
    }
}

