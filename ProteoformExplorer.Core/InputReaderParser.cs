using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.Core
{
    public static class InputReaderParser
    {
        public enum InputSourceType { Promex, FlashDeconv, ThermoDecon, ProteoformExplorer, MetaMorpheus, TDPortal, Unknown }
        public static List<string> AcceptedTextFileFormats = new List<string> { ".psmtsv", ".tsv", ".txt" };
        public static List<string> AcceptedSpectraFileFormats = new List<string> { ".raw", ".mzml" };

        // populated from KnownFileExtensions.txt
        public static List<string> AllKnownFileFormats = new List<string> { ".raw", ".mzml", ".wiff", ".mzxml", ".mgf", ".d", ".yep", ".baf", ".fid", ".tdf", ".t2d", ".pkl",
            ".dat", ".ms", ".qgd", ".lcd", ".spc", ".sms", ".xms", ".itm", ".ita", ".tdc", ".psmtsv", ".tsv", ".txt", ".csv", ".tab" };

        private static int SpeciesNameColumn;
        private static int SpectraFileNameColumn;
        private static int MonoisotopicMassColumn;
        private static int ScanNumberColumn;
        private static int RetentionTimeColumn;
        private static int FeatureRtStartColumn;
        private static int FeatureRtEndColumn;
        private static int ChargeColumn;
        private static int MinChargeColumn;
        private static int MaxChargeColumn;
        private static int PeaksListColumn;

        private static char[] ItemDelimiter = new char[] { '\t' };
        private static string[] HeadersFlashDeconv = new string[] { "ID", "FileName", "IsotopeCosineScore", "ChargeIntensityCosineScore" };
        private static string[] HeadersMetaMorpheus = new string[] { "File Name", "Notch", "Full Sequence", "QValue Notch" };
        private static string[] HeadersThermoDecon = new string[] { "No.", "Monoisotopic Mass", "Number of Charge States", "Scan Range" };
        private static string[] HeadersTdPortal = new string[] { "PFR", "Uniprot Id", "Monoisotopic Mass", "Result Set" };
        private static string[] HeadersProteoformExplorer = new string[] { "File Name", "Scan Number", "Retention Time", "Species", "Monoisotopic Mass", "Charge", "Peaks List" };

        public static List<AnnotatedSpecies> ReadSpeciesFromFile(string filePath, out List<string> errors)
        {
            var fileType = InputSourceType.Unknown;
            errors = new List<string>();
            var listOfSpecies = new List<AnnotatedSpecies>();

            // open the file to read
            StreamReader reader;
            try
            {
                reader = new StreamReader(filePath);
            }
            catch (Exception e)
            {
                errors.Add("Error reading file " + filePath + "\n" + e.Message);
                return listOfSpecies;
            }

            // read the file
            int lineNum = 0;

            while (reader.Peek() > 0)
            {
                string line = reader.ReadLine();
                lineNum++;

                // determine file type from the header
                if (lineNum == 1)
                {
                    fileType = GetFileTypeFromHeader(line);

                    if (fileType == InputSourceType.Unknown)
                    {
                        errors.Add("Could not interpret header labels from file: " + filePath);
                        return listOfSpecies;
                    }

                    continue;
                }

                // read the line + create the species object
                AnnotatedSpecies species = null;
                switch (fileType)
                {
                    case InputSourceType.MetaMorpheus:
                        species = GetMetaMorpheusSpecies(line);
                        break;

                    case InputSourceType.FlashDeconv:
                        species = GetFlashDeconvSpecies(line);
                        break;

                    case InputSourceType.TDPortal:
                        species = GetTdPortalSpecies(line);
                        break;

                    case InputSourceType.ThermoDecon:
                        species = GetThermoDeconSpecies(line, reader, filePath);
                        break;

                    case InputSourceType.ProteoformExplorer:
                        species = GetProteoformExplorerSpecies(line, reader);
                        break;
                }

                // add the item to the list
                if (species != null)
                {
                    listOfSpecies.Add(species);
                }
            }

            int linesWithSpecies = lineNum - 1; // first line is header

            if (linesWithSpecies != listOfSpecies.Count)
            {
                errors.Add(filePath + ": " + (linesWithSpecies - listOfSpecies.Count) + " lines of text did not contain valid species");
            }

            return listOfSpecies;
        }

        private static AnnotatedSpecies GetMetaMorpheusSpecies(string line)
        {
            string[] items = line.Split(ItemDelimiter);

            string baseSequence = items[SpeciesNameColumn];
            string modSequence = items[SpeciesNameColumn];

            if (items[MonoisotopicMassColumn].Contains("|"))
            {
                return null;
            }

            double mass = double.Parse(items[MonoisotopicMassColumn]);
            int charge = (int)double.Parse(items[ChargeColumn]);
            int precursorScanNumber = int.Parse(items[ScanNumberColumn]);
            string fileNameWithExtension = items[SpectraFileNameColumn];

            var id = new Identification(baseSequence, modSequence, mass, charge, precursorScanNumber, fileNameWithExtension);

            var species = new AnnotatedSpecies(id);

            return species;
        }

        private static AnnotatedSpecies GetTdPortalSpecies(string line)
        {
            string[] items = line.Split(ItemDelimiter);

            string baseSequence = items[SpeciesNameColumn];
            string modSequence = items[SpeciesNameColumn];
            double mass = double.Parse(items[MonoisotopicMassColumn]);
            // TD portal does not report precursor charge
            //TODO: figure out charge + precursor one based scan num
            string fileNameWithExtension = items[SpectraFileNameColumn];

            var id = new Identification(baseSequence, modSequence, mass, -1, -1, fileNameWithExtension);

            var species = new AnnotatedSpecies(id);

            return species;
        }

        private static AnnotatedSpecies GetFlashDeconvSpecies(string line)
        {
            string[] items = line.Split(ItemDelimiter);

            string identifier = items[SpeciesNameColumn];
            double mass = double.Parse(items[MonoisotopicMassColumn]);

            int minChargeState = int.Parse(items[MinChargeColumn]);
            int maxChargeState = int.Parse(items[MaxChargeColumn]);
            var chargeList = Enumerable.Range(minChargeState, maxChargeState - minChargeState + 1).ToList();
            double apexRt = double.Parse(items[RetentionTimeColumn]) / 60;
            double rtStart = double.Parse(items[FeatureRtStartColumn]) / 60;
            double rtEnd = double.Parse(items[FeatureRtEndColumn]) / 60;
            string fileName = items[SpectraFileNameColumn];
            var deconFeature = new DeconvolutionFeature(mass, apexRt, rtStart, rtEnd, chargeList, fileName);
            var species = new AnnotatedSpecies(deconFeature);

            return species;
        }

        /// <summary>
        /// The Thermo decon result filename is assumed to be the same as the spectra file name
        /// </summary>
        private static AnnotatedSpecies GetThermoDeconSpecies(string line, StreamReader reader, string fileName)
        {
            string[] items = line.Split(ItemDelimiter);
            int numCharges = int.Parse(items[3]); // TODO: get this from header, not hardcoded
            List<int> chargeList = new List<int>();

            double mass = double.Parse(items[MonoisotopicMassColumn]);
            string speciesName = items[SpeciesNameColumn] + " (" + mass.ToString("F1") + ")";
            double apexRt = double.Parse(items[RetentionTimeColumn]);
            string rtRange = items[FeatureRtStartColumn];
            double rtStart = double.Parse(rtRange.Split('-')[0].Trim());
            double rtEnd = double.Parse(rtRange.Split('-')[1].Trim());

            for (int i = 0; i < numCharges + 1; i++)
            {
                var nextLine = reader.ReadLine();
                var chargeLineSplit = nextLine.Split(ItemDelimiter);

                if (chargeLineSplit[1].Trim().Equals("Charge State", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                chargeList.Add(int.Parse(chargeLineSplit[1]));
            }

            var deconFeature = new DeconvolutionFeature(mass, apexRt, rtStart, rtEnd, chargeList, fileName);
            var species = new AnnotatedSpecies(deconFeature);

            return species;
        }

        private static AnnotatedSpecies GetProteoformExplorerSpecies(string line, StreamReader reader)
        {
            line = line.Replace("\"", string.Empty);
            string[] items = line.Split(ItemDelimiter);

            string identifier = items[SpeciesNameColumn];
            double mass = double.Parse(items[MonoisotopicMassColumn]);
            int charge = int.Parse(items[ChargeColumn]);
            double apexRt = double.Parse(items[RetentionTimeColumn]);
            string fileName = items[SpectraFileNameColumn];
            int scan = int.Parse(items[ScanNumberColumn]);
            var peaks = items[PeaksListColumn].Split(',').Select(p => double.Parse(p)).ToList();

            var annotEnvelope = new AnnotatedEnvelope(scan, apexRt, charge, peaks);
            var deconFeature = new DeconvolutionFeature(mass, apexRt, apexRt, apexRt, new List<int> { charge }, fileName, new List<AnnotatedEnvelope> { annotEnvelope });
            var species = new AnnotatedSpecies(deconFeature);

            return species;
        }

        public static InputSourceType GetFileTypeFromHeader(string line)
        {
            var split = line.Split(ItemDelimiter).Select(p => p.Trim()).ToArray();

            // metamorpheus input
            if (HeadersMetaMorpheus.All(p => split.Contains(p)))
            {
                SpeciesNameColumn = Array.IndexOf(split, "Full Sequence");
                SpectraFileNameColumn = Array.IndexOf(split, "File Name");
                MonoisotopicMassColumn = Array.IndexOf(split, "Peptide Monoisotopic Mass");
                RetentionTimeColumn = Array.IndexOf(split, "Scan Retention Time");
                ChargeColumn = Array.IndexOf(split, "Precursor Charge");
                ScanNumberColumn = Array.IndexOf(split, "Precursor Scan Number");

                return InputSourceType.MetaMorpheus;
            }
            // flashdecon input
            else if (HeadersFlashDeconv.All(p => split.Contains(p)))
            {
                SpeciesNameColumn = Array.IndexOf(split, "ID");
                SpectraFileNameColumn = Array.IndexOf(split, "FileName");
                MonoisotopicMassColumn = Array.IndexOf(split, "MonoisotopicMass");
                RetentionTimeColumn = Array.IndexOf(split, "ApexRetentionTime");
                FeatureRtStartColumn = Array.IndexOf(split, "StartRetentionTime");
                FeatureRtEndColumn = Array.IndexOf(split, "EndRetentionTime");
                MinChargeColumn = Array.IndexOf(split, "MinCharge");
                MaxChargeColumn = Array.IndexOf(split, "MaxCharge");

                return InputSourceType.FlashDeconv;
            }
            // thermo decon input
            else if (HeadersThermoDecon.All(p => split.Select(p => p.Trim()).Contains(p)))
            {
                SpeciesNameColumn = Array.IndexOf(split, "No.");
                MonoisotopicMassColumn = Array.IndexOf(split, "Monoisotopic Mass");
                // thermo decon does not include file name
                RetentionTimeColumn = Array.IndexOf(split, "Apex RT");
                FeatureRtStartColumn = Array.IndexOf(split, "RT Range");
                ChargeColumn = Array.IndexOf(split, "Precursor Charge");

                return InputSourceType.ThermoDecon;
            }
            // td portal input
            else if (HeadersTdPortal.All(p => split.Contains(p)))
            {
                SpeciesNameColumn = Array.IndexOf(split, "Sequence"); // does not include mods
                SpectraFileNameColumn = Array.IndexOf(split, "File Name");
                MonoisotopicMassColumn = Array.IndexOf(split, "Monoisotopic Mass");
                RetentionTimeColumn = Array.IndexOf(split, "RetentionTime");
                //ChargeColumn = Array.IndexOf(split, "Precursor Charge"); // tdportal doesn't seem to report precursor charge

                return InputSourceType.TDPortal;
            }
            // proteoform explorer input
            else if (HeadersProteoformExplorer.All(p => split.Contains(p)))
            {
                SpectraFileNameColumn = Array.IndexOf(split, "File Name");
                ScanNumberColumn = Array.IndexOf(split, "Scan Number");
                RetentionTimeColumn = Array.IndexOf(split, "Retention Time");
                SpeciesNameColumn = Array.IndexOf(split, "Species");
                MonoisotopicMassColumn = Array.IndexOf(split, "Monoisotopic Mass");
                ChargeColumn = Array.IndexOf(split, "Charge");
                PeaksListColumn = Array.IndexOf(split, "Peaks List");

                return InputSourceType.ProteoformExplorer;
            }

            // input not recognized based on column headers
            return InputSourceType.Unknown;
        }
    }
}
