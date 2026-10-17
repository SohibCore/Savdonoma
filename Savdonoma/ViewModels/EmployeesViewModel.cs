using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.UserService;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace Savdonoma.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly IUserService _userService;
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

        public EmployeesViewModel(IUserService userService, ILicenseService license)
        {
            _userService = userService;
            _license = license;
            _license.StatusChanged += LicenseStatusChanged;
        }

        private void LicenseStatusChanged(object? sender, LicenseState state)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnPropertyChanged(nameof(CanWrite));
                CreateCommand.NotifyCanExecuteChanged();
            }));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var users = await _userService.GetListAsync(CancellationToken.None);
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

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task CreateAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            HasError = false;
            try
            {
                await _userService.CreateEmployeeAsync(
                    new CreateUserDto
                    {
                        FullName = FullName,
                        UserName = UserName,
                        Password = Password,
                        UserRole = SelectedRole
                    },
                    CancellationToken.None);

                FullName = "";
                UserName = "";
                Password = "";
                SelectedRole = UserRole.Sotuvchi;
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
                StatusMessage = "Xodimni saqlashda xatolik yuz berdi. Login band bo‘lishi mumkin.";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
