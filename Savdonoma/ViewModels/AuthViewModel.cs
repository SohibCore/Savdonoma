using Savdonoma.Services;
using Savdonoma.Core.Enums;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.UserService;
using Savdonoma.Data.Services.Authservice;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class AuthViewModel : ObservableObject
    {
        private readonly IAuthservice _authService;
        private readonly IUserService _userService;
        private readonly CurrentUserService _currentUser;

        [ObservableProperty]
        private bool isRegistration;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string fullName = "";

        [ObservableProperty]
        private string orgName = "";

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        private string errorMessage = "";

        public bool IsAuthenticated { get; private set; }

        public AuthViewModel(IAuthservice authService, IUserService userService, CurrentUserService currentUser)
        {
            _authService = authService;
            _userService = userService;
            _currentUser = currentUser;
        }

        public async Task InitializeAsync()
        {
            IsRegistration = !await _userService.HasUsersAsync(CancellationToken.None);
        }

        [RelayCommand]
        private async Task SubmitAsync()
        {
            ErrorMessage = "";
            IsBusy = true;

            try
            {
                AuthResultDto result;
                if (IsRegistration)
                {
                    result = await _userService.RegisterFirstOwnerAsync(
                        new CreateUserDto
                        {
                            OrgName = OrgName,
                            FullName = FullName,
                            UserName = UserName,
                            Password = Password,
                            UserRole = UserRole.Boshliq
                        }, CancellationToken.None);
                }
                else
                {
                    result = await _authService.Login(new LoginDto
                    {
                        UserName = UserName,
                        Password = Password
                    }, CancellationToken.None);
                }

                _currentUser.SignIn(result);
                IsAuthenticated = true;
                OnPropertyChanged(nameof(IsAuthenticated));
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Ma’lumotni saqlashda xatolik yuz berdi. Login band bo‘lishi mumkin.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
