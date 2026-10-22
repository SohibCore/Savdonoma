namespace Savdonoma.Core.Logic
{
    public static class SaleCalculator
    {
        public static long RowSum(long price, decimal quantity)
        {
            return (long)Math.Round(price * quantity, MidpointRounding.AwayFromZero);
        }
        public static long TotalSale(IEnumerable<long> lineSum)
        {
            return (long)lineSum.Sum();
        }
    }
}
