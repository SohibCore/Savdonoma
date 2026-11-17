using Savdonoma.Core.Enums;
using Savdonoma.Data.Services.Authservice;

namespace Savdonoma.Services;

public class CurrentUserService
{
    public int? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? OrgName { get; private set; }
    public UserRole? UserRole { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;
    public bool IsOwner => UserRole == Savdonoma.Core.Enums.UserRole.Boshliq;

    private bool IsOwnerOrManager => UserRole is Savdonoma.Core.Enums.UserRole.Boshliq or Savdonoma.Core.Enums.UserRole.Manager;

    // Ombor amallari (kirim, hisobdan chiqarish, inventarizatsiya): rahbar va menejer
    public bool CanManageStock => IsOwnerOrManager;

    // Chekni bekor qilish: rahbar va menejer. Sotuvchi bekor qila olmaydi.
    public bool CanCancelSales => IsOwnerOrManager;

    // Hisobotda hamma sotuvlar: rahbar va menejer. Sotuvchi faqat o'zinikini ko'radi.
    public bool CanViewAllSales => IsOwnerOrManager;

    public void SignIn(AuthResultDto user)
    {
        UserId = user.UserId;
        UserName = user.UserName;
        OrgName = user.OrgName;
        UserRole = user.UserRole;
    }

    public void Logout()
    {
        UserId = null;
        UserName = null;
        OrgName = null;
        UserRole = null;
    }
}
