namespace Savdonoma.Data.Services.UserService
{
    public enum UserDeleteResult
    {
        // Bazadan butunlay o'chirildi
        Deleted,

        // Sotuvlari bor, shuning uchun faqat nofaol qilindi
        Deactivated
    }
}
