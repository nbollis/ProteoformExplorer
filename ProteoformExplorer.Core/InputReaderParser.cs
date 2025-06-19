using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.Core
{
    public static class InputReaderParser
    {
        public enum InputSourceType
        {
            Promex,
            FlashDeconv,
            ThermoDecon,
            ProteoformExplorer,
            MetaMorpheus,
            TDPortal,
            ProteoformSuiteNodes,
            ProteoformSuiteEdges,
            TopFD,
            Unknown
        }
        public static List<string> AcceptedTextFileFormats = new List<string> { ".psmtsv", ".tsv", ".osmtsv", ".txt", ".feature" };
        public static List<string> AcceptedSpectraFileFormats = new List<string> { ".raw", ".mzml" };

        // populated from KnownFileExtensions.txt
        public static List<string> AllKnownFileFormats = new List<string> 
        { 
          ".raw", ".mzml", ".wiff", ".mzxml", ".mgf", ".d", ".yep", ".baf", ".fid", ".tdf", ".t2d", ".pkl",
          ".dat", ".ms", ".qgd", ".lcd", ".spc", ".sms", ".xms", ".itm", ".ita", ".tdc", ".psmtsv", ".tsv", 
          ".txt", ".csv", ".tab" , ".feature"
        };

        private static int SpeciesNameColumn;
        private static int SpectraFileNameColumn;
        private static int MonoisotopicMassColumn;
        private static int PrecursorScanNumberColumn;
        private static int IdentScanNumberColumn;
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
        private static string[] HeadersTopFD = new string[] { "Sample_ID", "ID", "Mass", "Intensity", "Time_begin" };
        private static string[] HeadersThermoDecon = new string[] { "No.", "Monoisotopic Mass", "Number of Charge States", "Scan Range" };
        private static string[] HeadersTdPortal = new string[] { "PFR", "Uniprot Id", "Monoisotopic Mass", "Result Set" };
        private static string[] HeadersProteoformExplorer = new string[] { "File Name", "Scan Number", "Retention Time", "Species", "Monoisotopic Mass", "Charge", "Peaks List" };
        private static string[] HeadersProteoformSuiteNodes = new string[] { "accession", "E_or_T", "total_intensity", "more_info", "layout" };
        private static string[] HeadersProteoformSuiteEdges = new string[] { "accession_1", "lysine_ct", "accession_2", "delta_mass", "modification" };

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
            string dataset = "";
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

                    if (fileType == InputSourceType.MetaMorpheus)
                    {
                        // assumes dataset name is a component of the file path: the name of the main search output in which the psmtsv file is located
                        dataset = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(Path.GetDirectoryName(filePath)));
                    }

                    continue;
                }

                // read the line + create the species object
                AnnotatedSpecies species = null;

                try
                {
                    switch (fileType)
                    {
                        case InputSourceType.MetaMorpheus:
                            species = GetMetaMorpheusSpecies(line, dataset);
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

                        case InputSourceType.TopFD:
                            species = GetTopFDSpecies(line, filePath);
                            break;
                    }
                }
                catch (Exception e)
                {
                    errors.Add("Error on line " + lineNum + "; " + e.Message);
                }

                // add the item to the list
                if (species != null)
                {
                    listOfSpecies.Add(species);
                }
            }

            return listOfSpecies;
        }

        private static AnnotatedSpecies GetMetaMorpheusSpecies(string line, string dataset = "")
        {
            string[] items = line.Split(ItemDelimiter);

            string baseSequence = items[SpeciesNameColumn];
            string modSequence = items[SpeciesNameColumn];

            if (items[MonoisotopicMassColumn].Contains("|") || string.IsNullOrWhiteSpace(items[MonoisotopicMassColumn]))
            {
                return null;
            }

            double mass = double.Parse(items[MonoisotopicMassColumn]);
            int charge = (int)double.Parse(items[ChargeColumn]);
            int precursorScanNumber = int.Parse(items[PrecursorScanNumberColumn]);
            int identificationScanNum = int.Parse(items[IdentScanNumberColumn]);
            string fileNameWithExtension = items[SpectraFileNameColumn];

            var id = new Identification(baseSequence, modSequence, mass, charge, precursorScanNumber, 
                identificationScanNum, fileNameWithExtension, dataset);

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
            int identificationScanNum = int.Parse(items[IdentScanNumberColumn]);
            string fileNameWithExtension = items[SpectraFileNameColumn];
            
            var id = new Identification(baseSequence, modSequence, mass, -1, -1, identificationScanNum, fileNameWithExtension);

            var species = new AnnotatedSpecies(id);

            return species;
        }

        private static AnnotatedSpecies GetFlashDeconvSpecies(string line)
        {
            string[] items = line.Split(ItemDelimiter);

            double mass = double.Parse(items[MonoisotopicMassColumn]);
            string identifier = items[SpeciesNameColumn] + " (" + mass.ToString("F3") + ")";
            
            int minChargeState = int.Parse(items[MinChargeColumn]);
            int maxChargeState = int.Parse(items[MaxChargeColumn]);
            var chargeList = Enumerable.Range(minChargeState, maxChargeState - minChargeState + 1).ToList();
            double apexRt = double.Parse(items[RetentionTimeColumn]) / 60;
            double rtStart = double.Parse(items[FeatureRtStartColumn]) / 60;
            double rtEnd = double.Parse(items[FeatureRtEndColumn]) / 60;
            string fileName = items[SpectraFileNameColumn];
            var deconFeature = new DeconvolutionFeature(mass, apexRt, rtStart, rtEnd, chargeList, fileName);
            var species = new AnnotatedSpecies(deconFeature, identifier);

            return species;
        }

        private static AnnotatedSpecies GetTopFDSpecies(string line, string filePath)
        {
            string[] items = line.Split(ItemDelimiter);
            double mass = double.Parse(items[MonoisotopicMassColumn]);
            string identifier = items[SpeciesNameColumn] + " (" + mass.ToString("F3") + ")";

            double apexRt = double.Parse(items[RetentionTimeColumn]);
            double rtStart = double.Parse(items[FeatureRtStartColumn]);
            double rtEnd = double.Parse(items[FeatureRtEndColumn]);
            int minCharge = int.Parse(items[MinChargeColumn]);
            int maxCharge = int.Parse(items[MaxChargeColumn]);
            var chargeList = Enumerable.Range(minCharge, maxCharge - minCharge + 1).ToList();

            // TODO: remove -calib-averaged from this 
            string fileName = Path.GetFileName(filePath).Replace("_ms1.feature", "-calib-averaged");


            var deconFeature = new DeconvolutionFeature(mass, apexRt, rtStart, rtEnd, chargeList, fileName);
            var species = new AnnotatedSpecies(deconFeature, identifier);

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
            string speciesName = items[SpeciesNameColumn] + " (" + mass.ToString("F3") + ")";
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
            var species = new AnnotatedSpecies(deconFeature, speciesName);

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
            int scan = int.Parse(items[PrecursorScanNumberColumn]);
            var peaks = items[PeaksListColumn].Split(',').Select(p => double.Parse(p)).ToList();

            var annotEnvelope = new AnnotatedEnvelope(scan, apexRt, charge, peaks);
            var deconFeature = new DeconvolutionFeature(mass, apexRt, apexRt, apexRt, new List<int> { charge }, fileName, 
                new List<AnnotatedEnvelope> { annotEnvelope });
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
                PrecursorScanNumberColumn = Array.IndexOf(split, "Precursor Scan Number");
                IdentScanNumberColumn = Array.IndexOf(split, "Scan Number");

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
            else if (HeadersTopFD.All(p => split.Contains(p)))
            {
                SpeciesNameColumn = Array.IndexOf(split, "ID");
                MonoisotopicMassColumn = Array.IndexOf(split, "Mass");
                FeatureRtStartColumn = Array.IndexOf(split, "Time_begin");
                RetentionTimeColumn = Array.IndexOf(split, "Apex_time");
                FeatureRtEndColumn = Array.IndexOf(split, "Time_end");
                MinChargeColumn = Array.IndexOf(split, "Minimum_charge_state");
                MaxChargeColumn = Array.IndexOf(split, "Maximum_charge_state");

                return InputSourceType.TopFD;
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
                IdentScanNumberColumn = Array.IndexOf(split, "ScanIndex");
                // tdportal doesn't seem to report precursor charge

                return InputSourceType.TDPortal;
            }
            // proteoform explorer input
            else if (HeadersProteoformExplorer.All(p => split.Contains(p)))
            {
                SpectraFileNameColumn = Array.IndexOf(split, "File Name");
                PrecursorScanNumberColumn = Array.IndexOf(split, "Scan Number");
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
