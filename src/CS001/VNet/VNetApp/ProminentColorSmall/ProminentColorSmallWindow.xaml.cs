using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

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

    public ProminentColorSmallWindow() {
        ViewModel = new ProminentColorSmallViewModel(1, 3);
        ViewModel.LogEvent += ViewModel_LogEvent;
        ViewModel.NewHighScore += ViewModel_NewHighScore;
        ViewModel.NewHighestScore += ViewModel_NewHighestScore;
        ViewModel.TrainingProgress += ViewModel_TrainingProgress;

        _grid = new int[ViewModel.Columns][];
        _grid_rectangles = new Rectangle[ViewModel.Columns][];

        InitializeComponent();

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
            var myEllipse = new Ellipse {
                Fill = Brushes.DarkSlateBlue,
                StrokeThickness = 1,
                Stroke = Brushes.DarkSlateBlue,
                Width = 1,
                Height = 1
            };

            var progress_percent = e.Progress / (double)e.MaxProgress;

            var x = ScoreGraph.ActualWidth * progress_percent;

            var highest_score_percent = _highestscore / (double)ViewModel.MaxHighScore;

            var y = ScoreGraph.ActualHeight * highest_score_percent;
            y = ScoreGraph.ActualHeight - y;

            Canvas.SetTop(myEllipse, y);
            Canvas.SetLeft(myEllipse, x);

            ScoreGraph.Children.Add(myEllipse);
        });
    }

    private int _highestscore;
    private int _highscore;

    private void ViewModel_NewHighestScore(object? sender, Net e) {
        _highestscore = e.Score;
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

    private void Setting_CheckBox_HibernateAfterTraining_Checked(object sender, RoutedEventArgs e) {
        _hibernate_after_training = true;
    }

    private void Setting_CheckBox_HibernateAfterTraining_Unchecked(object sender, RoutedEventArgs e) {
        _hibernate_after_training = false;
    }
}
