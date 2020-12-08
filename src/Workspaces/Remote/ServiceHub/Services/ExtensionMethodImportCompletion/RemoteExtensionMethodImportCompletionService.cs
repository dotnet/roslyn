// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteExtensionMethodImportCompletionService : BrokeredServiceBase, IRemoteExtensionMethodImportCompletionService
    {
        internal sealed class Factory : FactoryBase<IRemoteExtensionMethodImportCompletionService>
        {
            protected override IRemoteExtensionMethodImportCompletionService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteExtensionMethodImportCompletionService(arguments);
        }

        public RemoteExtensionMethodImportCompletionService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask<SerializableUnimportedExtensionMethods> GetUnimportedExtensionMethodsAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            int position,
            string receiverTypeSymbolKeyData,
            ImmutableArray<string> namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId)!;
                    var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var symbol = SymbolKey.ResolveString(receiverTypeSymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();

                    if (symbol is ITypeSymbol receiverTypeSymbol)
                    {
                        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                        var namespaceInScopeSet = new HashSet<string>(namespaceInScope, syntaxFacts.StringComparer);

                        return await ExtensionMethodImportCompletionHelper.GetUnimportedExtensionMethodsInCurrentProcessAsync(
                            document, position, receiverTypeSymbol, namespaceInScopeSet, forceIndexCreation, cancellationToken).ConfigureAwait(false);
                    }

                    return new SerializableUnimportedExtensionMethods(ImmutableArray<SerializableImportCompletionItem>.Empty, isPartialResult: false, getSymbolsTicks: 0, createItemsTicks: 0);
                }
            }, cancellationToken);
        }
    }
}
