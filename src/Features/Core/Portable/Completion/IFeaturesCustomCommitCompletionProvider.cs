using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    interface IFeaturesCustomCommitCompletionProvider
    {
        Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default, CancellationToken cancellationToken = default);
    }
}
