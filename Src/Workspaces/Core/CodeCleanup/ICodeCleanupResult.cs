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
    /// contains result of code cleanup work
    /// </summary>
    public interface ICodeCleanupResult
    {
        bool ContainsChanges { get; }

        IList<TextChange> GetTextChanges(CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// returns a new node with all changes applied to it
        /// </summary>
        CommonSyntaxNode GetFormattedRoot(CancellationToken cancellation = default(CancellationToken));
    }
}
