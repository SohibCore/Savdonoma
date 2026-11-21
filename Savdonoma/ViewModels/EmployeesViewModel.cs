using System.Windows;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.UserService;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly IUserService _user;
        private readonly ILicenseService _license;
        public bool CanWrite => _license.Current.IsActive;

        public ObservableCollection<UserDto> Employees { get; } = new();
        public IReadOnlyList<RoleOption> Roles { get; } = new[]
        {
            new RoleOption(UserRole.Manager, "Menejer"),
            new RoleOption(UserRole.Sotuvchi, "Sotuvchi"),
        };

        public record RoleOption(UserRole Value, string Name);

        [ObservableProperty]
        private string fullName = "";

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private string password = "";

        // Rahbar o'z login/parolini o'zgartirganda kerak
        [ObservableProperty]
        private string currentPassword = "";

        [ObservableProperty]
        private UserRole selectedRole = UserRole.Sotuvchi;

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool showInactive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditingOwner))]
        [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
        private UserDto? selectedUser;

        /// <summary>
        /// True bo'lsa forma "Tahrirlash" rejimida, false bo'lsa "Yaratish" rejimida.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditingOwner))]
        private bool isEditing;

        // Rahbar o'z ma'lumotlarini tahrirlayapti: rol o'rniga joriy parol so'raladi
        public bool IsEditingOwner => IsEditing && SelectedUser?.IsOwner == true;

        public EmployeesViewModel(IUserService user, ILicenseService license)
        {
            _user = user;
            _license = license;
            _license.StatusChanged += LicenseStatusChanged;
        }

        private void LicenseStatusChanged(object? sender, LicenseState state)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnPropertyChanged(nameof(CanWrite));
                CreateCommand.NotifyCanExecuteChanged();
                UpdateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                ReactivateCommand.NotifyCanExecuteChanged();
            }));
        }

        partial void OnShowInactiveChanged(bool value) => _ = LoadAsync();

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var users = await _user.GetListAsync(ShowInactive, CancellationToken.None);
                Employees.Clear();
                foreach (var employee in users)
                    Employees.Add(employee);
                StatusMessage = "";
                HasError = false;
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Jadvaldan xodimni tanlash — formaga ma'lumotlarini yuklaydi (tahrirlash rejimi).
        /// </summary>
        [RelayCommand]
        private void SelectEmployee(UserDto? employee)
        {
            if (employee is null) return;

            SelectedUser = employee;
            FullName = employee.FullName;
            UserName = employee.UserName;
            Password = "";
            CurrentPassword = "";
            if (!employee.IsOwner)
                SelectedRole = employee.UserRole;
            IsEditing = true;
            StatusMessage = "";
            HasError = false;
        }

        /// <summary>
        /// Tahrirlashni bekor qilish — formani tozalaydi va "Yaratish" rejimiga qaytadi.
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            ClearForm();
        }

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task CreateAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            HasError = false;
            try
            {
                await _user.CreateEmployeeAsync(new CreateUserDto
                {
                    FullName = FullName,
                    UserName = UserName,
                    Password = Password,
                    UserRole = SelectedRole
                }, CancellationToken.None);

                ClearForm();
                await LoadAsync();
                StatusMessage = "Xodim muvaffaqiyatli yaratildi.";
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or LicenseExpiredException or InvalidOperationException)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            catch (DbUpdateException)
            {
                StatusMessage = "Xodimni saqlashda xatolik yuz berdi. Login band bo'lishi mumkin.";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Rahbar o'z ma'lumotlarini litsenziya muddati o'tgan bo'lsa ham o'zgartira oladi
        private bool CanUpdate() => CanWrite || SelectedUser?.IsOwner == true;

        [RelayCommand(CanExecute = nameof(CanUpdate))]
        private async Task UpdateAsync()
        {
            if (SelectedUser is null) return;

            var isOwner = SelectedUser.IsOwner;
            IsBusy = true;
            StatusMessage = "";
            HasError = false;
            try
            {
                await _user.UpdateAsync(new UpdateUserDto
                {
                    Id = SelectedUser.Id,
                    FullName = FullName,
                    UserName = UserName,
                    Password = string.IsNullOrEmpty(Password) ? null : Password,
                    UserRole = isOwner ? null : SelectedRole,
                    CurrentPassword = isOwner ? CurrentPassword : null,
                }, CancellationToken.None);

                ClearForm();
                await LoadAsync();
                StatusMessage = isOwner
                    ? "Ma'lumotlaringiz yangilandi. Keyingi kirishda yangi login va paroldan foydalaning."
                    : "Xodim muvaffaqiyatli yangilandi.";
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or LicenseExpiredException or InvalidOperationException)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            catch (DbUpdateException)
            {
                StatusMessage = "Xodimni yangilashda xatolik yuz berdi. Login band bo'lishi mumkin.";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Qator ichidagi tugma xodimni parametr sifatida uzatadi: tugma bosilganda qator tanlanmaydi,
        // shuning uchun faqat SelectedUser'ga tayanib bo'lmaydi.
        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task DeleteAsync(UserDto? employee)
        {
            employee ??= SelectedUser;
            if (employee is null || employee.IsOwner) return;

            var result = MessageBox.Show(
                $"'{employee.FullName}' xodimini o'chirmoqchimisiz?",
                "O'chirishni tasdiqlang",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = "";
            HasError = false;
            try
            {
                var deleteResult = await _user.DeleteAsync(employee.Id, CancellationToken.None);

                ClearForm();
                await LoadAsync();
                StatusMessage = deleteResult == UserDeleteResult.Deactivated
                    ? "Xodimning sotuvlari bor, shuning uchun u o'chirilmadi, nofaol qilindi. Endi u tizimga kira olmaydi. " +
                      "Uni \"Nofaollarni ko'rsatish\" orqali qayta faollashtirish mumkin."
                    : "Xodim muvaffaqiyatli o'chirildi.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task ReactivateAsync(UserDto? employee)
        {
            if (employee is null || employee.IsActive) return;

            IsBusy = true;
            StatusMessage = "";
            HasError = false;
            try
            {
                await _user.ReactivateAsync(employee.Id, CancellationToken.None);

                ClearForm();
                await LoadAsync();
                StatusMessage = $"'{employee.FullName}' qayta faollashtirildi. Endi u tizimga kira oladi.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearForm()
        {
            FullName = "";
            UserName = "";
            Password = "";
            CurrentPassword = "";
            SelectedRole = UserRole.Sotuvchi;
            SelectedUser = null;
            IsEditing = false;
            StatusMessage = "";
            HasError = false;
        }
    }
}
