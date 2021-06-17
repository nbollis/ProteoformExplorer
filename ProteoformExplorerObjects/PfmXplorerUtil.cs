using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorerObjects;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ProteoformExplorer
{
    public static class PfmXplorerUtil
    {
        static Dictionary<string, double[]> SpectraFilePathsToRtArray;

        public static double GetXPositionFromMouseClickOnChart(object sender, MouseButtonEventArgs e)
        {
            var plot = (WpfPlot)sender;

            if (plot == null)
            {
                return double.NaN;
            }

            int pixelX = (int)e.MouseDevice.GetPosition(plot).X;
            int pixelY = (int)e.MouseDevice.GetPosition(plot).Y;

            (double coordinateX, double coordinateY) = plot.GetMouseCoordinates();

            return coordinateX;
        }

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

        public static AnnotatedEnvelope GetAnnotatedEnvelope(double monoMass, MsDataScan scan, int charge, double ppmTolerance = 5)
        {
            List<double> mzPeaks = new List<double>();
            Tolerance tol = new PpmTolerance(ppmTolerance);

            for (int i = 0; i < 20; i++)
            {
                double isotopeMass = monoMass + i * Constants.C13MinusC12;
                double isotopeMz = isotopeMass.ToMz(charge);
                int ind = scan.MassSpectrum.GetClosestPeakIndex(isotopeMz);

                double expMz = scan.MassSpectrum.XArray[ind];

                if (tol.Within(expMz.ToMass(charge), isotopeMass) && !mzPeaks.Contains(expMz))
                {
                    mzPeaks.Add(expMz);
                }
                else if (mzPeaks.Count > 0)
                {
                    break;
                }
            }

            return new AnnotatedEnvelope(scan.OneBasedScanNumber, scan.RetentionTime, charge, mzPeaks);
        }
    }
}
