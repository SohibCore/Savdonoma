using Savdonoma.Core.Licensing;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Savdonoma.Data.Licensing
{
    public class LicenseService : ILicenseService, IDisposable
    {
        private static string LicensePath => Path.Combine(AppPaths.DataFolder, "license.lic");
        private static string LastSeenDatePath => Path.Combine(AppPaths.DataFolder, "license-last-seen-date.txt");
        private const string LastSeenDateFormat = "Savdonoma.LastSeenDate.v1";

        private LicenseInfo? _info;
        private string? _loadError;
        private string? _clockError;
        private DateOnly? _lastSeenDate;
        private byte[]? _lastSeenDateKey;
        private bool _lastSeenDateLoaded;
        private bool _lastSeenDateNeedsUpgrade;
        private readonly Timer _timer;
        private LicenseState _current;
        private readonly string _machineId;
        private readonly object _lock = new();

        public event EventHandler<LicenseState>? StatusChanged;

        public LicenseState Current
        {
            get { lock (_lock) return _current; }
        }

        public string MachineId => _machineId;

        private void LoadFromDisk()
        {
            if (!File.Exists(LicensePath)) return;

            try
            {
                var key = File.ReadAllText(LicensePath);
                if (LicenseCrypto.TryRead(key, LicenseKeys.PublicKey, out var info))
                    _info = info;
                else
                    _loadError = "Litsenziya fayli buzilgan yoki noto'g'ri.";
            }
            catch (IOException ex)
            {
                _loadError = "Litsenziya faylini o'qib bo'lmadi: " + ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                _loadError = "Litsenziya faylini o'qishga ruxsat yo'q: " + ex.Message;
            }
        }

        public LicenseService(IMachineIdProvider machineIdProvider)
        {
            // ID bir marta olinadi (sekin bo'lishi mumkin), keyin maydonda turadi
            _machineId = machineIdProvider.GetMachineId();
            LoadFromDisk();
            _current = Evaluate();
            // Har daqiqada sanani tekshiradi (fayl o'qimaydi): yarim tunda muddat tugasa ham sezadi
            _timer = new Timer(_ => Refresh(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private LicenseState Evaluate()
        {
            if (_loadError != null)
                return LicenseState.Invalid(_loadError);

            if (_info == null || _info.ExpiresOn == null)
                return LicenseEvaluator.Evaluate(_info, DateOnly.MinValue, _machineId);

            if (!TryGetToday(out var today, out var clockError))
                return LicenseState.Invalid(clockError!);

            return LicenseEvaluator.Evaluate(_info, today, _machineId);
        }

        private void LoadLastSeenDate()
        {
            if (_lastSeenDateLoaded)
                return;

            _lastSeenDateLoaded = true;
            try
            {
                if (!File.Exists(LastSeenDatePath))
                    return;

                var lines = File.ReadAllLines(LastSeenDatePath);
                if (lines.Length == 1 && TryParseDate(lines[0], out var legacyDate))
                {
                    _lastSeenDate = legacyDate;
                    _lastSeenDateNeedsUpgrade = true;
                    return;
                }

                if (lines.Length != 4 || lines[0] != LastSeenDateFormat)
                {
                    _clockError = "Litsenziya sanasi nazorati fayli buzilgan yoki formati noto'g'ri.";
                    return;
                }

                if (!TryParseDate(lines[2], out var signedDate))
                {
                    _clockError = "Litsenziya sanasi nazorati fayli buzilgan.";
                    return;
                }

                var protectedKey = Convert.FromBase64String(lines[1]);
                var signature = Convert.FromBase64String(lines[3]);
                var key = ProtectedData.Unprotect(
                    protectedKey,
                    GetDateProtectionEntropy(),
                    DataProtectionScope.CurrentUser);
                if (key.Length != 32)
                {
                    CryptographicOperations.ZeroMemory(key);
                    _clockError = "Litsenziya sanasi uchun himoyalangan kalit noto'g'ri.";
                    return;
                }

                var expectedSignature = CreateDateSignature(key, lines[2]);

                if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
                {
                    CryptographicOperations.ZeroMemory(key);
                    _clockError = "Litsenziya sanasi imzosi noto'g'ri.";
                    return;
                }

                _lastSeenDate = signedDate;
                _lastSeenDateKey = key;
            }
            catch (IOException ex)
            {
                _clockError = "Litsenziya sanasi nazorati faylini o'qib bo'lmadi: " + ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                _clockError = "Litsenziya sanasi nazorati faylini o'qishga ruxsat yo'q: " + ex.Message;
            }
            catch (FormatException)
            {
                _clockError = "Litsenziya sanasi nazorati fayli buzilgan.";
            }
            catch (CryptographicException ex)
            {
                _clockError = "Litsenziya sanasi imzosini tekshirib bo'lmadi: " + ex.Message;
            }
        }

        private bool TryGetToday(out DateOnly today, out string? error)
        {
            today = LicenseEvaluator.TodayInTashkent(DateTime.UtcNow);

            lock (_lock)
            {
                LoadLastSeenDate();
                error = _clockError;
                if (error != null)
                    return false;

                if (_lastSeenDate is DateOnly lastSeenDate && today < lastSeenDate)
                {
                    error = $"Kompyuter sanasi orqaga o'zgartirilgan. Sana {lastSeenDate:dd.MM.yyyy} dan oldin bo'lmasligi kerak.";
                    return false;
                }

                if (_lastSeenDate is DateOnly existingDate
                    && today <= existingDate
                    && !_lastSeenDateNeedsUpgrade)
                    return true;

                try
                {
                    Directory.CreateDirectory(AppPaths.DataFolder);
                    var temporaryPath = LastSeenDatePath + ".tmp";
                    var dateText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    _lastSeenDateKey ??= RandomNumberGenerator.GetBytes(32);
                    var protectedKey = ProtectedData.Protect(
                        _lastSeenDateKey,
                        GetDateProtectionEntropy(),
                        DataProtectionScope.CurrentUser);
                    var signature = CreateDateSignature(_lastSeenDateKey, dateText);
                    File.WriteAllLines(temporaryPath, new[]
                    {
                        LastSeenDateFormat,
                        Convert.ToBase64String(protectedKey),
                        dateText,
                        Convert.ToBase64String(signature)
                    });
                    File.Move(temporaryPath, LastSeenDatePath, overwrite: true);
                    _lastSeenDate = today;
                    _lastSeenDateNeedsUpgrade = false;
                    return true;
                }
                catch (IOException ex)
                {
                    error = "Litsenziya sanasini saqlab bo'lmadi: " + ex.Message;
                    return false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    error = "Litsenziya sanasini saqlashga ruxsat yo'q: " + ex.Message;
                    return false;
                }
                catch (CryptographicException ex)
                {
                    error = "Litsenziya sanasini himoyalab bo'lmadi: " + ex.Message;
                    return false;
                }
                catch (PlatformNotSupportedException ex)
                {
                    error = "Bu Windows muhitida litsenziya sanasini himoyalash qo'llab-quvvatlanmaydi: " + ex.Message;
                    return false;
                }
            }
        }

        private static bool TryParseDate(string value, out DateOnly date)
            => DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);

        private byte[] GetDateProtectionEntropy()
            => Encoding.UTF8.GetBytes($"Savdonoma.LastSeenDate.v1|{_machineId}");

        private static byte[] CreateDateSignature(byte[] key, string date)
            => HMACSHA256.HashData(
                key,
                Encoding.UTF8.GetBytes($"{LastSeenDateFormat}\n{date}"));

        public LicenseState Refresh()
        {
            LicenseState state;
            bool changed;

            lock (_lock)
            {
                state = Evaluate();
                changed = state != _current;
                _current = state;
            }

            // Diqqat: bu event fon oqimidan chaqirilishi mumkin (UI'da Dispatcher kerak)
            if (changed) StatusChanged?.Invoke(this, state);
            return state;
        }

        public LicenseState Activate(string licenseKey)
        {
            if (!LicenseCrypto.TryRead(licenseKey, LicenseKeys.PublicKey, out var info) || info == null)
                return LicenseState.Invalid("Litsenziya kaliti noto'g'ri.");

            var today = DateOnly.MinValue;
            if (info.ExpiresOn != null && !TryGetToday(out today, out var clockError))
                return LicenseState.Invalid(clockError!);

            var state = LicenseEvaluator.Evaluate(info, today, _machineId);

            // Muddati tugagan yoki shu kompyuterga mos kelmaydigan litsenziya faylni almashtirmaydi
            if (!state.IsActive)
                return state;

            lock (_lock)
            {
                // Doimiy litsenziyani muddatli bilan almashtirmaslik
                if (_info is { ExpiresOn: null } && info.ExpiresOn != null)
                    return LicenseState.Invalid("Bu kompyuterda doimiy litsenziya bor.");

                try
                {
                    Directory.CreateDirectory(AppPaths.DataFolder);
                    var temporaryPath = LicensePath + ".tmp";
                    File.WriteAllText(temporaryPath, LicenseCrypto.Clean(licenseKey));
                    File.Move(temporaryPath, LicensePath, overwrite: true);
                }
                catch (IOException ex)
                {
                    return LicenseState.Invalid("Litsenziyani saqlab bo'lmadi: " + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return LicenseState.Invalid("Litsenziyani saqlashga ruxsat yo'q: " + ex.Message);
                }

                _info = info;
                _loadError = null;
            }

            return Refresh();
        }

        public void EnsureCanWrite()
        {
            var state = Refresh();
            if (!state.IsActive)
                throw new LicenseExpiredException(state.Message);
        }

        public void Dispose()
        {
            _timer.Dispose();
            if (_lastSeenDateKey != null)
            {
                CryptographicOperations.ZeroMemory(_lastSeenDateKey);
                _lastSeenDateKey = null;
            }
        }
    }
}