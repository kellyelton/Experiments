using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace VNetApp.ProminentColorSmall;

public class ProminentColorSmallViewModel : ViewModel
{
    public double CurrentConfidence {
        get => _confidence;
        set => SetAndNotify(ref _confidence, value);
    }

    public bool IsTraining {
        get => _isTraining;
        set => SetAndNotify(ref _isTraining, value, additional_properties: nameof(IsNotTraining));
    }

    public bool IsNotTraining => !_isTraining;

    public Net Best { get; set; }

    public int Best_Score { get; set; } = 0;

    public int Rows { get; }

    public int Columns { get; }

    private double _confidence;
    private bool _isTraining;

    public ProminentColorSmallViewModel(int grid_rows, int grid_columns) {
        Best = CreateNet();
        Rows = grid_rows;
        Columns = grid_columns;
    }

    public async Task Train(CancellationToken cancellation) {
        Application.Current.Dispatcher.VerifyAccess();

        try {
            IsTraining = true;

            LogInfo("Training starting...");

            try {
                cancellation.ThrowIfCancellationRequested();

                await Task.Run(() => {
                    LogInfo("Training");

                    Train2(cancellation);
                }, cancellation);

                LogInfo("Done Training");
            } catch (OperationCanceledException) {
                LogInfo("Training cancelled");
            } catch (Exception ex) {
                LogError("Training Error");
            }
        } finally {
            IsTraining = false;
        }
    }

    private class DescScore : IComparer<Net>
    {
        public int Compare(Net? x, Net? y) {
            if (x.Score == y.Score) return 0;

            if (y.Score < x.Score) return -1;

            return 1;
        }
    }

    private static readonly IComparer<Net> DescendingScore = new DescScore();

    protected virtual void Train2(CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();

        const int runs = 10000;

        var best_brains = new SortedSet<Net>(DescendingScore);

        var best_brain = CreateNet();
        var best_brain_2 = CreateNet();

        best_brains.Add(best_brain);
        best_brains.Add(best_brain_2);

        for (var i = 0; i < runs; i++) {
            var tasks = Enumerable
                .Range(0, 500)
                .Select(_ => {
                    if (best_brains.Count > 10 && Random.Shared.NextDouble() > 0.4) {
                        var ri = Random.Shared.Next(0, best_brains.Count);
                        var r = best_brains.Skip(ri).Take(1).Single();
                        return MutateNet(r);
                    } else {
                        return CreateNet();
                    }
                })
                .Select(brain => Task.Run(() => {
                    var score = TestNet(brain);
                    return (Score: score, Net: brain);
                }))
                .ToArray()
            ;

            Task.WaitAll(tasks, cancellation);

            foreach (var task in tasks) {
                var test_result = task.Result;

                if (test_result.Score > best_brains.Min(b => b.Score)) {
                    best_brains.Add(test_result.Net);
                }

                while (best_brains.Count > 100) {
                    var k = best_brains.Last();
                    best_brains.Remove(k);
                }

                if (test_result.Score > best_brain.Score) {
                    LogInfo("New 1st Place Record: " + test_result.Score + " gen-" + test_result.Net.Generation);
                    best_brain_2 = best_brain;

                    best_brain = test_result.Net;
                } else if (test_result.Score > best_brain_2.Score) {
                    LogInfo("New 2nd Place Record: " + test_result.Score + " gen-" + test_result.Net.Generation);
                    best_brain_2 = test_result.Net;
                }

                if (test_result.Score > Best_Score) {
                    LogInfo("New Mega Best: " + test_result.Score + " gen-" + test_result.Net.Generation);
                    Best = test_result.Net;
                    Best_Score = test_result.Score;
                }
            }

            if ((i % 50) == 0) {
                LogInfo($"{i}/{runs}");
            }
        }

        LogInfo($"Best Score: {best_brain.Score} gen-{best_brain.Generation}");

        var ops = new JsonSerializerOptions() {
            WriteIndented = true
        };
        ops.ReferenceHandler = ReferenceHandler.Preserve;

        var obj = new {
            best_brain,
            best_brain_2,
        };

        //var str = JsonSerializer.Serialize(obj, ops);

        //File.WriteAllText("last_run.json", str);
    }

    protected virtual int TestNet(Net net) {
        var training_data = TrainingData.Load_DataSet_1(Rows, Columns, 100);

        var scores = new List<double>();

        var ca = 0;
        var cb = 0;
        foreach (var data in training_data) {
            var r = RunPrediction(net, data.Inputs);

            if (r == data.ExpectedResult) {
                scores.Add(1);
                if (r == 1) {
                    ca++;
                } else if (r == 2) {
                    cb++;
                } else throw new InvalidOperationException("hafl");
            } else {
                scores.Add(0);
            }
        }

        var avg_score = scores.Average();

        if (ca == 0 || cb == 0)
            avg_score = 0;

        var total_score = (int)(1000 * avg_score);

        net.Score = total_score;

        return total_score;
    }

    public int BestPrediction(double[] inputs) {
        var best = Best;

        if (inputs.Length > best.InputNeurons.Length) throw new ArgumentOutOfRangeException("Input is too long.");

        return RunPrediction(best, inputs);
    }

    protected virtual int RunPrediction(Net net, double[] inputs) {
        //TODO: Predict in a loop until the outputs aren't NaN. Maybe max of 10 tries. Sometimes it can take a few pushes for data to reach the output (maybe, should test this)
        // Could even possibly have the net record the number of inital pushes before output happens
        // Maybe even bake this part into the Net.Predict function
        // Maybe even after hitting the activation threshold, x number of predictions after that and average

        var outputs = net.Predict(inputs);
        net.Reset();

        var a = false;
        var b = false;

        if (double.IsNaN(outputs[0]) == false && outputs[0] > 0) {
            a = true;
        }

        if (double.IsNaN(outputs[1]) == false && outputs[1] > 0) {
            b = true;
        }

        if (a == b) return 0;

        if (a) return 1;

        if (b) return 2;

        throw new InvalidOperationException("asdf");
    }

    protected virtual Net CreateNet() {
        return Net.Random(Rows * Columns, 2, 40);
    }

    protected virtual Net MutateNet(Net parent) {
        return Net.Mutate(parent);
    }

    protected virtual Net MutateNets(Net parenta, Net parentb) {
        throw new NotImplementedException();
    }
}
