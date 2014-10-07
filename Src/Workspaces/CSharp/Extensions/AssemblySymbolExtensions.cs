using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal static class AssemblySymbolExtensions
    {
        public static bool HasInternalAccessTo(this AssemblySymbol fromAssembly, AssemblySymbol toAssembly)
        {
            if (Equals(fromAssembly, toAssembly))
            {
                return true;
            }

            // checks if fromAssembly has friend assembly access to the internals in toAssembly
            // TODO(cyrusn): defer to the compiler function that computes this.
            return false;
        }
    }
}