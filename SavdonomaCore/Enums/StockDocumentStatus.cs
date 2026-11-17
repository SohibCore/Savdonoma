namespace Savdonoma.Core.Enums
{
    public enum StockDocumentStatus
    {
        Draft = 1,      // Qoralama: qoldiqqa ta'sir qilmaydi, tahrirlash mumkin
        Posted = 2,     // O'tkazilgan: qoldiq o'zgargan, tahrirlab bo'lmaydi
        Cancelled = 3   // Bekor qilingan: teskari harakatlar yozilgan
    }
}
