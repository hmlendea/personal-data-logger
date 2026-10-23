using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

using PersonalDataLogger.Service;

using Assembly = System.Reflection.Assembly;

namespace PersonalDataLogger.ArchitectureTests
{
    internal static class ProductionArchitecture
    {
        internal static readonly Assembly[] Assemblies = [typeof(EmailWorker).Assembly];

        internal static readonly Architecture Model = new ArchLoader()
            .LoadAssemblies(Assemblies)
            .Build();
    }
}