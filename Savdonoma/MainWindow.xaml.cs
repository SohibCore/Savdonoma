using Savdonoma.ViewModels;

namespace Savdonoma
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow(MainViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}