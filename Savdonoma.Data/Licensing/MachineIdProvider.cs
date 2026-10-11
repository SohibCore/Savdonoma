using Savdonoma.Core.Licensing;

namespace Savdonoma.Data.Licensing
{
    public sealed class MachineIdProvider : IMachineIdProvider
    {
        public string GetMachineId() => MachineId.Get();
    }
}
