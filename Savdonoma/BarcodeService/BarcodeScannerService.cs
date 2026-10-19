using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Controls;
using Savdonoma.BarcodeService;

public sealed class BarcodeScannerService : IBarcodeScannerService
{
    private const int MaxGapMs = 50;   // belgilar orasidagi eng katta masofa
    private const int MinLength = 4;   // bundan qisqa kod emas

    private long _lastTick;
    private UIElement? _root;
    private readonly StringBuilder _buffer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public event Action<string>? BarcodeScanned;

    public void Attach(UIElement root)
    {
        if (ReferenceEquals(_root, root))
            return;

        if (_root != null)
        {
            _root.PreviewTextInput -= OnTextInput;
            _root.PreviewKeyDown -= OnKeyDown;
        }

        _root = root;
        root.PreviewTextInput += OnTextInput;
        root.PreviewKeyDown += OnKeyDown;
    }

    public void Simulate(string code) => BarcodeScanned?.Invoke(code);

    private void OnTextInput(object sender, TextCompositionEventArgs e)
    {
        long now = _clock.ElapsedMilliseconds;

        if (e.Text.Length == 0 || e.Text.Any(x => !char.IsDigit(x)))
        {
            _buffer.Clear();
            _lastTick = now;
            return;
        }

        // oldingi belgidan beri uzoq o'tgan bo'lsa, bu skaner emas, qaytadan boshlaymiz
        if (_buffer.Length > 0 && now - _lastTick > MaxGapMs)
            _buffer.Clear();

        _buffer.Append(e.Text);
        _lastTick = now;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        long now = _clock.ElapsedMilliseconds;
        bool isScan = _buffer.Length >= MinLength && now - _lastTick <= MaxGapMs;

        if (!isScan)
        {
            _buffer.Clear();
            return;
        }

        string code = _buffer.ToString();
        _buffer.Clear();
        e.Handled = true;

        // kursor TextBox'da bo'lsa, unga tushib qolgan kodni o'chiramiz
        if (Keyboard.FocusedElement is TextBox tb && tb.Text.EndsWith(code))
        {
            tb.Text = tb.Text[..^code.Length];
            tb.CaretIndex = tb.Text.Length;
        }

        BarcodeScanned?.Invoke(code);
    }
}