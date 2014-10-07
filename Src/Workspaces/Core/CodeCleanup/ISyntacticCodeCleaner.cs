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
    /// code cleaner that only needs syntactic information to do its cleanup
    /// </summary>
    public interface ISyntacticCodeCleaner : ICodeCleaner
    {
        /// <summary>
        /// run code cleanup task for this particular code cleaner
        /// </summary>
        CommonSyntaxNode Cleanup(CommonSyntaxNode node, IEnumerable<TextSpan> spans, CodeCleanupOptions options, CancellationToken cancellationToken);
    }
}
