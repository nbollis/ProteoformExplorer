using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProteoformExplorer.Core;

public static class Extensions
{
    public static List<Datum> RollingAverage(this List<Datum> inputData, int toAverage)
    {
        if (toAverage < 1)
            return inputData;
        var outputData = new List<Datum>();
        for (int i = 0; i < inputData.Count; i++)
        {
            if (i < toAverage)
            {
                outputData.Add(inputData[i]);
                continue;
            }
            if (i > inputData.Count - toAverage)
            {
                outputData.Add(inputData[i]);
                continue;
            }
            var sum = 0.0;
            for (int j = 0; j < toAverage; j++)
            {
                sum += inputData[i - j].Y ?? 0;
            }
            var average = sum / toAverage;
            outputData.Add(new Datum(inputData[i].X, average, inputData[i].Z, inputData[i].Label, inputData[i].Weight));
        }
        return outputData;
    }
}
