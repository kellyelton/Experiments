using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;

namespace VNetApp;

public partial class ColorStringToRGBAWindow : Window
{
    public ColorStringToRGBAViewModel ViewModel {
        get => (ColorStringToRGBAViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(ColorStringToRGBAViewModel), typeof(ColorStringToRGBAWindow), new PropertyMetadata(null));

    public ColorStringToRGBAWindow() {
        ViewModel = new ColorStringToRGBAViewModel();
        ViewModel.LogEvent += ViewModel_LogEvent;

        InitializeComponent();
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

    private void TryButton_Click(object sender, RoutedEventArgs e) {
        var input = TryInput.Text;

        var color = ViewModel.BestPrediction(input);

        TryColorBorder.Background = new SolidColorBrush(Color.FromRgb((byte)color.r, (byte)color.g, (byte)color.b));

        ViewModel_LogEvent(this, $"Predicted color ({color.r}, {color.g}, {color.b})");
    }

    private void TryInput_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            TryButton_Click(this, e);
        } else {
            TryButton_Click(this, e);
        }
    }
}
