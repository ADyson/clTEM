using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimulationGUI.Utils.Settings
{
    /// <summary>
    /// Class designed to read in files and get simulation parameters.
    /// Intended for saving particular settings or particular microscopes
    /// </summary>
    class LoadSettings
    {
        private class TempSettings
        {
            public void AddValue(int key, Match match)
            {
                switch (key)
                {
                    case 0:
                        MicroscopeName = match.Groups[0].Value;
                        break;
                    case 1:
                        Voltage = float.Parse(match.Groups[1].Value);
                        break;

                }
            }

            public string MicroscopeName;

            public float Voltage;
        }

        private class SearchStrings
        {
            public SearchStrings(string a, string b)
            {
                Name = a;
                Regexp = new Regex(b);
            }
            public string Name { get; set; }
            public Regex Regexp { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly string searchFolder = AppDomain.CurrentDomain.BaseDirectory + "_microscopes\\";

        /// <summary>
        /// 
        /// </summary>
        private const string searchExtension = "*.microscope";

        /// <summary>
        /// 
        /// </summary>
        private static readonly Dictionary<int, SearchStrings> Settings_strings = new Dictionary<int, SearchStrings>
        {
            {0, new SearchStrings("_name", @"_name[:]?\s*(.*)\s*") },
            {1, new SearchStrings("_voltage", @"_voltage[:]?\s*([\+]?[0-9]*\.?[0-9]*)\s*") },
        };

        public LoadSettings()
        {
            SearchDefaultFolder();
        }

        /// <summary>
        /// 
        /// </summary>
        public void SearchDefaultFolder()
        {
            ReadFolder(searchFolder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"></param>
        public void ReadFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath, searchExtension);

            if (!files.Any())
                return;

            // loop through every file
            // later need to put the results from each file in a list so they can be added tosome sort of combo box
            foreach (var file in files)
            {
                var currentSetings = new TempSettings();

                var lines = File.ReadAllLines(file);

                bool haveAllSettingss = true;
                // loop through each value/regex we want
                foreach (KeyValuePair<int, SearchStrings> entry in Settings_strings)
                {
                    if (!haveAllSettingss)
                        break; // is this right, want to break the dictionary foreach loop
                    // loop through file line by line
                    foreach (var line in lines)
                    {
                        // find regex
                        var match = entry.Value.Regexp.Match(line);
                        if (match.Success)
                        {
                            // need to handle more than one return value? (only 2 max?)
                            // can have dict of lists,or just a new class?

                            currentSetings.AddValue(entry.Key, match);

                            //var value = match.Groups[1].Value;
                            //string value = match.Groups[2].Value;
                        }
                        else
                        {
                            haveAllSettingss = false;
                        }
                    }
                }


            }
        }


    }
}
