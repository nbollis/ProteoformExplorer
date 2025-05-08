#nullable enable
using Readers;
using System.Collections.Generic;
using System.Linq;

namespace ProteoformExplorer.Core.IO;

public interface IFileProcessingStrategy
{
    List<AnnotatedSpecies> ProcessFile(string filePath, string dataset = "");
}

public class MetaMorpheusProcessingStrategy : IFileProcessingStrategy
{
    public List<AnnotatedSpecies> ProcessFile(string filePath, string dataset = "")
    {
        var psms = SpectrumMatchTsvReader.ReadTsv(filePath, out var errors);

        List<AnnotatedSpecies> species = new List<AnnotatedSpecies>(psms.Count);
        foreach (var spectralMatch in psms)
        {
            var id = new Identification(spectralMatch.BaseSequence, spectralMatch.FullSequence, spectralMatch.MonoisotopicMass, spectralMatch.ChargeState,
                spectralMatch.PrecursorScanNum, spectralMatch.Ms2ScanNumber, spectralMatch.FileNameWithoutExtension, dataset);

            species.Add(new AnnotatedSpecies(id));
        }
        return species;
    }
}

public class Ms1FeatureProcessingStrategy : IFileProcessingStrategy
{
    public List<AnnotatedSpecies> ProcessFile(string filePath, string dataset = "")
    {
        var features = FileReader.ReadFile<Ms1FeatureFile>(filePath).Results;
        List<AnnotatedSpecies> species = new List<AnnotatedSpecies>(features.Count);

        // TODO: File name may have issues here with feature alignment for plots. This parses the file name of the align file, not the mass spec file. 
        var fileName = PfmXplorerUtil.GetFileNameWithoutExtension(filePath.Replace("_ms1.feature", ""));
        foreach (var feature in features)
        {
            var decon = new DeconvolutionFeature(feature.Mass, feature.RetentionTimeApex, feature.RetentionTimeBegin, feature.RetentionTimeEnd,
                Enumerable.Range(feature.ChargeStateMin, feature.ChargeStateMax - feature.ChargeStateMin + 1).ToList(), fileName);
            var specie = new AnnotatedSpecies(decon, feature.Id.ToString());
            species.Add(specie);
        }
        return species;
    }
}

public class FlashDeconvTsvProcessingStrategy : IFileProcessingStrategy
{
    public List<AnnotatedSpecies> ProcessFile(string filePath, string dataset = "")
    {
        var features = FileReader.ReadFile<FlashDeconvTsvFile>(filePath).Results;

        List<AnnotatedSpecies> species = new List<AnnotatedSpecies>(features.Count);
        foreach (var feature in features)
        {
            var decon = new DeconvolutionFeature(feature.MonoisotopicMass, feature.RetentionTimeApex, feature.RetentionTimeBegin, feature.RetentionTimeEnd,
                Enumerable.Range(feature.ChargeStateMin, feature.ChargeStateMax - feature.ChargeStateMin + 1).ToList(), feature.FilePath);
            var specie = new AnnotatedSpecies(decon, feature.FeatureIndex.ToString());
            species.Add(specie);
        }
        return species;
    }
}