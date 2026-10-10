namespace Savdonoma.Core.Licensing
{
    public enum LicenseStatus { NoLicense, Valid, ExpiringSoon, Expired, Invalid }

    public record LicenseState(LicenseStatus Status, string? ShopName, DateOnly? ExpiresOn, int DaysLeft, string Message, bool IsPerpetual = false)
    {
        public bool IsActive => Status is LicenseStatus.Valid or LicenseStatus.ExpiringSoon;

        public static LicenseState Invalid(string message)
            => new(LicenseStatus.Invalid, null, null, 0, message);
    }
}
