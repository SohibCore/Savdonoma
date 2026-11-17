namespace Savdonoma.Core.Logic
{
    // Tarozi chiqargan EAN-13 barcode:  PP CCCCC WWWWW K
    //   PP    - prefiks 20..29 (do'kon ichki kodlari)
    //   CCCCC - mahsulotning tarozidagi PLU kodi (Product.ScaleCode)
    //   WWWWW - vazn grammda (01250 = 1.250 kg)
    //   K     - nazorat raqami
    public static class ScaleBarcode
    {
        public static bool TryParse(string? code, out string scaleCode, out decimal weightKg)
        {
            scaleCode = "";
            weightKg = 0;

            var text = code?.Trim();
            if (text is null || text.Length != 13 || !text.All(char.IsAsciiDigit))
                return false;

            if (text[0] != '2')
                return false;

            if (!HasValidCheckDigit(text))
                return false;

            var grams = int.Parse(text.Substring(7, 5));
            if (grams <= 0)
                return false;

            scaleCode = NormalizeCode(text.Substring(2, 5));
            weightKg = grams / 1000m;
            return true;
        }

        // "00123" va "123" bir xil PLU hisoblanadi
        public static string NormalizeCode(string? code)
        {
            var trimmed = (code ?? "").Trim().TrimStart('0');
            return trimmed.Length == 0 ? "0" : trimmed;
        }

        public static bool HasValidCheckDigit(string ean13)
        {
            var sum = 0;
            for (var i = 0; i < 12; i++)
            {
                var digit = ean13[i] - '0';
                sum += i % 2 == 0 ? digit : digit * 3;
            }

            var check = (10 - sum % 10) % 10;
            return check == ean13[12] - '0';
        }
    }
}
