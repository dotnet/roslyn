using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
#if MEF
    [ExportLanguageService(typeof(IFormattingService), LanguageNames.CSharp)]
#endif
    internal class CSharpFormattingService : IFormattingService
    {
        public async Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var span = textSpan.HasValue ? textSpan.Value : new TextSpan(0, sourceText.Length);
            return Formatter.GetFormattedTextChanges(root, new TextSpan[] { span }, document.Project.Solution.Workspace, cancellationToken: cancellationToken);
        }
    }
}
