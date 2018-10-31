using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToSymbolService : AbstractGoToSymbolService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGoToSymbolService(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }
    }
}
