using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using VNet;

namespace VNetApp;

public partial class ColorStringToRGBAViewModel : ViewModel
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

    public double Best_Score { get; set; } = 0d;

    private double _confidence;
    private bool _isTraining;

    public ColorStringToRGBAViewModel() {
        Best = Net.Random();
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

    private class DescDoubles : IComparer<double>
    {
        public int Compare(double x, double y) {
            if (x == y) return 0;

            if (y < x) return -1;

            return 1;
        }
    }

    private static readonly IComparer<double> DescendingDoubles = new DescDoubles();

    protected virtual void Train2(CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();

        const int runs = 500;

        var best_brains = new SortedDictionary<double, Net>(DescendingDoubles);

        var best_brain = CreateNet();
        var best_brain_score = 0.001d;

        var best_brain_2 = CreateNet();
        var best_brain_2_score = 0d;

        best_brains.Add(best_brain_score, best_brain);
        best_brains.Add(best_brain_2_score, best_brain_2);

        var braina = CreateNet();
        var brainb = CreateNet();

        for (var i = 0; i < runs; i++) {
            var tasks = Enumerable
                .Range(0, 500)
                .Select(_ => {
                    if (best_brains.Count > 10 && Random.Shared.NextDouble() > 0.4) {
                        var ri = Random.Shared.Next(0, best_brains.Count);
                        var r = best_brains.Skip(ri).Take(1).Single();
                        return MutateNet(r.Value);
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

                if (best_brains.ContainsKey(test_result.Score)) {
                    best_brains[test_result.Score] = test_result.Net;
                } else if (test_result.Score > best_brains.Keys.Min()) {
                    best_brains.Add(test_result.Score, test_result.Net);
                }

                while (best_brains.Count > 100) {
                    var k = best_brains.Last().Key;
                    best_brains.Remove(k);
                }

                if (test_result.Score > best_brain_score) {
                    LogInfo("New 1st Place Record: " + test_result.Score + " gen-" + test_result.Net.Generation);
                    best_brain_2 = best_brain;
                    best_brain_2_score = best_brain_score;

                    best_brain = test_result.Net;
                    best_brain_score = test_result.Score;
                } else if (test_result.Score > best_brain_2_score) {
                    LogInfo("New 2nd Place Record: " + test_result.Score + " gen-" + test_result.Net.Generation);
                    best_brain_2 = test_result.Net;
                    best_brain_2_score = test_result.Score;
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

        LogInfo($"Best Score: {best_brain_score} gen-{best_brain.Generation}");

        var ops = new JsonSerializerOptions() {
            WriteIndented = true
        };
        ops.ReferenceHandler = ReferenceHandler.Preserve;

        var obj = new {
            best_brain,
            best_brain_score,
            best_brain_2,
            best_brain_2_score
        };

        var str = JsonSerializer.Serialize(obj, ops);

        File.WriteAllText("last_run.json", str);
    }

    protected virtual double TestNet(Net net) {
        var training_data = TrainingData.Cached_DataSet_2.Value;

        var scores = new List<double>();

        foreach (var data in training_data) {
            var inputs = data.Name
                .ToLower()
                .ToCharArray()
                .Select(c => (double)c)
                .ToArray()
            ;

            var outputs = net.Predict(inputs);
            net.Reset();

            var rdiff = Math.Abs(outputs[0] - data.R);
            var gdiff = Math.Abs(outputs[1] - data.G);
            var bdiff = Math.Abs(outputs[2] - data.B);

            var adiff = (rdiff + gdiff + bdiff) / 3;

            var score = 1 - (adiff);

            scores.Add(score);
        }

        var total_score = scores.Average();

        return total_score;
    }

    public (int r, int g, int b) BestPrediction(string input) {
        var best = Best;

        //best = Net.Random();

        if (input.Length > best.InputNeurons.Length) throw new ArgumentOutOfRangeException("Input is too long.");

        var inputs = input
            .ToLower()
            .ToCharArray()
            .Select(c => (double)c)
            .ToArray()
        ;

        //TODO: Predict in a loop until the outputs aren't NaN. Maybe max of 10 tries. Sometimes it can take a few pushes for data to reach the output (maybe, should test this)
        // Could even possibly have the net record the number of inital pushes before output happens
        // Maybe even bake this part into the Net.Predict function
        // Maybe even after hitting the activation threshold, x number of predictions after that and average
        var outputs = best.Predict(inputs);
        best.Reset();

        var r = (int)(outputs[0] * 255);
        var g = (int)(outputs[1] * 255);
        var b = (int)(outputs[2] * 255);

        return (r, g, b);
    }

    protected virtual Net CreateNet() {
        return Net.Random();
    }

    protected virtual Net MutateNet(Net parent) {
        return Net.Mutate(parent);
    }

    protected virtual Net MutateNets(Net parenta, Net parentb) {
        throw new NotImplementedException();
    }
}
