using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorer.Deconvoluter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Easy.Common.Extensions;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ProteoformExplorer.Core
{
    public class CachedSpectraFileData
    {
        public KeyValuePair<string, MsDataFile> DataFile { get; private set; }
        public Dictionary<int, List<AnnotatedSpecies>> OneBasedScanToAnnotatedSpecies { get; private set; }
        public Dictionary<int, List<AnnotatedEnvelope>> OneBasedScanToAnnotatedEnvelopes { get; private set; }
        private List<Datum> TicData;
        private List<Datum> IdentifiedTicData;
        private List<Datum> DeconvolutedTicData;
        private static ConcurrentDictionary<(string, int), MsDataScan> CachedScans;
        private static int NumScansToCache;
        private static ConcurrentQueue<(string, int)> CachedScanNumberQueue;

        public CachedSpectraFileData(KeyValuePair<string, MsDataFile> loadedDataFile)
        {
            DataFile = loadedDataFile;
            DataFile.Value.LoadAllStaticData();
            TicData = new List<Datum>();
            IdentifiedTicData = new List<Datum>();
            DeconvolutedTicData = new List<Datum>();
            OneBasedScanToAnnotatedSpecies = new();
            OneBasedScanToAnnotatedEnvelopes = new();
            CachedScans = new ConcurrentDictionary<(string, int), MsDataScan>();
            NumScansToCache = 50000;
            CachedScanNumberQueue = new ConcurrentQueue<(string, int)>();
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
            var cacheKey = (DataFile.Key, oneBasedScanNum);

            if (CachedScans.TryGetValue(cacheKey, out var scan))
            {
                return scan;
            }

            if (!CachedScans.TryGetValue(cacheKey, out scan))
            {
                scan = DataFile.Value.GetOneBasedScan(oneBasedScanNum);

                if (scan == null)
                {
                    return null;
                }

                if (CachedScans.Count >= NumScansToCache && CachedScanNumberQueue.TryDequeue(out var scanToRemove))
                {
                    CachedScans.TryRemove(scanToRemove, out _);
                }

                if (CachedScans.TryAdd(cacheKey, scan))
                {
                    CachedScanNumberQueue.Enqueue(cacheKey);
                }
            }

            return scan;
        }

        public List<Datum> GetTicChromatogram(int rollingAverage = 0)
        {
            HashSet<double> deconClaimedMzs = new HashSet<double>();
            Dictionary<string, HashSet<double>> identClaimedMzs = new();
            Dictionary<string, double> identifiedTicDict = new();
            string[] datasetNames = OneBasedScanToAnnotatedSpecies.Values
                .SelectMany(p => p.Where(m => m.Identification != null))
                .Select(species => species.Identification.Dataset)
                .Distinct()
                .ToArray();

            if (TicData.Count != 0) return TicData;
            
            int lastScanNum = DataFile.Value.Scans[^1].OneBasedScanNumber;

            foreach (var dataset in datasetNames)
            {
                identClaimedMzs.Add(dataset, new HashSet<double>());
                identifiedTicDict.Add(dataset, 0.0);
            }

            for (int i = 1; i <= lastScanNum; i++)
            {

                var scan = DataFile.Value.GetOneBasedScan(i);

                // Only process MS1s
                if (scan == null || scan.MsnOrder != 1)
                    continue;
                else
                    TicData.Add(new Datum(scan.RetentionTime, scan.TotalIonCurrent, scan.OneBasedScanNumber));

                // Reset claimed mzs and tic for this scan
                deconClaimedMzs.Clear();
                double deconvolutedTic = 0;
                identifiedTicDict.ForEach(p => identifiedTicDict[p.Key] = 0.0);
                identClaimedMzs.ForEach(p => p.Value.Clear());

                // deconvoluted and identified tic
                var deconDatum = new Datum(scan.RetentionTime, 0, scan.OneBasedScanNumber);

                // Pull all annotated envelopes for this scan. 
                if (OneBasedScanToAnnotatedEnvelopes.TryGetValue(i, out var annotatedEnvelopes))
                {
                    foreach (var envelope in annotatedEnvelopes)
                    {
                        foreach (double mz in envelope.PeakMzs)
                        {
                            int index = scan.MassSpectrum.GetClosestPeakIndex(mz);
                            double actualMz = scan.MassSpectrum.XArray[index];

                            if (deconClaimedMzs.Add(actualMz))
                            {
                                deconvolutedTic += scan.MassSpectrum.YArray[index];
                            }

                            if (envelope.Species.Identification == null)
                                continue;

                            if (identClaimedMzs[envelope.Species.Identification.Dataset].Contains(actualMz)) 
                                continue;

                            identClaimedMzs[envelope.Species.Identification.Dataset].Add(actualMz);
                            identifiedTicDict[envelope.Species.Identification.Dataset] += scan.MassSpectrum.YArray[index];
                        }
                    }

                    deconDatum = new Datum(scan.RetentionTime, deconvolutedTic, scan.OneBasedScanNumber);
                }

                DeconvolutedTicData.Add(deconDatum);
                foreach (var searchSpecificTicData in identifiedTicDict)
                {
                    IdentifiedTicData.Add(new Datum(scan.RetentionTime, searchSpecificTicData.Value, scan.OneBasedScanNumber, searchSpecificTicData.Key));
                }
            }

            // Apply rolling average
            if (rollingAverage > 0)
            {
                TicData = TicData.RollingAverage(rollingAverage);
                DeconvolutedTicData = DeconvolutedTicData.RollingAverage(rollingAverage);
                IdentifiedTicData = IdentifiedTicData.RollingAverage(rollingAverage);
            }
            
            return TicData;
        }

        public List<Datum> GetDeconvolutedTicChromatogram(int rollingAverage = 0)
        {
            GetTicChromatogram(rollingAverage);

            return DeconvolutedTicData;
        }

        public List<Datum> GetIdentifiedTicChromatogram(int rollingAverage = 0)
        {
            GetTicChromatogram(rollingAverage);

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
