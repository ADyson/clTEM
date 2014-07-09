namespace GPUTEMSTEMSimulation
{
    public class STEMArea : SimArea
    {
        // public float xStart { get; set; }

        // public float xFinish { get; set; }

        // public float yStart { get; set; }

        // public float yFinish { get; set; }

        public int xPixels { get; set; }

        public int yPixels { get; set; }

        public float getxInterval
        {
            get { return (xFinish - xStart) / xPixels; } // maybe abs
        }

        public float getyInterval
        {
            get { return (yFinish - yStart) / yPixels; }
        }
    }

    public class SimArea
    {
        public float xStart { get; set; }

        public float xFinish { get; set; }

        public float yStart { get; set; }

        public float yFinish { get; set; }
    }
}