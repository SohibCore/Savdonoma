using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Data.Licensing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class PasswordResetViewModel : ObservableObject
    {
        private readonly IPasswordResetService _service;

        public string MachineId => _service.MachineId;
        public string RequestCode => _service.RequestCode;

        [ObservableProperty]
        private string ownerUserName = "";

        [ObservableProperty]
        private string resetCode = "";

        [ObservableProperty]
        private string errorMessage = "";

        [ObservableProperty]
        private string infoMessage = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
        private bool isBusy;

        // PasswordBox'lardan code-behind orqali keladi
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";

        // Muvaffaqiyatli tiklangandan keyin yangilangan login
        public string? ResetUserName { get; private set; }

        public event Action? Completed;

        public PasswordResetViewModel(IPasswordResetService service)
        {
            _service = service;
        }

        public async Task InitializeAsync()
        {
            OwnerUserName = await _service.GetOwnerUserNameAsync(CancellationToken.None) ?? "(topilmadi)";
        }

        [RelayCommand]
        private void CopyRequest()
        {
            try
            {
                Clipboard.SetText($"Machine ID: {MachineId}{Environment.NewLine}So'rov kodi: {RequestCode}");
                InfoMessage = "Nusxa olindi. Uni ishlab chiquvchiga yuboring.";
            }
            catch (Exception)
            {
                InfoMessage = "Nusxa olib bo'lmadi, qo'lda ko'chirib oling.";
            }
        }

        private bool CanReset() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanReset))]
        private async Task ResetAsync()
        {
            ErrorMessage = "";

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Yangi parol va uning takrori bir xil emas.";
                return;
            }

            IsBusy = true;
            try
            {
                ResetUserName = await _service.ResetOwnerPasswordAsync(ResetCode, NewPassword, CancellationToken.None);
                Completed?.Invoke();
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Parolni saqlashda xatolik: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
