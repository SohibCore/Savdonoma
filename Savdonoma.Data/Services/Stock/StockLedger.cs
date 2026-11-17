using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Stock
{
    // Qoldiqni o'zgartirishning yagona yo'li: Product.Quantity yangilanadi va
    // shu zahoti StockMovement yozuvi qo'shiladi. Ikkalasi bitta SaveChanges'da saqlanadi.
    public static class StockLedger
    {
        // Sotuv, ombor hujjatlari va mahsulot qoldig'ini tahrirlash shu qulf ostida bajariladi,
        // aks holda parallel amallar qoldiqni noto'g'ri hisoblashi mumkin.
        public static readonly SemaphoreSlim Lock = new(1, 1);

        public static StockMovement Apply(
            AppDbContext db,
            Product product,
            decimal quantity,
            StockMovementType type,
            DateTime now,
            int? userId,
            StockDocument? document = null,
            Sale? sale = null,
            string? note = null)
        {
            if (quantity == 0)
                throw new ArgumentException("Harakat miqdori 0 bo'lishi mumkin emas.", nameof(quantity));

            product.Quantity += quantity;
            product.UpdatedAt = now;

            var movement = new StockMovement
            {
                Product = product,
                ProductId = product.Id,
                Type = type,
                Quantity = quantity,
                BalanceAfter = product.Quantity,
                CostPrice = product.CostPrice,
                Document = document,
                Sale = sale,
                UserId = userId,
                Note = note,
                CreatedAt = now,
            };

            db.StockMovements.Add(movement);
            return movement;
        }
    }
}
