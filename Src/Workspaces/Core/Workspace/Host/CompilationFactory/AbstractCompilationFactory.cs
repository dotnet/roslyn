using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Services.LanguageServices
{
    internal abstract class AbstractCompilationFactory
    {
        protected internal static string CreateAssemblyFileName(string assemblyName, OutputKind outputKind)
        {
            switch (outputKind)
            {
                case OutputKind.WindowsApplication:
                case OutputKind.ConsoleApplication:
                    return assemblyName + ".exe";

                case OutputKind.DynamicallyLinkedLibrary:
                    return assemblyName + ".dll";

                case OutputKind.NetModule:
                    return assemblyName + ".netmodule";

                default:
                    throw Contract.Unreachable;
            }
        }
    }
}
