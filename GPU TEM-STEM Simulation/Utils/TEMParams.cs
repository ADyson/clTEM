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
        public float astig2mag;
        public float astig2ang;
        public float b2mag;
        public float b2ang;

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
            astig2mag = 0;
            astig2ang = 0;
            b2mag = 0;
            b2ang = 0;
        }
    }
}
