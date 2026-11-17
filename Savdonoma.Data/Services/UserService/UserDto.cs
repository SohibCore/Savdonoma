using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.UserService
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public UserRole UserRole { get; set; }
        public bool IsActive { get; set; }

        // Do'kon egasi (rahbar): o'chirib ham, rolini o'zgartirib ham bo'lmaydi
        public bool IsOwner => UserRole == UserRole.Boshliq;

        public string RoleDisplay => UserRole switch
        {
            UserRole.Boshliq => "Rahbar",
            UserRole.Manager => "Menejer",
            UserRole.Sotuvchi => "Sotuvchi",
            _ => UserRole.ToString()
        };
    }
}
