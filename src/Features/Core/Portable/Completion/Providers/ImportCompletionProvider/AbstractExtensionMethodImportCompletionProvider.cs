// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        private bool? _isTargetTypeCompletionFilterExperimentEnabled = null;

        protected abstract string GenericSuffix { get; }

        protected override bool ShouldProvideCompletion(CompletionContext completionContext, SyntaxContext syntaxContext)
            => syntaxContext.IsRightOfNameSeparator && IsAddingImportsSupported(completionContext.Document);

        protected override void LogCommit()
            => CompletionProvidersLogger.LogCommitOfExtensionMethodImportCompletionItem();

        protected override async Task AddCompletionItemsAsync(
            CompletionContext completionContext,
            SyntaxContext syntaxContext,
            HashSet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                var syntaxFacts = completionContext.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                if (TryGetReceiverTypeSymbol(syntaxContext, syntaxFacts, cancellationToken, out var receiverTypeSymbol))
                {
                    using var nestedTokenSource = new CancellationTokenSource();
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(nestedTokenSource.Token, cancellationToken);
                    var inferredTypes = IsTargetTypeCompletionFilterExperimentEnabled(completionContext.Document.Project.Solution.Options)
                        ? syntaxContext.InferredTypes
                        : ImmutableArray<ITypeSymbol>.Empty;

                    var getItemsTask = Task.Run(() => ExtensionMethodImportCompletionHelper.GetUnimportedExtensionMethodsAsync(
                        completionContext.Document,
                        completionContext.Position,
                        receiverTypeSymbol,
                        namespaceInScope,
                        inferredTypes,
                        forceIndexCreation: isExpandedCompletion,
                        linkedTokenSource.Token));

                    var timeoutInMilliseconds = completionContext.Options.GetOption(CompletionServiceOptions.TimeoutInMillisecondsForExtensionMethodImportCompletion);

                    // Timebox is enabled if timeout value is >= 0 and we are not triggered via expander
                    if (timeoutInMilliseconds >= 0 && !isExpandedCompletion)
                    {
                        // timeout == 0 means immediate timeout (for testing purpose)
                        if (timeoutInMilliseconds == 0 || await Task.WhenAny(getItemsTask, Task.Delay(timeoutInMilliseconds, linkedTokenSource.Token)).ConfigureAwait(false) != getItemsTask)
                        {
                            nestedTokenSource.Cancel();
                            CompletionProvidersLogger.LogExtensionMethodCompletionTimeoutCount();
                            return;
                        }
                    }

                    // Either the timebox is not enabled, so we need to wait until the operation for complete,
                    // or there's no timeout, and we now have all completion items ready.
                    var items = await getItemsTask.ConfigureAwait(false);

                    var receiverTypeKey = SymbolKey.CreateString(receiverTypeSymbol, cancellationToken);
                    completionContext.AddItems(items.Select(i => Convert(i, receiverTypeKey)));
                }
                else
                {
                    // If we can't get a valid receiver type, then we don't show expander as available.
                    // We need to set this explicitly here because we didn't do the (more expensive) symbol check inside 
                    // `ShouldProvideCompletion` method above, which is intended for quick syntax based check.
                    completionContext.ExpandItemsAvailable = false;
                }
            }
        }

        private bool IsTargetTypeCompletionFilterExperimentEnabled(OptionSet options)
        {
            _isTargetTypeCompletionFilterExperimentEnabled ??= options.GetOption(CompletionOptions.TargetTypedCompletionFilterFeatureFlag);
            return _isTargetTypeCompletionFilterExperimentEnabled == true;
        }

        private static bool TryGetReceiverTypeSymbol(
            SyntaxContext syntaxContext,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol)
        {
            var parentNode = syntaxContext.TargetToken.Parent;

            // Even though implicit access to extension method is allowed, we decide not support it for simplicity 
            // e.g. we will not provide completion for unimported extension method in this case
            // New Bar() {.X = .$$ }
            var expressionNode = syntaxFacts.GetLeftSideOfDot(parentNode, allowImplicitTarget: false);

            if (expressionNode != null)
            {
                // Check if we are accessing members of a type, no extension methods are exposed off of types.
                if (syntaxContext.SemanticModel.GetSymbolInfo(expressionNode, cancellationToken).GetAnySymbol() is not ITypeSymbol)
                {
                    // The expression we're calling off of needs to have an actual instance type.
                    // We try to be more tolerant to errors here so completion would still be available in certain case of partially typed code.
                    receiverTypeSymbol = syntaxContext.SemanticModel.GetTypeInfo(expressionNode, cancellationToken).Type;
                    if (receiverTypeSymbol is IErrorTypeSymbol errorTypeSymbol)
                    {
                        receiverTypeSymbol = errorTypeSymbol.CandidateSymbols.Select(s => GetSymbolType(s)).FirstOrDefault(s => s != null);
                    }

                    return receiverTypeSymbol != null;
                }
            }

            receiverTypeSymbol = null;
            return false;
        }

        private static ITypeSymbol? GetSymbolType(ISymbol symbol)
            => symbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                IAliasSymbol aliasSymbol => aliasSymbol.Target as ITypeSymbol,
                _ => symbol as ITypeSymbol,
            };

        private CompletionItem Convert(SerializableImportCompletionItem serializableItem, string receiverTypeSymbolKey)
            => ImportCompletionItem.Create(
                serializableItem.Name,
                serializableItem.Arity,
                serializableItem.ContainingNamespace,
                serializableItem.Glyph,
                GenericSuffix,
                CompletionItemFlags.Expanded,
                (serializableItem.SymbolKeyData, receiverTypeSymbolKey, serializableItem.AdditionalOverloadCount),
                serializableItem.IncludedInTargetTypeCompletion);
    }
}
