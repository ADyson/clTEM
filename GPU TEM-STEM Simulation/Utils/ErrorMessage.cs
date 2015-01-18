using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUTEMSTEMSimulation.Utils
{
    static class ErrorMessage
    {
        private static Dictionary<int, string> _errorCodes = new Dictionary<int, string>
        {
            {0, "No structure loaded."},
            {1, "No OpenCL device set."},
            {2, "No resolution set."},
            {3, "Voltage must be greater than 0."},
            {4, "Objective aperture must be greater than 0."},
            {5, "Slice thickness must be greater than 0."},
            {6, "Full 3D integrals must be greater than 0."},
            {7, "CBED coordinates must be within simulation bounds."},
            {8, "CBED TDS runs must be greater than 0."},
            {9, "Concurrent STEM pixels nust be greater than 0."},
            {10, "STEM TDS runs must be greater than 0."}
        };

        private static readonly List<int> _activeCodes = new List<int>();

        public static void AddCode(int code)
        {
            if (_activeCodes.Contains(code))
                return;

            _activeCodes.Add(code);
        }

        public static void RemoveCode(int code)
        {
            if (_activeCodes.Contains(code))
                return;

            _activeCodes.Remove(code);
        }

    }
}
