using ProteoformExplorer.Deconvoluter;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using UsefulProteomicsDatabases;
using System.Linq;
using System.IO;

namespace ProteoformExplorer.Core
{
    public static class PfmXplorerUtil
    {
        public static DeconvolutionEngine DeconvolutionEngine
        {
            get
            {
                if (_deconvolutionEngine == null)
                {
                    Loaders.LoadElements();
                    _deconvolutionEngine = new DeconvolutionEngine(2000, 0.3, 4, 0.3, 3, 5, 2, 60, 2);
                }

                return _deconvolutionEngine;
            }
        }

        private static DeconvolutionEngine _deconvolutionEngine;
        static Dictionary<string, double[]> SpectraFilePathsToRtArray;

        public static MsDataScan GetClosestScanToRtFromDynamicConnection(KeyValuePair<string, CachedSpectraFileData> data, double rt)
        {
            if (SpectraFilePathsToRtArray == null)
            {
                SpectraFilePathsToRtArray = new Dictionary<string, double[]>();
            }

            if (!SpectraFilePathsToRtArray.TryGetValue(data.Key, out var rtArray))
            {
                var lastScanNumber = GetLastOneBasedScanNumber(data);
                var lastScan = data.Value.GetOneBasedScan(lastScanNumber);

                var arr = new double[lastScan.OneBasedScanNumber];
                SpectraFilePathsToRtArray.Add(data.Key, arr);

                for (int j = 0; j < arr.Length; j++)
                {
                    arr[j] = double.NaN;
                }

                arr[arr.Length - 1] = lastScan.RetentionTime;
                rtArray = arr;
            }

            int m = 0;
            int l = 0;
            int r = rtArray.Length - 1;

            // binary search
            while (l <= r)
            {
                m = (l + r) / 2;

                if (double.IsNaN(rtArray[m]))
                {
                    rtArray[m] = data.Value.GetOneBasedScan(m + 1).RetentionTime;
                }

                if (r - l < 2)
                {
                    for (; r >= 0; r--)
                    {
                        if (double.IsNaN(rtArray[r]))
                        {
                            rtArray[r] = data.Value.GetOneBasedScan(r + 1).RetentionTime;
                        }

                        if (rtArray[r] <= rt || r == 0)
                        {
                            return data.Value.GetOneBasedScan(r + 1);
                        }
                    }
                }

                if (rt > rtArray[m])
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            // TODO: error?
            return null;
        }

        public static MsDataScan GetClosestScanToRtFromDynamicConnection(KeyValuePair<string, CachedSpectraFileData> data, double rt, int msOrder)
        {
            var closestScan = GetClosestScanToRtFromDynamicConnection(data, rt);
            MsDataScan next = null;
            MsDataScan previous = null;

            for (int i = closestScan.OneBasedScanNumber; i < int.MaxValue; i++)
            {
                var scan = data.Value.GetOneBasedScan(i);

                if (scan == null || scan.MsnOrder == msOrder)
                {
                    next = scan;
                    break;
                }
            }

            for (int i = closestScan.OneBasedScanNumber - 1; i >= 1; i--)
            {
                var scan = data.Value.GetOneBasedScan(i);

                if (scan == null || scan.MsnOrder == msOrder)
                {
                    previous = scan;
                    break;
                }
            }

            if (next == null)
            {
                return previous;
            }
            if (previous == null)
            {
                return next;
            }
            if (Math.Abs(next.RetentionTime - rt) < Math.Abs(previous.RetentionTime - rt))
            {
                return next;
            }

            return previous;
        }

        public static int GetLastOneBasedScanNumber(KeyValuePair<string, CachedSpectraFileData> data)
        {
            int m = 1;
            int l = 1;
            int r = int.MaxValue - l;

            while (l <= r)
            {
                m = (l + r) / 2;

                if (r - l < 2)
                {
                    for (; r >= 0; r--)
                    {
                        if (data.Value.GetOneBasedScan(r) != null)
                        {
                            return r;
                        }
                    }
                }

                if (data.Value.GetOneBasedScan(m) != null)
                {
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            return 0;
        }

        public static string GetFileNameWithoutExtension(string filename)
        {
            string str = Path.GetFileName(filename);

            foreach (string extension in InputReaderParser.AllKnownFileFormats.Where(p => str.Contains(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                str = str.Replace(extension, string.Empty, StringComparison.InvariantCultureIgnoreCase);
            }

            return str;
        }
    }
}
