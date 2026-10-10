using System.Text.Json.Serialization;

namespace Savdonoma.Core.Licensing
{
    public class LicenseInfo
    {
        public string LicenseId { get; set; } = "";
        public string ShopName { get; set; } = "";
        public DateOnly IssuedAt { get; set; }

        // null bo'lsa, litsenziya muddatsiz (doimiy)
        public DateOnly? ExpiresOn { get; set; }

        public List<string> MachineParts { get; set; } = new();

        [JsonIgnore]
        public bool IsPerpetual => ExpiresOn == null;
    }
}
