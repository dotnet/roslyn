// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteExtensionMethodImportCompletionService
    {
        public Task<(IList<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsAsync(
            DocumentId documentId,
            int position,
            string receiverTypeSymbolKeyData,
            string[] namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId)!;
                    var compilation = (await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false));
                    var symbol = SymbolKey.ResolveString(receiverTypeSymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();

                    if (symbol is ITypeSymbol receiverTypeSymbol)
                    {
                        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                        var namespaceInScopeSet = new HashSet<string>(namespaceInScope, syntaxFacts.StringComparer);

                        var (items, counter) = await ExtensionMethodImportCompletionHelper.GetUnimportedExtensionMethodsInCurrentProcessAsync(
                            document, position, receiverTypeSymbol, namespaceInScopeSet, forceIndexCreation, cancellationToken).ConfigureAwait(false);
                        return ((IList<SerializableImportCompletionItem>)items, counter);
                    }

                    return (Array.Empty<SerializableImportCompletionItem>(), new StatisticCounter());
                }
            }, cancellationToken);
        }
    }
}
