using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToSymbolService : AbstractGoToSymbolService
    {
        [ImportingConstructor]
        public CSharpGoToSymbolService(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }
    }
}
