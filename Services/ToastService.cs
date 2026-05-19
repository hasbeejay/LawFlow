namespace LawFlow.Services;

public enum ToastSeverity { Info, Success, Warning, Danger }

public class Toast
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; init; } = "";
    public ToastSeverity Severity { get; init; } = ToastSeverity.Info;
    public int DurationMs { get; init; } = 4200;
}

/// <summary>
/// In-process toast bus. Components subscribe via OnChange and render the active list.
/// </summary>
public class ToastService
{
    private readonly List<Toast> _toasts = new();
    public IReadOnlyList<Toast> Toasts => _toasts;
    public event Action? OnChange;

    public void Show(string message, ToastSeverity severity = ToastSeverity.Info, int durationMs = 4200)
    {
        var t = new Toast { Message = message, Severity = severity, DurationMs = durationMs };
        _toasts.Add(t);
        OnChange?.Invoke();
        _ = Task.Delay(durationMs).ContinueWith(_ => Dismiss(t.Id));
    }

    public void Info(string message)    => Show(message, ToastSeverity.Info);
    public void Success(string message) => Show(message, ToastSeverity.Success);
    public void Warn(string message)    => Show(message, ToastSeverity.Warning);
    public void Error(string message)   => Show(message, ToastSeverity.Danger);

    public void Dismiss(Guid id)
    {
        var t = _toasts.FirstOrDefault(x => x.Id == id);
        if (t != null)
        {
            _toasts.Remove(t);
            OnChange?.Invoke();
        }
    }
}
