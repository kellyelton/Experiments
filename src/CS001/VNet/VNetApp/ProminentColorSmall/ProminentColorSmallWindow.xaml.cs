using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text;
using System.IO;

namespace VNetApp.ProminentColorSmall;

public partial class ProminentColorSmallWindow : Window
{
    public ProminentColorSmallViewModel ViewModel {
        get => (ProminentColorSmallViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(ProminentColorSmallViewModel), typeof(ProminentColorSmallWindow), new PropertyMetadata(null));

    private bool _hibernate_after_training = false;

    private readonly int[][] _grid;
    private readonly Rectangle[][] _grid_rectangles;

    private readonly double cell_width;
    private readonly double cell_height;

    private readonly PointCollection _graph_highest_score_points = new();
    private readonly PointCollection _graph_mutation_pool_size_points = new();
    private readonly PointCollection _graph_create_random_net_change_points = new();

    public ProminentColorSmallWindow() {
        ViewModel = new ProminentColorSmallViewModel(1, 3);
        ViewModel.LogEvent += ViewModel_LogEvent;
        ViewModel.NewHighScore += ViewModel_NewHighScore;
        ViewModel.NewHighestScore += ViewModel_NewHighestScore;
        ViewModel.TrainingProgress += ViewModel_TrainingProgress;

        _grid = new int[ViewModel.Columns][];
        _grid_rectangles = new Rectangle[ViewModel.Columns][];

        InitializeComponent();

        ScoreGraph.Points = _graph_highest_score_points;
        MutationPoolGraph.Points = _graph_mutation_pool_size_points;
        CreateRandomNetChangeGraph.Points = _graph_create_random_net_change_points;

        Setting_CheckBox_HibernateAfterTraining.IsChecked = _hibernate_after_training;

        cell_width = 30 / ViewModel.Columns;
        cell_height = 30 / ViewModel.Rows;

        for (int x = 0; x < ViewModel.Columns; x++) {
            _grid[x] = new int[ViewModel.Rows];
            _grid_rectangles[x] = new Rectangle[ViewModel.Rows];
            for (var y = 0; y < ViewModel.Rows; y++) {
                _grid[x][y] = 1;
                var rect = new Rectangle();
                rect.Width = cell_width;
                rect.Height = cell_height;
                rect.Fill = Brushes.White;

                TryColorInputCanvas.Children.Add(rect);

                var cx = x * rect.Width;
                var cy = y * rect.Height;

                rect.SetValue(Canvas.LeftProperty, cx);
                rect.SetValue(Canvas.TopProperty, cy);

                _grid_rectangles[x][y] = rect;
            }
        }
    }

    private void ViewModel_TrainingProgress(object? sender, TrainingProgress e) {
        Dispatcher.InvokeAsync(() => {
            var progress_percent = e.Progress / (double)e.MaxProgress;

            var x = ScoreGraph.ActualWidth * progress_percent;

            { // Score graph
                var highest_score_percent = _highestscore / (double)ViewModel.MaxHighScore;

                var y = (ScoreGraph.ActualHeight - 10) * highest_score_percent;
                y = ScoreGraph.ActualHeight - y - 5;

                var to_point = new Point(x, y);

                _graph_highest_score_points.Add(to_point);

                var c = (Canvas)ScoreGraph.Parent;

                var cur = Canvas.GetLeft(ScoreGraph);

                var poff = c.ActualWidth - x;
            }
            { // Best bot count graph
                var mutation_pool_percent = e.MutationPoolSize / (double)ViewModel.MutationPoolCapacity;

                var y = (MutationPoolGraph.ActualHeight - 10) * mutation_pool_percent;
                y = MutationPoolGraph.ActualHeight - y - 5;

                var to_point = new Point(x, y);

                _graph_mutation_pool_size_points.Add(to_point);

                var c = (Canvas)MutationPoolGraph.Parent;

                var cur = Canvas.GetLeft(MutationPoolGraph);

                var poff = c.ActualWidth - x;
            }
            { // Create rando net chance graph
                var percent = e.CreateRandomNetChance;

                var y = (CreateRandomNetChangeGraph.ActualHeight - 10) * percent;
                y = CreateRandomNetChangeGraph.ActualHeight - y - 5;

                var to_point = new Point(x, y);

                _graph_create_random_net_change_points.Add(to_point);

                var c = (Canvas)CreateRandomNetChangeGraph.Parent;

                var cur = Canvas.GetLeft(CreateRandomNetChangeGraph);

                var poff = c.ActualWidth - x;
            }
        });
    }

    private int _highestscore;
    private int _highscore;

    private void ViewModel_NewHighestScore(object? sender, Net e) {
        _highestscore = e.Score;

        Dispatcher.InvokeAsync(()=>{
            WriteToFile("best_nets.csv", e);
        });

        Dispatcher.InvokeAsync(RunTry);
    }

    private void ViewModel_NewHighScore(object? sender, Net e) {
        _highscore = e.Score;
    }

    private void ViewModel_LogEvent(object? sender, string e) {
        var newp = new Paragraph(new Run(e));

        LogTextBox.Document.Blocks.Add(newp);

        LogTextBox.ScrollToEnd();
    }

    private async void TrainButton_Click(object sender, RoutedEventArgs e) {
        if (ViewModel.IsTraining) {
            ViewModel_LogEvent(this, "Already training");

            return;
        }

        _highestscore = 0;
        _highscore = 0;
        _graph_highest_score_points.Clear();
        _graph_mutation_pool_size_points.Clear();

        await ViewModel.Train(CancellationToken.None);

        await HibernateIfRequested();
    }

    private async Task HibernateIfRequested() {
        if (!_hibernate_after_training) return;

        ViewModel_LogEvent(this, "Triggering system hibernation");

        await Task.Delay(1000);

        try {
            var psi = new ProcessStartInfo("shutdown", "/h") {
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);

            var str = proc.StandardOutput.ReadToEnd();

            ViewModel_LogEvent(this, str);

            proc.WaitForExit();

            if (proc.ExitCode != 0) {
                ViewModel_LogEvent(this, $"Error hibernating: \"{psi.FileName} {psi.Arguments}\" returned a non-zero exit code {proc.ExitCode}");
            }
        } catch (Exception ex) {
            ViewModel_LogEvent(this, "Error hibernating: " + Environment.NewLine + ex.ToString());
        }
    }

    private void TryColorInputCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var pos = e.GetPosition((IInputElement)sender);

        var mx = pos.X - 2;
        var my = pos.Y - 2;

        if (mx < 0 || my < 0) return;

        var x = (int)Math.Floor(mx / cell_width);
        var y = (int)Math.Floor(my / cell_height);

        var current_value = _grid[x][y];
        var rect = _grid_rectangles[x][y];

        var new_value = current_value == 1 ? 2 : 1;

        var color = new_value == 1 ? Brushes.White : Brushes.Black;

        rect.Fill = color;

        _grid[x][y] = new_value;

        RunTry();
    }

    private void RunTry() {
        var flat_grid = _grid.SelectMany(a => a).Select(a => (double)a).ToArray();
        var result = ViewModel.BestPrediction(flat_grid);

        if (result == -1) {
            TryColorResultBorder.Background = Brushes.Red;
        } else if (result == 0) {
            TryColorResultBorder.Background = Brushes.LightGray;
        } else if (result == 1) {
            TryColorResultBorder.Background = Brushes.White;
        } else if (result == 2) {
            TryColorResultBorder.Background = Brushes.Black;
        } else {
            throw new InvalidOperationException($"Invalid result {result}");
        }
        TryColorResultBorder.InvalidateVisual();
    }

    private void WriteToFile(string name, params Net[] nets) {
        var sb = new StringBuilder();

        foreach (var net in nets) {
            foreach (var neuron in net.Neurons) {
                var values = new object[6];
                values[0] = neuron.Id;
                values[1] = $"\"{neuron.Type}\"";
                values[2] = neuron.Bias;

                var inputs_string = "\"";
                if (neuron.Inputs.Count > 0) {
                    foreach (var input in neuron.Inputs) {
                        inputs_string += input.Id.ToString() + ",";
                    }
                    inputs_string = inputs_string[..^1];
                }
                inputs_string += "\"";
                values[3] = inputs_string;

                var input_weights_string = "\"";
                if (neuron.InputWeights.Count > 0) {
                    foreach (var weight in neuron.InputWeights) {
                        input_weights_string += weight.ToString() + ",";
                    }
                    input_weights_string = input_weights_string[..^1];
                }
                input_weights_string += "\"";
                values[4] = input_weights_string;

                var outputs_string = "\"";
                if (neuron.Outputs.Count > 0) {
                    foreach (var output in neuron.Outputs) {
                        outputs_string += output.Id.ToString() + ",";
                    }
                    outputs_string = outputs_string[..^1];
                }
                outputs_string += "\"";
                values[5] = outputs_string;

                var str = string.Join(",", values);

                sb.AppendLine(str);
            }
        }

        File.WriteAllText(name, sb.ToString());
    }

    private void Setting_CheckBox_HibernateAfterTraining_Checked(object sender, RoutedEventArgs e) {
        _hibernate_after_training = true;
    }

    private void Setting_CheckBox_HibernateAfterTraining_Unchecked(object sender, RoutedEventArgs e) {
        _hibernate_after_training = false;
    }
}
