using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUTEMSTEMSimulation
{
    public class TEMParams
    {
        
        public float df;
        public float astigmag;
        public float astigang;
        public float kilovoltage;
        public float spherical;
        public float beta;
        public float delta;
        public float aperturemrad;

        public TEMParams()
        {
            df = 0;
            astigmag= 0;
            astigang = 0;
            kilovoltage = 0;
            spherical = 0;
            beta = 0;
            delta = 0;
            aperturemrad = 0;
        }
    }
}
