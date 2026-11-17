using System.Security.Cryptography;
using System.Text;

namespace Savdonoma.Core.Licensing
{
    // Do'kon egasining parolini tiklash kodi.
    //
    // Ishlash tartibi:
    //   1. Dastur Machine ID va tasodifiy "so'rov kodi"ni ko'rsatadi.
    //   2. Ishlab chiquvchi LicenseGen'da yopiq kalit bilan shu ikkisi va bugungi sanani imzolaydi.
    //   3. Dastur imzoni ochiq kalit bilan tekshiradi.
    //
    // Dastur ichida hech qanday sir yo'q: kod yaratish uchun yopiq kalit kerak.
    // So'rov kodi har safar yangi, shuning uchun eski kodni qayta ishlatib bo'lmaydi.
    public static class PasswordResetCode
    {
        private const string Purpose = "TijoratUz.PasswordReset.v1";

        // Chalkashadigan belgilarsiz (0/O, 1/I/L yo'q)
        private const string RequestAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        private const int RequestLength = 8;

        // Kod bugun va kecha (Toshkent vaqti) berilgan bo'lsa amal qiladi
        public const int ValidDays = 2;

        public static string NewRequestCode()
        {
            var chars = new char[RequestLength];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = RequestAlphabet[RandomNumberGenerator.GetInt32(RequestAlphabet.Length)];

            var raw = new string(chars);
            return raw[..4] + "-" + raw[4..];
        }

        public static string NormalizeRequestCode(string? requestCode)
            => new string((requestCode ?? "")
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());

        public static string NormalizeMachineId(string? machineId)
            => (machineId ?? "").Trim().ToUpperInvariant();

        public static byte[] BuildMessage(string machineId, string requestCode, DateOnly date)
            => Encoding.UTF8.GetBytes(
                $"{Purpose}|{NormalizeMachineId(machineId)}|{NormalizeRequestCode(requestCode)}|{date:yyyyMMdd}");

        // LicenseGen'da chaqiriladi
        public static string Create(string machineId, string requestCode, DateOnly date, string privateKeyBase64)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

            var signature = ecdsa.SignData(
                BuildMessage(machineId, requestCode, date),
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            return ToBase64Url(signature);
        }

        // Dasturda chaqiriladi
        public static bool Verify(string? code, string machineId, string requestCode, DateOnly today, string publicKeyBase64)
        {
            byte[] signature;
            try
            {
                signature = FromBase64Url(LicenseCrypto.Clean(code));
            }
            catch (FormatException)
            {
                return false;
            }

            if (signature.Length == 0)
                return false;

            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);

                for (var i = 0; i < ValidDays; i++)
                {
                    var message = BuildMessage(machineId, requestCode, today.AddDays(-i));
                    if (ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256,
                            DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                        return true;
                }
            }
            catch (CryptographicException)
            {
                return false;
            }

            return false;
        }

        private static string ToBase64Url(byte[] data)
            => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromBase64Url(string text)
        {
            var s = text.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
                case 1: throw new FormatException();
            }
            return Convert.FromBase64String(s);
        }
    }
}
