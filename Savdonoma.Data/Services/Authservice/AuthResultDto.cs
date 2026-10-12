using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Authservice
{
    public class AuthResultDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public UserRole UserRole { get; set; }
    }
}
