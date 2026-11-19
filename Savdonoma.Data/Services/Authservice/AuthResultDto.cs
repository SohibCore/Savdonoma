using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Authservice
{
    public class AuthResultDto
    {
        // User details
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public UserRole UserRole { get; set; }

        // Org details
        public string Address { get; set; } = "";
        public string INN { get; set; } = "";
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string OrgName { get; set; } = null!;
    }
}
