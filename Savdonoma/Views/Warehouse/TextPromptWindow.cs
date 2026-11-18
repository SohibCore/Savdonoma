using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Savdonoma.Views.Warehouse
{
    // Oddiy matn so'raydigan oyna (masalan, bekor qilish sababi). XAML'siz, kod bilan yaratiladi.
    public class TextPromptWindow : Window
    {
        private readonly TextBox _input;

        private TextPromptWindow(string title, string message)
        {
            Title = title;
            Width = 460;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));

            _input = new TextBox { MinHeight = 60, TextWrapping = TextWrapping.Wrap, AcceptsReturn = false, Margin = new Thickness(0, 8, 0, 12), Padding = new Thickness(6) };

            var ok = new Button { Content = "Tasdiqlash", IsDefault = true, Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(0, 0, 8, 0) };
            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_input.Text))
                {
                    _input.Focus();
                    return;
                }
                DialogResult = true;
            };

            var cancel = new Button { Content = "Bekor qilish", IsCancel = true, Padding = new Thickness(16, 6, 16, 6) };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(_input);
            panel.Children.Add(buttons);
            Content = panel;

            Loaded += (_, _) => _input.Focus();
        }

        // Tasdiqlansa kiritilgan matn, aks holda null
        public static string? Ask(Window owner, string title, string message)
        {
            var window = new TextPromptWindow(title, message) { Owner = owner };
            return window.ShowDialog() == true ? window._input.Text.Trim() : null;
        }
    }
}
