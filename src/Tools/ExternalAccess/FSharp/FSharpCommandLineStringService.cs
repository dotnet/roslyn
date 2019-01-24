using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    [ExportLanguageService(typeof(ICommandLineStringService), LanguageNames.FSharp), Shared]
    internal class FSharpCommandLineStringService : ICommandLineStringService
    {
    }
}
