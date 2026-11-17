using System.Text;
using System.Globalization;
using Savdonoma.Core.Licensing;

// Kalitlar va litsenziyalar repozitoriydan TASHQARIDA saqlanadi
var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SavdonomaLicenses");
Directory.CreateDirectory(dir);

var privatePath = Path.Combine(dir, "private.key");
var publicPath = Path.Combine(dir, "public.key");
var registryPath = Path.Combine(dir, "RegisteredMarkets.csv");

Console.WriteLine("1 - Kalit juftligini yaratish (faqat bir marta)");
Console.WriteLine("2 - Litsenziya berish");
Console.WriteLine("3 - Parol tiklash kodi (do'kon egasi parolni unutganda)");
Console.Write("Tanlang: ");
var choice = Console.ReadLine()?.Trim();

if (choice == "1")
{
    if (File.Exists(privatePath))
    {
        Console.WriteLine("Kalit allaqachon mavjud. O'chirmang: eski litsenziyalar ishlamay qoladi.");
        return;
    }

    var (priv, pub) = LicenseCrypto.GenerateKeyPair();
    File.WriteAllText(privatePath, priv);
    File.WriteAllText(publicPath, pub);

    Console.WriteLine("Tayyor. Ochiq kalitni LicenseKeys.cs ga qo'ying:");
    Console.WriteLine(pub);
}
else if (choice == "2")
{
    if (!File.Exists(privatePath))
    {
        Console.WriteLine("Avval 1-tanlov bilan kalit yarating.");
        return;
    }

    Console.Write("Do'kon nomi: ");
    var shop = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(shop))
    {
        Console.WriteLine("Do'kon nomi bo'sh bo'lmasligi kerak.");
        return;
    }

    Console.Write("Machine ID (mijozning dasturidan oling): ");
    var machineId = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(machineId))
    {
        Console.WriteLine("Machine ID bo'sh bo'lmasligi kerak.");
        return;
    }

    // 3-bo'lak: doimiy yoki muddatli
    Console.Write("Muddatsiz (doimiy) litsenziyami? (ha/yo'q): ");
    var perpetual = Console.ReadLine()?.Trim().ToLower() == "ha";

    DateOnly? expires = null;
    if (!perpetual)
    {
        Console.Write("Tugash sanasi (dd.MM.yyyy): ");
        if (!DateOnly.TryParseExact(Console.ReadLine()?.Trim(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            Console.WriteLine("Sana noto'g'ri kiritildi.");
            return;
        }
        expires = parsed;
    }

    var info = new LicenseInfo
    {
        LicenseId = Guid.NewGuid().ToString("N")[..8].ToUpper(),
        ShopName = shop,
        IssuedAt = LicenseEvaluator.TodayInTashkent(DateTime.UtcNow),
        ExpiresOn = expires,
        MachineId = machineId
    };

    var key = LicenseCrypto.Sign(info, File.ReadAllText(privatePath));

    // O'zini tekshirish
    var ok = LicenseCrypto.TryRead(key, File.ReadAllText(publicPath), out _);
    Console.WriteLine(ok ? "Tekshiruv: OK" : "Tekshiruv: XATO!");

    // 4-bo'lak: faylga va ro'yxatga yozish
    var safeName = string.Concat(shop.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
    var expiresText = expires?.ToString("yyyy-MM-dd") ?? "DOIMIY";

    var filePath = Path.Combine(dir, $"{safeName}-{expiresText}.lic");
    File.WriteAllText(filePath, key);

    AppendRegistryRecord(registryPath, info, expiresText);

    Console.WriteLine($"Fayl: {filePath}");
    Console.WriteLine($"Ro'yxat: {registryPath}");
    Console.WriteLine("Kalit (matn ko'rinishida yuborsangiz ham bo'ladi):");
    Console.WriteLine(key);
}
else if (choice == "3")
{
    if (!File.Exists(privatePath) || !File.Exists(publicPath))
    {
        Console.WriteLine("Avval 1-tanlov bilan kalit yarating.");
        return;
    }

    Console.Write("Machine ID (mijozning 'Parolni tiklash' oynasidan): ");
    var machineId = PasswordResetCode.NormalizeMachineId(Console.ReadLine());
    if (string.IsNullOrWhiteSpace(machineId))
    {
        Console.WriteLine("Machine ID bo'sh bo'lmasligi kerak.");
        return;
    }

    Console.Write("So'rov kodi (masalan, ABCD-2345): ");
    var requestCode = PasswordResetCode.NormalizeRequestCode(Console.ReadLine());
    if (requestCode.Length != 8)
    {
        Console.WriteLine("So'rov kodi 8 ta belgidan iborat bo'lishi kerak.");
        return;
    }

    Console.Write("Do'kon nomi yoki izoh (ro'yxat uchun): ");
    var note = Console.ReadLine()?.Trim() ?? "";

    var today = LicenseEvaluator.TodayInTashkent(DateTime.UtcNow);
    var resetCode = PasswordResetCode.Create(machineId, requestCode, today, File.ReadAllText(privatePath));

    // O'zini tekshirish
    var ok = PasswordResetCode.Verify(resetCode, machineId, requestCode, today, File.ReadAllText(publicPath));
    Console.WriteLine(ok ? "Tekshiruv: OK" : "Tekshiruv: XATO!");

    AppendPasswordResetRecord(Path.Combine(dir, "PasswordResets.csv"), today, machineId, requestCode, note);

    Console.WriteLine($"Kod {PasswordResetCode.ValidDays} kun ichida va faqat shu so'rov kodi uchun ishlaydi.");
    Console.WriteLine("Tiklash kodi (mijozga yuboring):");
    Console.WriteLine(resetCode);
}

static void AppendPasswordResetRecord(string path, DateOnly date, string machineId, string requestCode, string note)
{
    const string header = "Sana;Machine ID;So'rov kodi;Izoh";
    if (!File.Exists(path) || new FileInfo(path).Length == 0)
        File.WriteAllText(path, header + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    var fields = new[] { date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), machineId, requestCode, note };
    using var writer = new StreamWriter(path, append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    writer.WriteLine(string.Join(';', fields.Select(EscapeCsvField)));
}

static void AppendRegistryRecord(string path, LicenseInfo info, string expiresText)
{
    const string header = "Litsenziya ID | Do'kon nomi | Berilgan sana | Amal qilish muddati | Machine ID";

    if (!File.Exists(path) || new FileInfo(path).Length == 0)
    {
        File.WriteAllText(path, header + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
    else
    {
        var existingLines = File.ReadAllLines(path);
        if (existingLines[0].TrimStart('\uFEFF') != header)
        {
            File.WriteAllLines(
                path,
                new[] { header }.Concat(existingLines),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
    }

    var fields = new[]
    {
        info.LicenseId,
        info.ShopName,
        info.IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        expiresText,
        info.MachineId
    };

    using var writer = new StreamWriter(path, append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    writer.WriteLine(string.Join(';', fields.Select(EscapeCsvField)));
}

static string EscapeCsvField(string value)
{
    if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        return value;

    return $"\"{value.Replace("\"", "\"\"")}\"";
}