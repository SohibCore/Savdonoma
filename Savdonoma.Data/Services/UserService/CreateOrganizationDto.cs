using Savdonoma.Core.Entity;

namespace Savdonoma.Data.Services.UserService
{
    public class CreateOrganizationDto
    {
        public int OwnerId { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string INN { get; set; } = "";
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public User Owner { get; set; } = null!;
    }
}
