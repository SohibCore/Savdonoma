namespace Savdonoma.Core.Licensing
{
    public class LicenseExpiredException : Exception
    {
        public LicenseExpiredException(string message) : base(message) { }
    }
}