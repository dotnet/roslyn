// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Completion;

[Shared]
[Export(typeof(ICSharpCopilotContextProviderService))]
internal sealed class CSharpContextProviderService : ICSharpCopilotContextProviderService
{
    // Exposed for testing
    public ImmutableArray<IContextProvider> Providers { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpContextProviderService([ImportMany] IEnumerable<IContextProvider> providers)
    {
        Providers = providers.ToImmutableArray();
    }

    public async IAsyncEnumerable<IContextItem> GetContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queue = new AsyncQueue<IContextItem>();
        var tasks = this.Providers.Select(provider => Task.Run(async () =>
        {
            try
            {
                await provider.ProvideContextItemsAsync(document, position, activeExperiments, ProvideItemsAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (FatalError.ReportAndCatchUnlessCanceled(exception, ErrorSeverity.General))
            {
            }
        },
        cancellationToken));

        // Let all providers run in parallel in the background, so we can steam results as they come in.
        // Complete the queue when all providers are done.
        _ = Task.WhenAll(tasks)
            .ContinueWith((_, __) => queue.Complete(),
                          null,
                          cancellationToken,
                          TaskContinuationOptions.ExecuteSynchronously,
                          TaskScheduler.Default);

        while (true)
        {
            IContextItem item;
            try
            {
                item = await queue.DequeueAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Dequeue is cancelled because the queue is empty and completed, we can break out of the loop.
                break;
            }

            yield return item;
        }

        ValueTask ProvideItemsAsync(ImmutableArray<IContextItem> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                queue.Enqueue(item);
            }

            return default;
        }
    }
}
