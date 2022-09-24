﻿using System.Linq;
using System.Threading;
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


    private readonly int[][] _grid;
    private readonly Rectangle[][] _grid_rectangles;

    public ProminentColorSmallWindow() {
        ViewModel = new ProminentColorSmallViewModel();
        ViewModel.LogEvent += ViewModel_LogEvent;

        _grid = new int[3][];
        _grid_rectangles = new Rectangle[3][];

        InitializeComponent();

        for (int x = 0; x < 3; x++) {
            _grid[x] = new int[3];
            _grid_rectangles[x] = new Rectangle[3];
            for (var y = 0; y < 3; y++) {
                _grid[x][y] = 1;
                var rect = new Rectangle();
                rect.Width = 10;
                rect.Height = 10;
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
    }

    private void TryColorInputCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var pos = e.GetPosition((IInputElement)sender);

        var mx = pos.X - 2;
        var my = pos.Y - 2;

        if (mx < 0 || my < 0) return;

        var x = (int)Math.Floor(mx / 10);
        var y = (int)Math.Floor(my / 10);

        var current_value = _grid[x][y];
        var rect = _grid_rectangles[x][y];

        var new_value = current_value == 1 ? 2 : 1;

        var color = new_value == 1 ? Brushes.White : Brushes.Black;

        rect.Fill = color;

        _grid[x][y] = new_value;

        var flat_grid = _grid.SelectMany(a => a).Select(a => (double)a).ToArray();
        var result = ViewModel.BestPrediction(flat_grid);

        if (result == 0) {
            TryColorResultBorder.Background = Brushes.Red;
        } else if (result == 1) {
            TryColorResultBorder.Background = Brushes.White;
        } else if (result == 2) {
            TryColorResultBorder.Background = Brushes.Black;
        } else {
            throw new InvalidOperationException($"Iinvalid result {result}");
        }
        TryColorResultBorder.InvalidateVisual();
    }
}