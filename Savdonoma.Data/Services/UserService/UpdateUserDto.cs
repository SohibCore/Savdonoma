using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.UserService
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? OrgName { get; set; }
        public UserRole? UserRole { get; set; }
    }
}
