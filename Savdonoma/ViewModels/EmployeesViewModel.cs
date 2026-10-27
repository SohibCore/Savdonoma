using System.Windows;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Data.Services.UserService;

namespace Savdonoma.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly IUserService _user;
        private readonly ILicenseService _license;
        public bool CanWrite => _license.Current.IsActive;

        public ObservableCollection<UserDto> Employees { get; } = new();
        public Array Roles { get; } = new[] { UserRole.Manager, UserRole.Sotuvchi };

        [ObservableProperty]
        private string fullName = "";

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        private UserRole selectedRole = UserRole.Sotuvchi;

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private UserDto? selectedUser;

        /// <summary>
        /// True bo'lsa forma "Tahrirlash" rejimida, false bo'lsa "Yaratish" rejimida.
        /// </summary>
        [ObservableProperty]
        private bool isEditing;

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
            }));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var users = await _user.GetListAsync(CancellationToken.None);
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
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            catch (LicenseExpiredException ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            catch (InvalidOperationException ex)
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

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task UpdateAsync()
        {
            if (SelectedUser is null) return;

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
                    UserRole = SelectedRole
                }, CancellationToken.None);

                ClearForm();
                await LoadAsync();
                StatusMessage = "Xodim muvaffaqiyatli yangilandi.";
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            catch (LicenseExpiredException ex)
            {
                StatusMessage = ex.Message;
                HasError = true;
            }
            catch (InvalidOperationException ex)
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

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task DeleteAsync()
        {
            if (SelectedUser is null) return;

            var result = MessageBox.Show(
                $"'{SelectedUser.FullName}' xodimini o'chirmoqchimisiz?",
                "O'chirishni tasdiqlang",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = "";
            HasError = false;
            try
            {
                await _user.DeleteAsync(SelectedUser.Id, CancellationToken.None);

                ClearForm();
                await LoadAsync();
                StatusMessage = "Xodim muvaffaqiyatli o'chirildi.";
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
            SelectedRole = UserRole.Sotuvchi;
            SelectedUser = null;
            IsEditing = false;
            StatusMessage = "";
            HasError = false;
        }
    }
}
