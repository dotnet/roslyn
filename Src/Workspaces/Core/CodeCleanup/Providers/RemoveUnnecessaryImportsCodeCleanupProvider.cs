using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services.RemoveUnnecessaryImports;
using Roslyn.Services.Shared.RemoveUnnecessaryImports;

namespace Roslyn.Services.CodeCleanup.Providers
{
    [ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.RemoveUnnecessaryImports, LanguageNames.CSharp, LanguageNames.VisualBasic)]
    [ExtensionOrder(After = PredefinedCodeCleanupProviderNames.Simplification)]
    internal class RemoveUnnecessaryImportsCodeCleanupProvider : ICodeCleanupProvider
    {
        public string Name
        {
            get { return PredefinedCodeCleanupProviderNames.RemoveUnnecessaryImports; }
        }

        public Document Cleanup(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRoot(cancellationToken);

            // this cleaner only works if asked to clean whole document
            if (!spans.Any(s => s.Contains(root.Span)))
            {
                // return document as it is
                return document;
            }

            return RemoveUnnecessaryImportsService.RemoveUnnecessaryImports(document, cancellationToken);
        }
    }
}
