using System.Text.Json;
using System.Security.Cryptography;

namespace Savdonoma.Core.Licensing
{
    public static class LicenseCrypto
    {
        // Kalit juftligi: (maxfiy, ochiq), ikkalasi base64 matn
        public static (string PrivateKey, string PublicKey) GenerateKeyPair()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return (Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()),
                    Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo()));
        }

        // Litsenziya kaliti: "<ma'lumot base64>.<imzo base64>"
        public static string Sign(LicenseInfo info, string privateKeyBase64)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(info);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
            var signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);

            return Convert.ToBase64String(payload) + "." + Convert.ToBase64String(signature);
        }

        public static string Clean(string? key)
            => new string((key ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray());

        // Imzo to'g'ri bo'lsagina true qaytaradi
        public static bool TryRead(string? key, string publicKeyBase64, out LicenseInfo? info)
        {
            info = null;
            try
            {
                var parts = Clean(key).Split('.');
                if (parts.Length != 2) return false;

                var payload = Convert.FromBase64String(parts[0]);
                var signature = Convert.FromBase64String(parts[1]);

                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);

                if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256))
                    return false;

                info = JsonSerializer.Deserialize<LicenseInfo>(payload);
                return info != null;
            }
            catch
            {
                return false;
            }
        }
    }
}