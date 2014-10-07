using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.CodeCleanup
{
    /// <summary>
    /// code cleaner that reqires semantic information to do its job
    /// </summary>
    public interface IDocumentCodeCleaner : ICodeCleaner
    {
        /// <summary>
        /// this should apply its code clean up logic to its 
        /// </summary>
        CommonSyntaxNode Cleanup(IDocument document, IEnumerable<TextSpan> spans, CodeCleanupOptions options, CancellationToken cancellationToken);
    }
}
