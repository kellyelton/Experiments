﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace VNetApp.ProminentColorSmallMSAI;

public class ProminentColorSmallMSAIViewModel : ViewModel
{
    public event EventHandler<Net> NewHighestScore;
    public event EventHandler<Net> NewHighScore;
    public event EventHandler<TrainingProgress> TrainingProgress;

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

    public int MaxHighScore { get; } = 20000;

    public int MutationPoolCapacity { get; } = 100;

    public int Rows { get; }

    public int Columns { get; }

    private double _confidence;
    private bool _isTraining;

    public ProminentColorSmallMSAIViewModel(int grid_rows, int grid_columns) {
        Rows = grid_rows;
        Columns = grid_columns;
        Best = CreateNet();
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

        const int runs = 1000 * 100;

        var best_brains = new SortedSet<Net>(DescendingScore);

        var best_brain = CreateNet();
        var best_brain_2 = CreateNet();

        best_brains.Add(best_brain);
        best_brains.Add(best_brain_2);

        var progress = new TrainingProgress(runs);

        Fire_TrainingProgress(progress);

        for (var i = 0; i < runs; i++) {
            var percent_complete = (double)i / runs;
            var create_rnd_net_threshold = percent_complete * 3;// * 0.9d;

            //create_rnd_net_threshold = 1 - create_rnd_net_threshold;
            //create_rnd_net_threshold = Math.Exp(-create_rnd_net_threshold);

            //create_rnd_net_threshold = Math.Min(1, Math.Pow(percent_complete * 4, 2));
            //create_rnd_net_threshold = Math.Min(1, Math.Sin(percent_complete * 90));

            //var freq = 4;

            //var a = 1 * (0.5 + Math.Sin(2 * Math.PI * freq * (percent_complete - 0.1)));
            //create_rnd_net_threshold = a;

            if (create_rnd_net_threshold > 1) create_rnd_net_threshold = 1;
            if (create_rnd_net_threshold < 0) create_rnd_net_threshold = 0;

            if (create_rnd_net_threshold > 1) throw new InvalidOperationException("feljafw");

            var tasks = Enumerable
                .Range(0, 100)
                .Select(_ => {
                    var r = Random.Shared.NextDouble();

                    if (best_brains.Count == 0 || r > create_rnd_net_threshold) {
                        return CreateNet();
                    } else {
                        var ri = Random.Shared.Next(0, best_brains.Count);
                        var parent = best_brains.Skip(ri).Take(1).Single();
                        return MutateNet(parent);
                    }
                    //if (best_brains.Count > 10 && Random.Shared.NextDouble() > 0.1) {
                    //    var ri = Random.Shared.Next(0, best_brains.Count);
                    //    var r = best_brains.Skip(ri).Take(1).Single();
                    //    return MutateNet(r);
                    //} else {
                    //    return CreateNet();
                    //}
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

                if (test_result.Score >= best_brains.Min.Score) {
                    best_brains.Add(test_result.Net);

                    Fire_NewHighScore(test_result.Net);
                }

                while (best_brains.Count > MutationPoolCapacity) {
                    var k = best_brains.Last();
                    best_brains.Remove(k);
                }

                if (test_result.Score > best_brain.Score) {
                    LogInfo("New 1st Place Record: " + test_result.Score + " gen-" + test_result.Net.Generation + ": " + test_result.Net.Neurons.Length + "x" + test_result.Net.Neurons.Sum(n => n.Outputs.Count));
                    best_brain_2 = best_brain;

                    best_brain = test_result.Net;

                    Fire_NewHighestScore(test_result.Net);
                } else if (test_result.Score > best_brain_2.Score) {
                    LogInfo("New 2nd Place Record: " + test_result.Score + " gen-" + test_result.Net.Generation + ": " + test_result.Net.Neurons.Length + "x" + test_result.Net.Neurons.Sum(n => n.Outputs.Count));
                    best_brain_2 = test_result.Net;
                }

                if (test_result.Score > Best_Score) {
                    LogInfo("New Mega Best: " + test_result.Score + " gen-" + test_result.Net.Generation + ": " + test_result.Net.Neurons.Length + "x" + test_result.Net.Neurons.Sum(n => n.Outputs.Count));
                    Best = test_result.Net;
                    Best_Score = test_result.Score;
                }
            }

            progress.Progress = i;
            progress.MutationPoolSize = best_brains.Count;
            progress.CreateRandomNetChance = create_rnd_net_threshold;

            Fire_TrainingProgress(progress);
        }

        LogInfo($"Best Score: {best_brain.Score} gen-{best_brain.Generation}" + ": " + best_brain.Neurons.Length + "x" + best_brain.Neurons.Sum(n => n.Outputs.Count));

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

        var c_zero = 0;
        var c_one = 0;
        var c_two = 0;
        foreach (var data in training_data) {
            var r = RunPrediction(net, data.Inputs);

            if (r == data.ExpectedResult) {
                scores.Add(1);
                if (r == 0) c_zero++;
                else if (r == 1) c_one++;
                else if (r == 2) c_two++;
                else throw new InvalidOperationException("hafl");
            } else {
                scores.Add(0);
            }
        }

        var avg_score = scores.Average();

        if (c_zero == 0 && c_one == 0 || c_zero == 0 && c_two == 0 || c_one == 0 && c_two == 0)
            avg_score /= 10;

        var total_score = (int)(MaxHighScore * avg_score);
        var size_deduction = Math.Min(40, net.Size);

        total_score -= size_deduction;

        if (total_score < 0) total_score = 0;

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

        //LearningModelDevice device = null;



        var outputs = net.Predict(inputs);
        net.Reset();

        var a = false;
        var b = false;

        if (double.IsNaN(outputs[0]) == false && outputs[0] > 0.5) {
            a = true;
        }

        if (double.IsNaN(outputs[1]) == false && outputs[1] > 0.5) {
            b = true;
        }

        // Half inputs are 1, half inputs are 2
        if (a && b) return 0;

        // Both false, invalid result
        if (a == b) return -1;

        // Most or all inputs are 1
        if (a) return 1;

        // Most or all inputs are 2
        if (b) return 2;

        throw new InvalidOperationException("asdf");
    }

    protected virtual Net CreateNet() {
        var net = Net.Random(Rows * Columns, 2, 30);

        return net;
    }

    protected virtual Net MutateNet(Net parent) {
        return Net.Mutate(parent);
    }

    protected virtual Net MutateNets(Net parenta, Net parentb) {
        throw new NotImplementedException();
    }

    protected virtual void Fire_NewHighScore(Net net) {
        Task.Run(() => {
            var handlers = NewHighScore?.GetInvocationList() ?? Array.Empty<EventHandler>();

            foreach (var handler in handlers.Cast<EventHandler<Net>>()) {
                try {
                    handler.Invoke(this, net);
                } catch { }
            }
        });
    }

    protected virtual void Fire_NewHighestScore(Net net) {
        Task.Run(() => {
            var handlers = NewHighestScore?.GetInvocationList() ?? Array.Empty<EventHandler>();

            foreach (var handler in handlers.Cast<EventHandler<Net>>()) {
                try {
                    handler.Invoke(this, net);
                } catch { }
            }
        });
    }

    protected virtual void Fire_TrainingProgress(TrainingProgress progress) {
        try {
            TrainingProgress?.Invoke(this, progress);
        } catch { }
    }
}