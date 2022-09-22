using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VNetApp;

public partial class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual bool SetAndNotify<T>(ref T field, T value, [CallerMemberName] string? property_name = null, params string[] additional_properties) {
        if (!Set(ref field, value)) return false;

        Notify(property_name, additional_properties);

        return true;
    }

    protected virtual bool Set<T>(ref T field, T value) {
        if (Equals(field, value)) return false;

        field = value;

        return true;
    }

    protected virtual void Notify(string? property_name, params string[] additional_properties) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));

        foreach (var ap in additional_properties) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ap));
        }
    }
}

public partial class ViewModel {
    public event EventHandler<string>? LogEvent;

    protected virtual void LogInfo(string message) {
        Application.Current.Dispatcher.InvokeAsync(() => {
            LogEvent?.Invoke(this, message);
        });
    }

    protected virtual void LogError(string message) {
        Application.Current.Dispatcher.InvokeAsync(() => {
            LogEvent?.Invoke(this, message);
        });
    }
}
