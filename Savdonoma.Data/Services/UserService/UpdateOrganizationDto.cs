namespace Savdonoma.Data.Services.UserService
{
    public class UpdateOrganizationDto
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string INN { get; set; } = "";
        public string? Phone { get; set; }
        public string? Website { get; set; }
    }
}
