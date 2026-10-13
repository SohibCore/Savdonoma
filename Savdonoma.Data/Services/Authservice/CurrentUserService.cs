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