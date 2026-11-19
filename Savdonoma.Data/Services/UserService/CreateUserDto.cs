using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.UserService
{
    public class CreateUserDto
    {
        // User details
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public UserRole UserRole { get; set; }

        // Org details
        public int OwnerId { get; set; }
        public string Name { get; set; } = "";
        public string OrgAddress { get; set; } = "";
        public string OrgINN { get; set; } = "";
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public User Owner { get; set; } = null!;
    }
}
