namespace Savdonoma.Core.Logic
{
    // O'rtacha tannarx (moving average) hisobi
    public static class CostCalculator
    {
        // Kirimdan keyingi yangi o'rtacha tannarx.
        // Kirim narxi tannarx bo'ladi, agar:
        //  - qoldiq 0 yoki manfiy bo'lsa (masalan, qoldiqsiz rejimda);
        //  - joriy tannarx noma'lum (0) bo'lsa: eski qoldiqni "tekin" deb o'rtachaga qo'shib bo'lmaydi.
        public static long AfterReceipt(decimal currentQuantity, long currentCost, decimal receivedQuantity, long receivedCost)
        {
            if (receivedQuantity <= 0)
                return currentCost;

            if (currentQuantity <= 0 || currentCost <= 0)
                return receivedCost;

            var total = currentQuantity * currentCost + receivedQuantity * receivedCost;
            return Round(total / (currentQuantity + receivedQuantity));
        }

        // Kirim bekor qilinganda: o'sha kirimni o'rtachadan "chiqarib tashlaymiz".
        // Natija mantiqsiz bo'lsa (qoldiq qolmasa yoki manfiy chiqsa), joriy tannarx saqlanadi.
        public static long AfterReceiptCancel(decimal currentQuantity, long currentCost, decimal cancelledQuantity, long cancelledCost)
        {
            var remaining = currentQuantity - cancelledQuantity;
            if (cancelledQuantity <= 0 || remaining <= 0)
                return currentCost;

            var total = currentQuantity * currentCost - cancelledQuantity * cancelledCost;
            if (total <= 0)
                return currentCost;

            return Round(total / remaining);
        }

        private static long Round(decimal value)
            => (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
