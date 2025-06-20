using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.Core
{
    public class Identification
    {
        public string FullSequence { get; private set; }
        public string BaseSequence { get; private set; }
        public double MonoisotopicMass { get; private set; }
        public int PrecursorChargeState { get; private set; }
        public int OneBasedPrecursorScanNumber { get; private set; }
        public int IdentificationScanNum { get; private set; }
        public string SpectraFileNameWithoutExtension { get; private set; }
        public string Dataset { get; private set; }
        public Polarity Polarity { get; private set; }

        public Identification(string baseSequence, string modifiedSequence, double monoMass, int chargeState,
            int precursorScanNum, int identificationScanNum, string spectraFileNameWithoutExtension, string dataset = "")
        {
            this.FullSequence = modifiedSequence;
            this.BaseSequence = baseSequence;
            this.MonoisotopicMass = monoMass;
            this.PrecursorChargeState = chargeState;
            this.OneBasedPrecursorScanNumber = precursorScanNum;
            this.IdentificationScanNum = identificationScanNum;
            this.Dataset = dataset;

            this.SpectraFileNameWithoutExtension = PfmXplorerUtil.GetFileNameWithoutExtension(spectraFileNameWithoutExtension);
        }

        public void GetPrecursorInfoForIdentification()
        {
            var file = DataManagement.SpectraFiles.FirstOrDefault(p => PfmXplorerUtil.GetFileNameWithoutExtension(p.Key) == this.SpectraFileNameWithoutExtension);

            if (file.Value == null)
            {
                return;
            }

            // get the precursor scan number
            if (OneBasedPrecursorScanNumber <= 0 && IdentificationScanNum > 0)
            {
                for (int i = IdentificationScanNum; i > 0; i--)
                {
                    var scan = file.Value.GetOneBasedScan(i);

                    if (scan == null)
                    {
                        continue;
                    }

                    if (scan.MsnOrder == 1)
                    {
                        OneBasedPrecursorScanNumber = scan.OneBasedScanNumber;
                        break;
                    }
                }
            }

            Polarity = file.Value.GetOneBasedScan(IdentificationScanNum).Polarity;

            // get the precursor charge
            if (OneBasedPrecursorScanNumber > 0 && ((PrecursorChargeState <= 0 && Polarity == Polarity.Positive) || (PrecursorChargeState >= 0 && Polarity == Polarity.Negative)))
            {
                var fragmentationScan = file.Value.GetOneBasedScan(IdentificationScanNum);

                if (fragmentationScan != null && fragmentationScan.IsolationRange != null)
                {
                    double approxZ = MonoisotopicMass / fragmentationScan.IsolationRange.Mean;
                    PrecursorChargeState = (int)Math.Round(approxZ);
                }
            }
        }
    }
}
