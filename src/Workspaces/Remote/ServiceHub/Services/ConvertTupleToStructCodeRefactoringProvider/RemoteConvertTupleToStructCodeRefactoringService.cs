﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteConvertTupleToStructCodeRefactoringService : BrokeredServiceBase, IRemoteConvertTupleToStructCodeRefactoringService
    {
        internal sealed class Factory : FactoryBase<IRemoteConvertTupleToStructCodeRefactoringService, IRemoteConvertTupleToStructCodeRefactoringService.ICallback>
        {
            protected override IRemoteConvertTupleToStructCodeRefactoringService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteConvertTupleToStructCodeRefactoringService.ICallback> callback)
                => new RemoteConvertTupleToStructCodeRefactoringService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteConvertTupleToStructCodeRefactoringService.ICallback> _callback;

        public RemoteConvertTupleToStructCodeRefactoringService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteConvertTupleToStructCodeRefactoringService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

#if TODO // Replace the below specialization with a call to a generic impl once https://github.com/microsoft/vs-streamjsonrpc/issues/789 is fixed
        private CodeCleanupOptionsProvider GetClientOptionsProvider(RemoteServiceCallbackId callbackId)
            => new((language, cancellationToken) => GetClientOptionsAsync<CodeCleanupOptions, IRemoteConvertTupleToStructCodeRefactoringService.ICallback>(_callback, callbackId, language, cancellationToken));
#else
        private CodeCleanupOptionsProvider GetClientOptionsProvider(RemoteServiceCallbackId callbackId)
        {
            return new((language, cancellationToken) => GetClientOptionsAsync(_callback, callbackId, language, cancellationToken));

            static async ValueTask<CodeCleanupOptions> GetClientOptionsAsync(
                RemoteCallback<IRemoteConvertTupleToStructCodeRefactoringService.ICallback> callback,
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

        public ValueTask<SerializableConvertTupleToStructResult> ConvertToStructAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            DocumentId documentId,
            TextSpan span,
            Scope scope,
            bool isRecord,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = solution.GetDocument(documentId);

                var service = document.GetLanguageService<IConvertTupleToStructCodeRefactoringProvider>();
                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var updatedSolution = await service.ConvertToStructAsync(document, span, scope, fallbackOptions, isRecord, cancellationToken).ConfigureAwait(false);

                var cleanedSolution = await CleanupAsync(solution, updatedSolution, fallbackOptions, cancellationToken).ConfigureAwait(false);

                var documentTextChanges = await RemoteUtilities.GetDocumentTextChangesAsync(
                    solution, cleanedSolution, cancellationToken).ConfigureAwait(false);
                var renamedToken = await GetRenamedTokenAsync(
                    solution, cleanedSolution, cancellationToken).ConfigureAwait(false);

                return new SerializableConvertTupleToStructResult(documentTextChanges, renamedToken);
            }, cancellationToken);
        }

        private static async Task<(DocumentId, TextSpan)> GetRenamedTokenAsync(
            Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            var changes = newSolution.GetChangedDocuments(oldSolution);

            foreach (var docId in changes)
            {
                var document = newSolution.GetDocument(docId);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var renamedToken = root.GetAnnotatedTokens(RenameAnnotation.Kind).FirstOrNull();
                if (renamedToken == null)
                    continue;

                return (docId, renamedToken.Value.Span);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static async Task<Solution> CleanupAsync(Solution oldSolution, Solution newSolution, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var changes = newSolution.GetChangedDocuments(oldSolution);
            var final = newSolution;

            foreach (var docId in changes)
            {
                var document = newSolution.GetDocument(docId);

                var options = await document.GetCodeCleanupOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
                var cleaned = await CodeAction.CleanupDocumentAsync(document, options, cancellationToken).ConfigureAwait(false);

                var cleanedRoot = await cleaned.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                final = final.WithDocumentSyntaxRoot(docId, cleanedRoot);
            }

            return final;
        }
    }
}
