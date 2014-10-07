using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services.Organizing;

namespace Roslyn.Services.CodeCleanup.Providers
{
    internal class SyntaxNodeOrganizationCodeCleanupProvider : ICodeCleanupProvider
    {
        public string Name
        {
            get { return PredefinedCodeCleanupProviderNames.SyntaxNodeOrganization; }
        }

        public Document Cleanup(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = document.GetSyntaxRoot(cancellationToken);

            // this cleaner only works if asked to clean whole document
            if (!spans.Any(s => s.Contains(root.Span)))
            {
                // return document as it is
                return document;
            }

            return OrganizingService.Organize(document, cancellationToken: cancellationToken);
        }
    }
}
