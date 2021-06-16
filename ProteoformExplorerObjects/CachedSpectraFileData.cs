using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProteoformExplorerObjects
{
    public class CachedSpectraFileData
    {
        public KeyValuePair<string, DynamicDataConnection> DataFile { get; private set; }
        public Dictionary<int, HashSet<AnnotatedSpecies>> OneBasedScanToAnnotatedSpecies { get; private set; }
        public Dictionary<int, List<AnnotatedEnvelope>> OneBasedScanToAnnotatedEnvelopes { get; private set; }
        private List<Datum> TicData;
        private List<Datum> IdentifiedTicData;
        private List<Datum> DeconvolutedTicData;
        private Dictionary<int, MsDataScan> CachedScans;
        private int ScansToCache;

        public CachedSpectraFileData(KeyValuePair<string, DynamicDataConnection> loadedDataFile)
        {
            DataFile = loadedDataFile;
            TicData = new List<Datum>();
            IdentifiedTicData = new List<Datum>();
            DeconvolutedTicData = new List<Datum>();
            OneBasedScanToAnnotatedSpecies = new Dictionary<int, HashSet<AnnotatedSpecies>>();
            OneBasedScanToAnnotatedEnvelopes = new Dictionary<int, List<AnnotatedEnvelope>>();
            CachedScans = new Dictionary<int, MsDataScan>();
            ScansToCache = 1000;
        }

        public void BuildScanToSpeciesDictionary(List<AnnotatedSpecies> allAnnotatedSpecies)
        {
            OneBasedScanToAnnotatedSpecies.Clear();

            foreach (var species in allAnnotatedSpecies)
            {
                if (species.DeconvolutionFeature != null)
                {
                    foreach (var envelope in species.DeconvolutionFeature.AnnotatedEnvelopes)
                    {
                        int scanNum = envelope.OneBasedScanNumber;

                        if (!OneBasedScanToAnnotatedSpecies.ContainsKey(scanNum))
                        {
                            OneBasedScanToAnnotatedSpecies.Add(scanNum, new HashSet<AnnotatedSpecies>());
                        }
                        if (!OneBasedScanToAnnotatedEnvelopes.ContainsKey(scanNum))
                        {
                            OneBasedScanToAnnotatedEnvelopes.Add(scanNum, new List<AnnotatedEnvelope>());
                        }

                        OneBasedScanToAnnotatedSpecies[scanNum].Add(species);
                        OneBasedScanToAnnotatedEnvelopes[scanNum].Add(envelope);
                    }
                }

                if (species.Identification != null)
                {
                    int scanNum = species.Identification.OneBasedPrecursorScanNumber;

                    if (!OneBasedScanToAnnotatedSpecies.ContainsKey(scanNum))
                    {
                        OneBasedScanToAnnotatedSpecies.Add(scanNum, new HashSet<AnnotatedSpecies>());
                    }

                    OneBasedScanToAnnotatedSpecies[scanNum].Add(species);
                }
            }
        }

        public MsDataScan GetOneBasedScan(int oneBasedScanNum)
        {
            if (!CachedScans.TryGetValue(oneBasedScanNum, out var scan))
            {
                scan = DataFile.Value.GetOneBasedScanFromDynamicConnection(oneBasedScanNum);

                if (scan == null)
                {
                    return scan;
                }

                while (CachedScans.Count > ScansToCache)
                {
                    int minKey = CachedScans.Min(p => p.Key);
                    CachedScans.Remove(minKey);
                }

                CachedScans.Add(scan.OneBasedScanNumber, scan);
            }

            return scan;
        }

        public List<Datum> GetTicChromatogram()
        {
            HashSet<double> claimedMzs = new HashSet<double>();
            if (TicData.Count == 0)
            {
                int lastScanNum = PfmXplorerUtil.GetLastOneBasedScanNumber(new KeyValuePair<string, CachedSpectraFileData>(DataFile.Key, this));

                for (int i = 1; i <= lastScanNum; i++)
                {
                    claimedMzs.Clear();
                    var scan = GetOneBasedScan(i);
                    double deconvolutedTic = 0;

                    // tic
                    if (scan != null && scan.MsnOrder == 1)
                    {
                        TicData.Add(new Datum(scan.RetentionTime, scan.TotalIonCurrent));
                    }

                    // deconvoluted tic
                    if (OneBasedScanToAnnotatedEnvelopes.TryGetValue(i, out var annotatedEnvelopes) && scan.MsnOrder == 1)
                    {
                        foreach (var envelope in annotatedEnvelopes)
                        {
                            foreach (double mz in envelope.PeakMzs)
                            {
                                int index = scan.MassSpectrum.GetClosestPeakIndex(mz);
                                double actualMz = scan.MassSpectrum.XArray[index];

                                if (claimedMzs.Contains(actualMz))
                                {
                                    continue;
                                }

                                claimedMzs.Add(actualMz);
                                deconvolutedTic += scan.MassSpectrum.YArray[index];
                            }
                        }

                        DeconvolutedTicData.Add(new Datum(scan.RetentionTime, deconvolutedTic));
                    }
                    
                    // TODO: identified tic
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

        public HashSet<AnnotatedSpecies> SpeciesInScan(int oneBasedScan)
        {
            if (OneBasedScanToAnnotatedSpecies.TryGetValue(oneBasedScan, out var species))
            {
                return species;
            }

            return null;
        }
    }
}
