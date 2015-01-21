using System.Collections.Generic;
using System.Linq;

namespace SimulationGUI.Utils
{
    static class ErrorMessage
    {
        private static readonly Dictionary<int, string> ErrorCodes = new Dictionary<int, string>
        {
            // General codes
            {0, "No structure loaded."},
            {1, "No OpenCL device set."},
            {2, "No resolution set."},
            {3, "Voltage must be greater than 0."},
            // Image and general
            {10, "Objective aperture should be greater than 0."},
            // are these only used if finite difference is set?
            {5, "Slice thickness must be greater than 0."},
            {6, "Full 3D integrals must be greater than 0."},
            // CTEM
            // N/A
            // CBED
            {30, "CBED coordinates must be within simulation bounds."},
            {31, "CBED TDS runs must be greater than 0."},
            // STEM
            {40, "Concurrent STEM pixels nust be greater than 0."},
            {41, "STEM TDS runs must be greater than 0."},
            {42, "No detectors have been set."},
            // Image
            {50, "Dose should be greater than 0."}
        };

        private static bool _haveError = true;

        private static readonly List<int> ActiveCodes = new List<int>(new[] {0, 1, 2});

        public static void AddCode(int code)
        {
            if (ActiveCodes.Contains(code))
                return;

            ActiveCodes.Add(code);
            _haveError = true;
        }

        public static void RemoveCode(int code)
        {
            if (!ActiveCodes.Contains(code))
                return;

            ActiveCodes.Remove(code);
            if (ActiveCodes.Count == 0)
                _haveError = false;
        }

        public static void ToggleCode(int code, bool good)
        {
            if(good)
                RemoveCode(code);
            else
                AddCode(code);
        }

        public static List<string> GetCTEMCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 0) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 20) && (item <= 29)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

        public static List<string> GetCBEDCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 0) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 30) && (item <= 39)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

        public static List<string> GetSTEMCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 0) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 40) && (item <= 49)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

        public static List<string> GetImageCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 10) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 50) && (item <= 59)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

    }

    static class WarningMessage
    {
        private static readonly Dictionary<int, string> ErrorCodes = new Dictionary<int, string>
        {
            // General codes
            {0, "No structure loaded."},
            {1, "No OpenCL device set."},
            {2, "No resolution set."},
            {3, "Voltage must be greater than 0."},
            // Image and general
            {10, "Objective aperture should be greater than 0."},
            // are these only used if finite difference is set?
            {5, "Slice thickness must be greater than 0."},
            {6, "Full 3D integrals must be greater than 0."},
            // CTEM
            // N/A
            // CBED
            {30, "CBED coordinates must be within simulation bounds."},
            {31, "CBED TDS runs must be greater than 0."},
            // STEM
            {40, "Concurrent STEM pixels nust be greater than 0."},
            {41, "STEM TDS runs must be greater than 0."},
            {42, "No detectors have been set."},
            // Image
            {50, "Dose should be greater than 0."}
        };

        private static bool _haveError = false;

        private static readonly List<int> ActiveCodes = new List<int>();

        public static void AddCode(int code)
        {
            if (ActiveCodes.Contains(code))
                return;

            ActiveCodes.Add(code);
            _haveError = true;
        }

        public static void RemoveCode(int code)
        {
            if (!ActiveCodes.Contains(code))
                return;

            ActiveCodes.Remove(code);
            if (ActiveCodes.Count == 0)
                _haveError = false;
        }

        public static void ToggleCode(int code, bool good)
        {
            if(good)
                RemoveCode(code);
            else
                AddCode(code);
        }

        public static bool IsValid()
        {
            return !_haveError;
        }

        public static List<string> GetCTEMCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 0) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 20) && (item <= 29)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

        public static List<string> GetCBEDCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 0) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 30) && (item <= 39)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

        public static List<string> GetSTEMCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 0) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 40) && (item <= 49)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

        public static List<string> GetImageCodes()
        {
            if(!_haveError)
                return new List<string>();
            var general = ActiveCodes.FindAll(item => (item >= 10) && (item <= 19));
            general.AddRange(ActiveCodes.FindAll(item => (item >= 50) && (item <= 59)));
            return general.Select(i => ErrorCodes[i]).ToList();
        }

    }
}
