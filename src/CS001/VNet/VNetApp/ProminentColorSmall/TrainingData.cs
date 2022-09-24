using System.Linq;

namespace VNetApp.ProminentColorSmall;

public class TrainingData
{
    public double[] Inputs { get; init; } = Array.Empty<double>();

    public int ExpectedResult { get; init; } = 0;

    //public static readonly Lazy<TrainingData[]> Cached_DataSet_1 = new(() => Load_DataSet_1().ToArray());

    public static TrainingData[] Load_DataSet_1(int rows, int columns, int amount) {
        var total_cells = rows * columns;
        var total_iterations = amount * total_cells;

        var result = new TrainingData[amount];
        var result_i = 0;

        var one_count = 0;
        var two_count = 0;
        var buffer_length = 0;
        var buffer = new double[total_cells];
        for (var i = 0; i < total_iterations; i++) {
            var val = Random.Shared.Next(1, 3);
            if (val == 1) {
                one_count++;
            } else {
                two_count++;
            }

            buffer[buffer_length++] = val;

            if (buffer_length == total_cells) {
                int expected_result;
                if (one_count == two_count) {
                    expected_result = 0;
                } else if (one_count > two_count) {
                    expected_result = 1;
                } else {
                    expected_result = 2;
                }

                buffer_length = 0;
                one_count = 0;
                two_count = 0;

                var data = new TrainingData() {
                    Inputs = buffer.ToArray(),
                    ExpectedResult = expected_result
                };

                result[result_i++] = data;
            }
        }

        return result;
    }
}