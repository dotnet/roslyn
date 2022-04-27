// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteEncapsulateFieldService : BrokeredServiceBase, IRemoteEncapsulateFieldService
    {
        internal sealed class Factory : FactoryBase<IRemoteEncapsulateFieldService, IRemoteEncapsulateFieldService.ICallback>
        {
            protected override IRemoteEncapsulateFieldService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEncapsulateFieldService.ICallback> callback)
                => new RemoteEncapsulateFieldService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteEncapsulateFieldService.ICallback> _callback;

        public RemoteEncapsulateFieldService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEncapsulateFieldService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

#if TODO // Replace the below specialization with a call to a generic impl once https://github.com/microsoft/vs-streamjsonrpc/issues/789 is fixed
        private CodeCleanupOptionsProvider GetClientOptionsProvider(RemoteServiceCallbackId callbackId)
            => new((language, cancellationToken) => GetClientOptionsAsync<CodeCleanupOptions, IRemoteEncapsulateFieldService.ICallback>(_callback, callbackId, language, cancellationToken));
#else
        private CodeCleanupOptionsProvider GetClientOptionsProvider(RemoteServiceCallbackId callbackId)
        {
            return new((language, cancellationToken) => GetClientOptionsAsync(_callback, callbackId, language, cancellationToken));

            static async ValueTask<CodeCleanupOptions> GetClientOptionsAsync(
                RemoteCallback<IRemoteEncapsulateFieldService.ICallback> callback,
                RemoteServiceCallbackId callbackId,
                HostLanguageServices languageServices,
                CancellationToken cancellationToken)
            {
                var cache = ImmutableDictionary<string, AsyncLazy<CodeCleanupOptions>>.Empty;
                var lazyOptions = ImmutableInterlocked.GetOrAdd(ref cache, languageServices.Language, _ => new AsyncLazy<CodeCleanupOptions>(GetRemoteOptions, cacheResult: true));
                return await lazyOptions.GetValueAsync(cancellationToken).ConfigureAwait(false);

                Task<CodeCleanupOptions> GetRemoteOptions(CancellationToken cancellationToken)
                    => callback.InvokeAsync((callback, cancellationToken) => callback.GetOptionsAsync(callbackId, languageServices.Language, cancellationToken), cancellationToken).AsTask();
            }
        }
#endif

        public ValueTask<ImmutableArray<(DocumentId, ImmutableArray<TextChange>)>> EncapsulateFieldsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            DocumentId documentId,
            ImmutableArray<string> fieldSymbolKeys,
            bool updateReferences,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = solution.GetRequiredDocument(documentId);

                using var _ = ArrayBuilder<IFieldSymbol>.GetInstance(out var fields);
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                foreach (var key in fieldSymbolKeys)
                {
                    var resolved = SymbolKey.ResolveString(key, compilation, cancellationToken: cancellationToken).GetAnySymbol() as IFieldSymbol;
                    if (resolved == null)
                        return ImmutableArray<(DocumentId, ImmutableArray<TextChange>)>.Empty;

                    fields.Add(resolved);
                }

                var service = document.GetLanguageService<AbstractEncapsulateFieldService>();
                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var newSolution = await service.EncapsulateFieldsAsync(
                    document, fields.ToImmutable(), fallbackOptions, updateReferences, cancellationToken).ConfigureAwait(false);

                return await RemoteUtilities.GetDocumentTextChangesAsync(
                    solution, newSolution, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
