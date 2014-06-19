using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.Common
{
    partial class CommonCompilation
    {
        internal sealed class DefaultAssemblyLoader : IAssemblyLoader
        {
            public static readonly IAssemblyLoader Instance = new DefaultAssemblyLoader();

            public System.Reflection.Assembly Load(AssemblyIdentity identity)
            {
                return System.Reflection.Assembly.Load(identity.ToAssemblyName(setCodeBase: false));
            }
        }
    }
}