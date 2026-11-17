namespace Savdonoma.Core.Logic
{
    // Sotuv narxini ustama (%) bilan hisoblash.
    // Hozirgi usul: yangi partiya kelganda narx YANGI kirim narxidan hisoblanadi
    // va ombordagi hamma tovarga qo'llanadi.
    public static class PriceCalculator
    {
        public const int DefaultRoundingStep = 100;
        public static readonly int[] AllowedRoundingSteps = { 1, 10, 50, 100, 200, 500, 1000 };
        public const decimal MaxMarkupPercent = 1000;

        // Ustama qayerdan olinadi: mahsulot -> kategoriya -> umumiy (birinchi topilgani)
        public static decimal? ResolveMarkup(decimal? productMarkup, decimal? categoryMarkup, decimal? defaultMarkup)
            => productMarkup ?? categoryMarkup ?? defaultMarkup;

        // Kirim narxi + ustama, yuqoriga yaxlitlangan. Hisoblab bo'lmasa null.
        public static long? SuggestSalePrice(long purchasePrice, decimal? markupPercent, int roundingStep)
        {
            if (markupPercent is null || purchasePrice <= 0)
                return null;

            var step = roundingStep > 0 ? roundingStep : DefaultRoundingStep;
            var raw = purchasePrice * (1 + markupPercent.Value / 100m);
            var rounded = Math.Ceiling(raw / step) * step;
            return (long)rounded;
        }

        // Ustama: foyda tannarxga nisbatan. 10 000 -> 12 500 = 25%
        public static decimal? MarkupPercent(long salePrice, long cost)
            => cost <= 0 ? null : Math.Round((salePrice - cost) * 100m / cost, 1);

        // Marja: foyda sotuv narxiga nisbatan. 10 000 -> 12 500 = 20%
        public static decimal? MarginPercent(long salePrice, long cost)
            => salePrice <= 0 || cost <= 0 ? null : Math.Round((salePrice - cost) * 100m / salePrice, 1);

        public static void ValidateMarkup(decimal? markupPercent)
        {
            if (markupPercent is < 0 or > MaxMarkupPercent)
                throw new ArgumentException($"Ustama 0% dan {MaxMarkupPercent:0}% gacha bo'lishi kerak.");
        }

        public static void ValidateRoundingStep(int step)
        {
            if (!AllowedRoundingSteps.Contains(step))
                throw new ArgumentException("Yaxlitlash qadami: " + string.Join(", ", AllowedRoundingSteps) + " so'mdan biri bo'lishi kerak.");
        }
    }
}
