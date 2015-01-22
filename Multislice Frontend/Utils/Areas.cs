using System;
using System.Collections.Generic;
using SimulationGUI.Utils;

namespace SimulationGUI
{
    public class STEMArea : SimArea
    {
        // public float StartX { get; set; }

        // public float EndX { get; set; }

        // public float StartY { get; set; }

        // public float EndY { get; set; }

        public int xPixels { get; set; }

        public int yPixels { get; set; }

        public float getxInterval
        {
            get { return (EndX - StartX) / xPixels; } // maybe abs
        }

        public float getyInterval
        {
            get { return (EndY - StartY) / yPixels; }
        }
    }

    public class SimArea
    {
        public float StartX { get; set; }

        public float EndX { get; set; }

        public float StartY { get; set; }

        public float EndY { get; set; }
    }

    /// <summary>
    /// Used to pass Simulation area class between dialogs etc.
    /// </summary>
    public class SimAreaArgs : EventArgs
    {
        public SimAreaArgs(SimArea s)
        {
            AreaParams = s;
        }
        public SimArea AreaParams { get; private set; }
    }

    /// <summary>
    /// Used to pass STEM area class between dialogs etc.
    /// </summary>
    public class StemAreaArgs : EventArgs
    {
        public StemAreaArgs(STEMArea s)
        {
            AreaParams = s;
        }

        public STEMArea AreaParams { get; private set; }
    }

    /// <summary>
    /// Used to pass back detectors from dialogs
    /// </summary>
    public class DetectorArgs : EventArgs
    {
        public DetectorArgs(DetectorItem s)
        {
            Detector = s;
        }

        public DetectorItem Detector { get; private set; }

        public DetectorArgs(List<DetectorItem> sList)
        {
            DetectorList = sList;
        }

        public List<DetectorItem> DetectorList { get; private set; }
    }
}