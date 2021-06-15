using System;
using System.Collections.Generic;
using System.Text;

namespace ProteoformExplorer
{
    public class Identification
    {
        public string FullSequence { get; private set; }
        public string BaseSequence { get; private set; }
        public double MonoisotopicMass { get; private set; }
        public int PrecursorChargeState { get; private set; }
        public int OneBasedPrecursorScanNumber { get; private set; }
        public Identification(string baseSequence, string modifiedSequence, double monoMass, int chargeState, int precursorScanNum)
        {
            this.FullSequence = modifiedSequence;
            this.BaseSequence = baseSequence;
            this.MonoisotopicMass = monoMass;
            this.PrecursorChargeState = chargeState;
            this.OneBasedPrecursorScanNumber = precursorScanNum;
        }
    }
}
