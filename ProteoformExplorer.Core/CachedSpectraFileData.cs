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

namespace ProteoformExplorer.Core
{
    public class CachedSpectraFileData
    {
        public KeyValuePair<string, MsDataFile> DataFile { get; private set; }
        public ConcurrentDictionary<int, List<AnnotatedSpecies>> OneBasedScanToAnnotatedSpecies { get; private set; }
        public ConcurrentDictionary<int, List<AnnotatedEnvelope>> OneBasedScanToAnnotatedEnvelopes { get; private set; }
        private List<Datum> TicData;
        private List<Datum> IdentifiedTicData;
        private List<Datum> DeconvolutedTicData;
        private static ConcurrentDictionary<(string, int), MsDataScan> CachedScans;
        private static int NumScansToCache;
        private static ConcurrentQueue<(string, int)> CachedScanNumberQueue;

        public CachedSpectraFileData(KeyValuePair<string, MsDataFile> loadedDataFile)
        {
            DataFile = loadedDataFile;
            TicData = new List<Datum>();
            IdentifiedTicData = new List<Datum>();
            DeconvolutedTicData = new List<Datum>();
            OneBasedScanToAnnotatedSpecies = new ConcurrentDictionary<int, List<AnnotatedSpecies>>();
            OneBasedScanToAnnotatedEnvelopes = new ConcurrentDictionary<int, List<AnnotatedEnvelope>>();
            CachedScans = new ConcurrentDictionary<(string, int), MsDataScan>();
            NumScansToCache = 10000;
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

                    OneBasedScanToAnnotatedEnvelopes.AddOrUpdate(scanNum, [envelope], (key, list) =>
                    {
                        list.Add(envelope);
                        return list;
                    });
                    OneBasedScanToAnnotatedSpecies.AddOrUpdate(scanNum, [species], (key, list) =>
                    {
                        list.Add(species);
                        return list;
                    });
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

            lock (CachedScans)
            {
                if (!CachedScans.TryGetValue(cacheKey, out scan))
                {
                    scan = DataFile.Value.GetOneBasedScanFromDynamicConnection(oneBasedScanNum);

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
            }

            return scan;
        }

        public List<Datum> GetTicChromatogram()
        {
            HashSet<double> deconClaimedMzs = new HashSet<double>();
            HashSet<double> identClaimedMzs = new HashSet<double>();

            if (TicData.Count == 0)
            {
                int lastScanNum = PfmXplorerUtil.GetLastOneBasedScanNumber(new KeyValuePair<string, CachedSpectraFileData>(DataFile.Key, this));
                var identifiedTicDict = OneBasedScanToAnnotatedSpecies.Values
                    .SelectMany(p => p.Where(m => m.Identification != null))
                    .Select(species => species.Identification.Dataset)
                    .Distinct()
                    .ToDictionary(p => p, p => 0.0);

                for (int i = 1; i <= lastScanNum; i++)
                {
                    deconClaimedMzs.Clear();
                    identClaimedMzs.Clear();
                    var scan = GetOneBasedScan(i);
                    double deconvolutedTic = 0;
                    identifiedTicDict.ForEach(p => identifiedTicDict[p.Key] = 0.0);


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
                    var identifiedDatumDict = identifiedTicDict.ToDictionary(p => p.Key, p => new Datum(scan.RetentionTime, 0, scan.OneBasedScanNumber));

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
                                    identifiedTicDict[envelope.Species.Identification.Dataset] += scan.MassSpectrum.YArray[index];
                                }
                            }
                        }

                        deconDatum = new Datum(scan.RetentionTime, deconvolutedTic, scan.OneBasedScanNumber);
                        identifiedTicDict.ForEach(p => identifiedDatumDict[p.Key] =
                            new Datum(scan.RetentionTime, identifiedTicDict[p.Key], scan.OneBasedScanNumber, p.Key));
                    }

                    DeconvolutedTicData.Add(deconDatum);
                    IdentifiedTicData.AddRange(identifiedDatumDict.Values);
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
