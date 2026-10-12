using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.UserService
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public UserRole UserRole { get; set; }
    }
}
