namespace Savdonoma.Core.Licensing
{
    public static class LicenseEvaluator
    {
        public const int WarningDays = 14;

        // O'zbekistonda yozgi vaqt yo'q: doim UTC+5
        public static DateOnly TodayInTashkent(DateTime utcNow)
            => DateOnly.FromDateTime(utcNow.AddHours(5));

        public static LicenseState Evaluate(LicenseInfo? info, DateOnly today)
        {
            if (info == null)
                return new(LicenseStatus.NoLicense, null, null, 0,
                    "Litsenziya topilmadi. Dasturni faollashtiring.");

            // Muddatsiz (doimiy) litsenziya
            if (info.ExpiresOn is not DateOnly expires)
                return new(LicenseStatus.Valid, info.ShopName, null, int.MaxValue,
                    "Doimiy litsenziya (muddatsiz).", true);

            var daysLeft = expires.DayNumber - today.DayNumber;

            if (daysLeft < 0)
                return new(LicenseStatus.Expired, info.ShopName, expires, 0,
                    $"Litsenziya muddati {expires:dd.MM.yyyy} da tugagan.");

            if (daysLeft <= WarningDays)
            {
                var text = daysLeft == 0
                    ? "Litsenziya bugun tugaydi."
                    : $"Litsenziya {daysLeft} kundan keyin tugaydi ({expires:dd.MM.yyyy}).";
                return new(LicenseStatus.ExpiringSoon, info.ShopName, expires, daysLeft, text);
            }

            return new(LicenseStatus.Valid, info.ShopName, expires, daysLeft,
                $"Litsenziya {expires:dd.MM.yyyy} gacha amal qiladi.");
        }
    }
}