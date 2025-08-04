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
using Microsoft.CodeAnalysis.Shared.Utilities;
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
        var items = await ProducerConsumer<IContextItem>.RunParallelAsync(
        source: this.Providers,
        produceItems: static async (provider, callback, args, cancellationToken) =>
        {
            var (document, position, activeExperiments) = args;
            try
            {
                await provider.ProvideContextItemsAsync(document, position, activeExperiments, ProvideItemsAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (FatalError.ReportAndCatchUnlessCanceled(exception, ErrorSeverity.General))
            {
            }

            ValueTask ProvideItemsAsync(ImmutableArray<IContextItem> items, CancellationToken cancellationToken)
            {
                foreach (var item in items)
                {
                    callback(item);
                }

                return default;
            }
        },
        args: (document, position, activeExperiments),
        cancellationToken).ConfigureAwait(false);

        foreach (var item in items)
        {
            yield return item;
        }
    }
}
