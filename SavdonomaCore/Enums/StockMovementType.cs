namespace Savdonoma.Core.Enums
{
    // Ombor harakati turi: qoldiq nima sababdan o'zgargani
    public enum StockMovementType
    {
        OpeningBalance = 1,      // Boshlang'ich qoldiq (migratsiya, yangi mahsulot, import)
        Receipt = 2,             // Kirim (yetkazib beruvchidan)
        Sale = 3,                // Sotuv
        SaleCancel = 4,          // Sotuv bekor qilindi
        WriteOff = 5,            // Hisobdan chiqarish
        InventoryAdjustment = 6, // Inventarizatsiya farqi
        ManualAdjustment = 7,    // Mahsulot kartasida qo'lda tuzatish
        DocumentCancel = 8       // Ombor hujjati bekor qilindi
    }
}
