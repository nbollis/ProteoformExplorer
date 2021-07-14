using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorer.Deconvoluter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.Core
{
    public class CachedSpectraFileData
    {
        public KeyValuePair<string, DynamicDataConnection> DataFile { get; private set; }
        public Dictionary<int, List<AnnotatedSpecies>> OneBasedScanToAnnotatedSpecies { get; private set; }
        public Dictionary<int, List<AnnotatedEnvelope>> OneBasedScanToAnnotatedEnvelopes { get; private set; }
        private List<Datum> TicData;
        private List<Datum> IdentifiedTicData;
        private List<Datum> DeconvolutedTicData;
        private static Dictionary<(string, int), MsDataScan> CachedScans;
        private static int NumScansToCache;
        private static Queue<(string, int)> CachedScanNumberQueue;

        public CachedSpectraFileData(KeyValuePair<string, DynamicDataConnection> loadedDataFile)
        {
            DataFile = loadedDataFile;
            TicData = new List<Datum>();
            IdentifiedTicData = new List<Datum>();
            DeconvolutedTicData = new List<Datum>();
            OneBasedScanToAnnotatedSpecies = new Dictionary<int, List<AnnotatedSpecies>>();
            OneBasedScanToAnnotatedEnvelopes = new Dictionary<int, List<AnnotatedEnvelope>>();
            CachedScans = new Dictionary<(string, int), MsDataScan>();
            NumScansToCache = 10000;
            CachedScanNumberQueue = new Queue<(string, int)>();
        }

        public void CreateAnnotatedDeconvolutionFeatures(List<AnnotatedSpecies> allAnnotatedSpecies)
        {
            OneBasedScanToAnnotatedSpecies.Clear();
            OneBasedScanToAnnotatedEnvelopes.Clear();

            foreach (var species in allAnnotatedSpecies.Where(p => p.SpectraFileNameWithoutExtension == PfmXplorerUtil.GetFileNameWithoutExtension(DataFile.Key)))
            {
                // the deconvoluted species is from a file type that does not specify the envelopes in the deconvolution feature
                // therefore, we'll have to do some peakfinding and guess what envelopes are part of this feature
                if (species.DeconvolutionFeature == null)
                {
                    if (species.Identification != null)
                    {
                        // do some peakfinding for species that have been identified but don't have a chromatographic peak assigned to them
                        // (i.e., from a top-down search program)
                        species.Identification.GetPrecursorInfoForIdentification();

                        if (species.Identification.PrecursorChargeState > 0 && species.Identification.OneBasedPrecursorScanNumber > 0)
                        {
                            species.DeconvolutionFeature = new DeconvolutionFeature(species.Identification, new KeyValuePair<string, CachedSpectraFileData>(DataFile.Key, this));
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // TODO: some kind of error message? or just skip? this species doesn't have a deconvolution feature or an identification...
                        continue;
                    }
                }

                species.DeconvolutionFeature.FindAnnotatedEnvelopesInData(new KeyValuePair<string, CachedSpectraFileData>(DataFile.Key, this));

                foreach (AnnotatedEnvelope envelope in species.DeconvolutionFeature.AnnotatedEnvelopes)
                {
                    int scanNum = envelope.OneBasedScanNumber;
                    envelope.Species = species;

                    if (!OneBasedScanToAnnotatedSpecies.ContainsKey(scanNum))
                    {
                        OneBasedScanToAnnotatedSpecies.Add(scanNum, new List<AnnotatedSpecies>());
                    }
                    if (!OneBasedScanToAnnotatedEnvelopes.ContainsKey(scanNum))
                    {
                        OneBasedScanToAnnotatedEnvelopes.Add(scanNum, new List<AnnotatedEnvelope>());
                    }

                    OneBasedScanToAnnotatedSpecies[scanNum].Add(species);
                    OneBasedScanToAnnotatedEnvelopes[scanNum].Add(envelope);
                }
            }
        }

        public MsDataScan GetOneBasedScan(int oneBasedScanNum)
        {
            lock (CachedScans)
            {
                if (!CachedScans.TryGetValue((this.DataFile.Key, oneBasedScanNum), out var scan))
                {
                    scan = DataFile.Value.GetOneBasedScanFromDynamicConnection(oneBasedScanNum);

                    if (scan == null)
                    {
                        return scan;
                    }

                    while (CachedScans.Count > NumScansToCache)
                    {
                        var scanToRemove = CachedScanNumberQueue.Dequeue();
                        CachedScans.Remove(scanToRemove);
                    }

                    if (CachedScans.TryAdd((this.DataFile.Key, scan.OneBasedScanNumber), scan))
                    {
                        CachedScanNumberQueue.Enqueue((this.DataFile.Key, oneBasedScanNum));
                    }
                }

                return scan;
            }
        }

        public List<Datum> GetTicChromatogram()
        {
            HashSet<double> deconClaimedMzs = new HashSet<double>();
            HashSet<double> identClaimedMzs = new HashSet<double>();

            if (TicData.Count == 0)
            {
                int lastScanNum = PfmXplorerUtil.GetLastOneBasedScanNumber(new KeyValuePair<string, CachedSpectraFileData>(DataFile.Key, this));

                for (int i = 1; i <= lastScanNum; i++)
                {
                    deconClaimedMzs.Clear();
                    identClaimedMzs.Clear();
                    var scan = GetOneBasedScan(i);
                    double deconvolutedTic = 0;
                    double identifiedTic = 0;

                    // tic
                    if (scan != null && scan.MsnOrder == 1)
                    {
                        TicData.Add(new Datum(scan.RetentionTime, scan.TotalIonCurrent, scan.OneBasedScanNumber));
                    }
                    else
                    {
                        continue;
                    }

                    // deconvoluted and identified tic
                    var deconDatum = new Datum(scan.RetentionTime, 0, scan.OneBasedScanNumber);
                    var identDatum = new Datum(scan.RetentionTime, 0, scan.OneBasedScanNumber);

                    if (OneBasedScanToAnnotatedEnvelopes.TryGetValue(i, out var annotatedEnvelopes))
                    {
                        foreach (var envelope in annotatedEnvelopes)
                        {
                            foreach (double mz in envelope.PeakMzs)
                            {
                                int index = scan.MassSpectrum.GetClosestPeakIndex(mz);
                                double actualMz = scan.MassSpectrum.XArray[index];

                                if (!deconClaimedMzs.Contains(actualMz))
                                {
                                    deconClaimedMzs.Add(actualMz);
                                    deconvolutedTic += scan.MassSpectrum.YArray[index];
                                }

                                if (envelope.Species.Identification != null && !identClaimedMzs.Contains(actualMz))
                                {
                                    identClaimedMzs.Add(actualMz);
                                    identifiedTic += scan.MassSpectrum.YArray[index];
                                }
                            }
                        }

                        deconDatum = new Datum(scan.RetentionTime, deconvolutedTic, scan.OneBasedScanNumber);
                        identDatum = new Datum(scan.RetentionTime, identifiedTic, scan.OneBasedScanNumber);
                    }

                    DeconvolutedTicData.Add(deconDatum);
                    IdentifiedTicData.Add(identDatum);
                }
            }

            return TicData;
        }

        public List<Datum> GetDeconvolutedTicChromatogram()
        {
            GetTicChromatogram();

            return DeconvolutedTicData;
        }

        public List<Datum> GetIdentifiedTicChromatogram()
        {
            GetTicChromatogram();

            return IdentifiedTicData;
        }

        public List<AnnotatedSpecies> GetSpeciesInScan(int oneBasedScan)
        {
            if (OneBasedScanToAnnotatedSpecies.TryGetValue(oneBasedScan, out var species))
            {
                return species;
            }

            return null;
        }

        public List<MsDataScan> GetScansInRtWindow(double rt, double rtWindow)
        {
            double rtWindowHalfWidth = rtWindow / 2;

            var xic = GetTicChromatogram();
            var firstScan = xic.First(p => p.X > rt - rtWindowHalfWidth);

            var scans = new List<MsDataScan>();
            for (int i = xic.IndexOf(firstScan); i < xic.Count; i++)
            {
                int scanNum = (int)Math.Round(xic[i].Z.Value);
                var theScan = GetOneBasedScan(scanNum);

                if (theScan.MsnOrder == 1)
                {
                    scans.Add(theScan);
                }

                if (theScan.RetentionTime >= rt + rtWindowHalfWidth)
                {
                    break;
                }
            }

            return scans;
        }

        public List<AnnotatedEnvelope> GetDistinctEnvelopes()
        {
            var envelopes = new List<AnnotatedEnvelope>();

            HashSet<double> mzsClaimed = new HashSet<double>();

            foreach (var scan in OneBasedScanToAnnotatedEnvelopes)
            {
                mzsClaimed.Clear();

                foreach (AnnotatedEnvelope envelope in scan.Value)
                {
                    int uniquePeakCount = 0;

                    foreach (double peakMz in envelope.PeakMzs)
                    {
                        if (!mzsClaimed.Contains(peakMz))
                        {
                            uniquePeakCount++;
                            mzsClaimed.Add(peakMz);
                        }
                    }

                    if (uniquePeakCount > 1)
                    {
                        envelopes.Add(envelope);
                    }
                }
            }

            return envelopes;
        }
    }
}
