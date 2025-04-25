// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Completion;

[Export(typeof(ICSharpCopilotContextProviderService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpContextProviderService([ImportMany] IEnumerable<IContextProvider> providers)
    : ICSharpCopilotContextProviderService
{
    private readonly ImmutableArray<IContextProvider> _providers = [.. providers];

    public IAsyncEnumerable<IContextItem> GetContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, CancellationToken cancellationToken)
        => ProducerConsumer<IContextItem>.RunAsync(
            static (callback, args, cancellationToken) =>
                RoslynParallel.ForEachAsync(
                    args.@this._providers,
                    cancellationToken,
                    (provider, cancellationToken) => provider.ProvideContextItemsAsync(
                        args.document, args.position, args.activeExperiments,
                        (items, cancellationToken) =>
                        {
                            foreach (var item in items)
                                callback(item);

                            return default;
                        }, cancellationToken)),
            args: (@this: this, document, position, activeExperiments),
            cancellationToken);
}
