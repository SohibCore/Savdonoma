using Savdonoma.Core.Enums;

namespace Savdonoma.Core.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Address { get; set; } = "NULL";
        public string Password { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public UserRole UserRole { get; set; }
        public string INN { get; set; } = "NULL";
        public bool IsActive { get; set; }
        public bool? SelfRegister { get; set; } = false;

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
