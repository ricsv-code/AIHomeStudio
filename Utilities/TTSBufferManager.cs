using System.Text;
using System.Timers;

public class TTSBufferManager
{
    private readonly StringBuilder _buffer = new();
    private readonly object _lock = new();
    private System.Timers.Timer _debounceTimer;
    private bool _speaking = false;

    public event EventHandler<string>? OnBufferReady;

    public TTSBufferManager(double debounceMilliseconds = 1500)
    {
        _debounceTimer = new System.Timers.Timer(debounceMilliseconds);
        _debounceTimer.Elapsed += OnDebounceElapsed;
        _debounceTimer.AutoReset = false;
    }

    public void PushToken(string token)
    {
        lock (_lock)
        {
            _buffer.Append(token);
        }
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        string textToSpeak;
        lock (_lock)
        {
            textToSpeak = _buffer.ToString().Trim();
            _buffer.Clear();
        }

        if (!string.IsNullOrWhiteSpace(textToSpeak))
        {
            OnBufferReady?.Invoke(this, textToSpeak);
        }
    }

    public void Reset()
    {
        _debounceTimer.Stop();
        lock (_lock)
        {
            _buffer.Clear();
        }
        _speaking = false;
    }
}
