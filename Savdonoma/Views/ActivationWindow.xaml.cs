using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class ActivationWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly LicenseViewModel _vm;

        public ActivationWindow(LicenseViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;

            vm.Activated += OnActivated;
            Closed += (_, _) => vm.Activated -= OnActivated;   // oyna yopilgach obunani olib tashlash
        }

        private void OnActivated() => DialogResult = true;
    }
}