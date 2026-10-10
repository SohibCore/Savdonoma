using Savdonoma.Core.Licensing;

namespace Savdonoma.Data.Licensing
{
    public class LicenseService : ILicenseService, IDisposable
    {
        private static string LicensePath => Path.Combine(AppPaths.DataFolder, "license.lic");

        private readonly object _lock = new();
        private readonly Timer _timer;
        private LicenseInfo? _info;
        private string? _loadError;
        private LicenseState _current;

        public event EventHandler<LicenseState>? StatusChanged;

        public LicenseState Current
        {
            get { lock (_lock) return _current; }
        }

        public LicenseService()
        {
            LoadFromDisk();
            _current = Evaluate();
            // Har daqiqada sanani tekshiradi (fayl o'qimaydi): yarim tunda muddat tugasa ham sezadi
            _timer = new Timer(_ => Refresh(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

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

        private LicenseState Evaluate()
        {
            if (_loadError != null) return LicenseState.Invalid(_loadError);

            var today = LicenseEvaluator.TodayInTashkent(DateTime.UtcNow);
            return LicenseEvaluator.Evaluate(_info, today);
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

            var today = LicenseEvaluator.TodayInTashkent(DateTime.UtcNow);
            var state = LicenseEvaluator.Evaluate(info, today);

            // Muddati tugagan litsenziya mavjud faylni almashtirmaydi
            if (!state.IsActive) return state;

            // Doimiy litsenziyani muddatli bilan almashtirmaslik   <-- SHU YERGA
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

            lock (_lock)
            {
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