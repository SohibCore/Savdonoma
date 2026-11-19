using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Data.Services.UserService;

namespace Savdonoma.ViewModels
{
    public partial class OrganizationViewModel : ObservableObject
    {
        private readonly IUserService _userService;

        [ObservableProperty]
        private OrganizationDetailsDto? details;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = "";

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private string editName = "";

        [ObservableProperty]
        private string editAddress = "";

        [ObservableProperty]
        private string editInn = "";

        [ObservableProperty]
        private string editPhone = "";

        [ObservableProperty]
        private string editWebsite = "";

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isSaving;

        [ObservableProperty]
        private string ownerEditFullName = "";

        [ObservableProperty]
        private string ownerEditUserName = "";

        [ObservableProperty]
        private string ownerCurrentPassword = "";

        [ObservableProperty]
        private bool isOwnerEditing;

        [ObservableProperty]
        private bool isOwnerSaving;

        [ObservableProperty]
        private string ownerErrorMessage = "";

        [ObservableProperty]
        private string ownerStatusMessage = "";

        public OrganizationViewModel(IUserService userService)
        {
            _userService = userService;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = "";
            StatusMessage = "";
            IsEditing = false;
            IsOwnerEditing = false;
            OwnerErrorMessage = "";
            OwnerStatusMessage = "";

            try
            {
                Details = await _userService.GetOrganizationDetailsAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Details = null;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void BeginEdit()
        {
            if (Details is null || IsLoading || IsSaving)
                return;

            EditName = Details.Name;
            EditAddress = Details.Address;
            EditInn = Details.INN;
            EditPhone = Details.Phone ?? "";
            EditWebsite = Details.Website ?? "";
            ErrorMessage = "";
            StatusMessage = "";
            IsEditing = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            ErrorMessage = "";
            StatusMessage = "";
        }

        [RelayCommand]
        private async Task SaveOrganizationAsync()
        {
            if (!IsEditing || IsSaving)
                return;

            IsSaving = true;
            ErrorMessage = "";
            StatusMessage = "";

            try
            {
                await _userService.UpdateOrganizationAsync(new UpdateOrganizationDto
                {
                    Name = EditName,
                    Address = EditAddress,
                    INN = EditInn,
                    Phone = EditPhone,
                    Website = EditWebsite,
                }, CancellationToken.None);

                Details = await _userService.GetOrganizationDetailsAsync(CancellationToken.None);
                IsEditing = false;
                StatusMessage = "Do‘kon ma’lumotlari saqlandi.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void BeginOwnerEdit()
        {
            if (Details is null || IsLoading || IsOwnerSaving)
                return;

            OwnerEditFullName = Details.OwnerName;
            OwnerEditUserName = Details.OwnerUserName;
            OwnerCurrentPassword = "";
            OwnerErrorMessage = "";
            OwnerStatusMessage = "";
            IsOwnerEditing = true;
        }

        [RelayCommand]
        private void CancelOwnerEdit()
        {
            IsOwnerEditing = false;
            OwnerCurrentPassword = "";
            OwnerErrorMessage = "";
            OwnerStatusMessage = "";
        }

        [RelayCommand]
        private async Task SaveOwnerAsync()
        {
            if (!IsOwnerEditing || IsOwnerSaving)
                return;

            IsOwnerSaving = true;
            OwnerErrorMessage = "";
            OwnerStatusMessage = "";

            try
            {
                if (string.IsNullOrWhiteSpace(OwnerEditFullName)
                    || string.IsNullOrWhiteSpace(OwnerEditUserName))
                    throw new InvalidOperationException("Egasi ismi va loginini to‘liq kiriting.");

                await _userService.UpdateAsync(new UpdateUserDto
                {
                    Id = Details!.OwnerId,
                    FullName = OwnerEditFullName,
                    UserName = OwnerEditUserName,
                    CurrentPassword = OwnerCurrentPassword,
                }, CancellationToken.None);

                Details = await _userService.GetOrganizationDetailsAsync(CancellationToken.None);
                IsOwnerEditing = false;
                OwnerCurrentPassword = "";
                OwnerStatusMessage = "Do‘kon egasi ma’lumotlari saqlandi.";
            }
            catch (Exception ex)
            {
                OwnerErrorMessage = ex.Message;
            }
            finally
            {
                IsOwnerSaving = false;
            }
        }
    }
}
