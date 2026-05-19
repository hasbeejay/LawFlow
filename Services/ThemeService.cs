using Microsoft.JSInterop;

namespace LawFlow.Services;

/// <summary>
/// Light/dark theme state with JS interop persistence to localStorage.
/// The initial mode is applied pre-paint by inline script in App.razor.
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _js;
    private bool _isDark;

    public event Action? OnChange;

    public bool IsDark => _isDark;
    public string Mode => _isDark ? "dark" : "light";

    public ThemeService(IJSRuntime js) { _js = js; }

    public async Task InitializeAsync()
    {
        try
        {
            var mode = await _js.InvokeAsync<string>("lfTheme.get");
            _isDark = mode == "dark";
            OnChange?.Invoke();
        }
        catch
        {
            // pre-render or interop not ready — defaults to light
        }
    }

    public async Task ToggleAsync()
    {
        _isDark = !_isDark;
        await _js.InvokeVoidAsync("lfTheme.set", Mode);
        OnChange?.Invoke();
    }

    public async Task SetAsync(bool dark)
    {
        if (_isDark == dark) return;
        _isDark = dark;
        await _js.InvokeVoidAsync("lfTheme.set", Mode);
        OnChange?.Invoke();
    }
}
