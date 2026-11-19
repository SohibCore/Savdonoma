namespace Savdonoma.Data.Services.UserService
{
    public class OrganizationDetailsDto
    {
        public int OwnerId { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string INN { get; set; } = "";
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string OwnerName { get; set; } = "";
        public string OwnerUserName { get; set; } = "";
    }
}
