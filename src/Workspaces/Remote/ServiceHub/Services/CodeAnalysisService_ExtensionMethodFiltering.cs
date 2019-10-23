// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteExtensionMethodImportCompletionService
    {

        public Task<(IList<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsAsync(
            DocumentId documentId,
            int position,
            string receiverTypeSymbolKeyData,
            string[] namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId)!;
                    var compilation = (await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
                    var symbol = SymbolKey.ResolveString(receiverTypeSymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();

                    if (symbol is ITypeSymbol receiverTypeSymbol)
                    {
                        var syntaxFacts = document.Project.LanguageServices.GetRequiredService<ISyntaxFactsService>();
                        var namespaceInScopeSet = new HashSet<string>(namespaceInScope, syntaxFacts.StringComparer);

                        var (items, counter) = await ExtensionMethodImportCompletionService.GetUnimportExtensionMethodsInCurrentProcessAsync(
                            document, position, receiverTypeSymbol, namespaceInScopeSet, isExpandedCompletion, cancellationToken).ConfigureAwait(false);
                        return ((IList<SerializableImportCompletionItem>)items, counter);
                    }

                    return (new SerializableImportCompletionItem[0], new StatisticCounter());
                }
            }, cancellationToken);
        }
    }
}
