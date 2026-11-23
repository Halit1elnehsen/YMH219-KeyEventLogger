namespace YMH219_KeyEventLogger;

public sealed class KeyboardRecorder
{
    private readonly HashSet<Keys> _pressed = [];
    public bool IsRecording { get; private set; }
    public event Action<KeyboardEventRecord>? Recorded;
    public void Start() { _pressed.Clear(); IsRecording = true; }
    public void Stop() { IsRecording = false; _pressed.Clear(); }
    public void KeyDown(Keys key)
    {
        if (IsRecording && _pressed.Add(key)) Recorded?.Invoke(new(DateTime.Now, key.ToString(), "BASMA"));
    }
    public void KeyUp(Keys key)
    {
        if (IsRecording && _pressed.Remove(key)) Recorded?.Invoke(new(DateTime.Now, key.ToString(), "BIRAKMA"));
    }
}
