namespace Savdonoma.Core.Logic
{
    public static class SaleCalculator
    {
        public static long RowSum(long price, decimal quantity)
        {
            return (long)Math.Round(price * quantity, 2);
        }
        public static long TotolSale(IEnumerable<long> lineSum)
        {
            return (long)lineSum.Sum();
        }
    }
}
