using System.Composition;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionItemService : AbstractGoToDefinitionItemService
    {
    }
}
