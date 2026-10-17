using Savdonoma.Core.Licensing;
using System.Globalization;

namespace Savdonoma.Data.Licensing
{
    public class LicenseService : ILicenseService, IDisposable
    {
        private static string LicensePath => Path.Combine(AppPaths.DataFolder, "license.lic");
        private static string LastSeenDatePath => Path.Combine(AppPaths.DataFolder, "license-last-seen-date.txt");

        private LicenseInfo? _info;
        private string? _loadError;
        private string? _clockError;
        private DateOnly? _lastSeenDate;
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
            LoadLastSeenDate();
            _current = Evaluate();
            // Har daqiqada sanani tekshiradi (fayl o'qimaydi): yarim tunda muddat tugasa ham sezadi
            _timer = new Timer(_ => Refresh(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private LicenseState Evaluate()
        {
            if (_loadError != null)
                return LicenseState.Invalid(_loadError);

            if (!TryGetToday(out var today, out var clockError))
                return LicenseState.Invalid(clockError!);

            return LicenseEvaluator.Evaluate(_info, today, _machineId);
        }

        private void LoadLastSeenDate()
        {
            try
            {
                if (!File.Exists(LastSeenDatePath))
                    return;

                var value = File.ReadAllText(LastSeenDatePath).Trim();
                if (!DateOnly.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var lastSeenDate))
                {
                    _clockError = "Litsenziya sanasi nazorati fayli buzilgan.";
                    return;
                }

                _lastSeenDate = lastSeenDate;
            }
            catch (IOException ex)
            {
                _clockError = "Litsenziya sanasi nazorati faylini o'qib bo'lmadi: " + ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                _clockError = "Litsenziya sanasi nazorati faylini o'qishga ruxsat yo'q: " + ex.Message;
            }
        }

        private bool TryGetToday(out DateOnly today, out string? error)
        {
            today = LicenseEvaluator.TodayInTashkent(DateTime.UtcNow);
            error = _clockError;
            if (error != null)
                return false;

            lock (_lock)
            {
                if (_lastSeenDate is DateOnly lastSeenDate && today < lastSeenDate)
                {
                    error = $"Kompyuter sanasi orqaga o'zgartirilgan. Sana {lastSeenDate:dd.MM.yyyy} dan oldin bo'lmasligi kerak.";
                    return false;
                }

                if (_lastSeenDate is DateOnly existingDate && today <= existingDate)
                    return true;

                try
                {
                    Directory.CreateDirectory(AppPaths.DataFolder);
                    var temporaryPath = LastSeenDatePath + ".tmp";
                    File.WriteAllText(temporaryPath, today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    File.Move(temporaryPath, LastSeenDatePath, overwrite: true);
                    _lastSeenDate = today;
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
            }
        }

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

            if (!TryGetToday(out var today, out var clockError))
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

        public void Dispose() => _timer.Dispose();
    }
}