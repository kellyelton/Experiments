using System.Collections.Generic;
using System.Linq;

namespace VNetApp.ProminentColorSmall;

public class TrainingData
{
    public double[] Inputs { get; init; } = Array.Empty<double>();

    public int ExpectedResult { get; init; } = 0;

    //public static readonly Lazy<TrainingData[]> Cached_DataSet_1 = new(() => Load_DataSet_1().ToArray());

    public static IEnumerable<TrainingData> Load_DataSet_1() {
        for (var i = 0; i < 100; i++) {
            var input = Enumerable
                .Range(0, 3 * 3)
                .Select(_ => (double)Random.Shared.Next(1, 3))
                .ToArray()
            ;

            var result = input.Count(f => f == 1) >= 5
                ? 1
                : 2
            ;

            yield return new TrainingData {
                Inputs = input,
                ExpectedResult = result
            };
        }
    }
}