// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class AbstractChangeSignatureService : ILanguageService
    {
        protected SyntaxAnnotation changeSignatureFormattingAnnotation = new SyntaxAnnotation("ChangeSignatureFormatting");

        /// <summary>
        /// Determines the symbol on which we are invoking ReorderParameters
        /// </summary>
        public abstract Task<(ISymbol symbol, int selectedIndex)> GetInvocationSymbolAsync(Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken);

        /// <summary>
        /// Given a SyntaxNode for which we want to reorder parameters/arguments, find the 
        /// SyntaxNode of a kind where we know how to reorder parameters/arguments.
        /// </summary>
        public abstract SyntaxNode FindNodeToUpdate(Document document, SyntaxNode node);

        public abstract Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsFromDelegateInvoke(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId, Document document, CancellationToken cancellationToken);

        public abstract Task<SyntaxNode> ChangeSignatureAsync(
            Document document,
            ISymbol declarationSymbol,
            SyntaxNode potentiallyUpdatedNode,
            SyntaxNode originalNode,
            SignatureChange signaturePermutation,
            CancellationToken cancellationToken);

        protected abstract IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document);

        protected abstract T TransferLeadingWhitespaceTrivia<T>(T newArgument, SyntaxNode oldArgument) where T : SyntaxNode;

        public async Task<ImmutableArray<ChangeSignatureCodeAction>> GetChangeSignatureCodeActionAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var context = await GetContextAsync(document, span.Start, restrictToDeclarations: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            return context is ChangeSignatureAnalyzedSucceedContext changeSignatureAnalyzedSucceedContext
                ? ImmutableArray.Create(new ChangeSignatureCodeAction(this, changeSignatureAnalyzedSucceedContext))
                : ImmutableArray<ChangeSignatureCodeAction>.Empty;
        }

        internal ChangeSignatureResult ChangeSignature(Document document, int position, Action<string, NotificationSeverity> errorHandler, CancellationToken cancellationToken)
        {
            var context = GetContextAsync(document, position, restrictToDeclarations: false, cancellationToken: cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);

            switch (context)
            {
                case ChangeSignatureAnalyzedSucceedContext changeSignatureAnalyzedSucceedContext:
                    return ChangeSignatureWithContextAsync(changeSignatureAnalyzedSucceedContext, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
                case CannotChangeSignatureAnalyzedContext cannotChangeSignatureAnalyzedContext:
                    switch (cannotChangeSignatureAnalyzedContext.CannotChangeSignatureReason)
                    {
                        case CannotChangeSignatureReason.DefinedInMetadata:
                            errorHandler(FeaturesResources.The_member_is_defined_in_metadata, NotificationSeverity.Error);
                            break;
                        case CannotChangeSignatureReason.IncorrectKind:
                            errorHandler(FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate, NotificationSeverity.Error);
                            break;
                    }

                    break;
            }

            return new ChangeSignatureResult(succeeded: false);

        }

        internal async Task<ChangeSignatureAnalyzedContext> GetContextAsync(
            Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken)
        {
            var (symbol, selectedIndex) = await GetInvocationSymbolAsync(
                document, position, restrictToDeclarations, cancellationToken).ConfigureAwait(false);

            // Cross-language symbols will show as metadata, so map it to source if possible.
            symbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false) ?? symbol;

            if (symbol == null)
            {
                return new CannotChangeSignatureAnalyzedContext(CannotChangeSignatureReason.IncorrectKind);
            }

            int? insertPositionOpt = TryGetInsertPositionFromDeclaration(symbol.Locations.FirstOrDefault().FindNode(cancellationToken));
            if (insertPositionOpt == null)
            {
                return new CannotChangeSignatureAnalyzedContext(CannotChangeSignatureReason.IncorrectKind);
            }

            if (symbol is IMethodSymbol method)
            {
                var containingType = method.ContainingType;

                if (method.Name == WellKnownMemberNames.DelegateBeginInvokeName &&
                    containingType != null &&
                    containingType.IsDelegateType() &&
                    containingType.DelegateInvokeMethod != null)
                {
                    symbol = containingType.DelegateInvokeMethod;
                }
            }

            if (symbol is IEventSymbol ev)
            {
                symbol = ev.Type;
            }

            if (symbol is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.IsDelegateType() && typeSymbol.DelegateInvokeMethod != null)
                {
                    symbol = typeSymbol.DelegateInvokeMethod;
                }
            }

            if (symbol.Locations.Any(loc => loc.IsInMetadata))
            {
                return new CannotChangeSignatureAnalyzedContext(CannotChangeSignatureReason.DefinedInMetadata);
            }

            if (!symbol.MatchesKind(SymbolKind.Method, SymbolKind.Property, SymbolKind.NamedType))
            {
                return new CannotChangeSignatureAnalyzedContext(CannotChangeSignatureReason.IncorrectKind);
            }

            var parameterConfiguration = ParameterConfiguration.Create(
                symbol.GetParameters().Select(p => new ExistingParameter(p)),
                symbol.IsExtensionMethod(), selectedIndex);

            return new ChangeSignatureAnalyzedSucceedContext(
                document, insertPositionOpt.Value, symbol, parameterConfiguration);
        }

        private async Task<ChangeSignatureResult> ChangeSignatureWithContextAsync(ChangeSignatureAnalyzedSucceedContext context, CancellationToken cancellationToken)
        {
            var options = GetChangeSignatureOptions(context);
            if (options == null)
            {
                return new ChangeSignatureResult(succeeded: false);
            }

            return await ChangeSignatureWithContextAsync(context, options, cancellationToken).ConfigureAwait(false);
        }

        protected abstract int? TryGetInsertPositionFromDeclaration(SyntaxNode matchingNode);

        internal async Task<ChangeSignatureResult> ChangeSignatureWithContextAsync(ChangeSignatureAnalyzedSucceedContext context, ChangeSignatureOptionsResult options, CancellationToken cancellationToken)
        {
            var updatedSolution = await TryCreateUpdatedSolutionAsync(context, options, cancellationToken).ConfigureAwait(false);
            return new ChangeSignatureResult(updatedSolution != null, updatedSolution, context.Symbol.ToDisplayString(), context.Symbol.GetGlyph(), options.PreviewChanges);
        }

        internal ChangeSignatureOptionsResult? GetChangeSignatureOptions(ChangeSignatureAnalyzedSucceedContext context)
        {
            var changeSignatureOptionsService = context.Solution.Workspace.Services.GetService<IChangeSignatureOptionsService>();

            return changeSignatureOptionsService?.GetChangeSignatureOptions(
                context.Document, context.InsertPosition, context.Symbol, context.ParameterConfiguration);
        }

        private static async Task<ImmutableArray<ReferencedSymbol>> FindChangeSignatureReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ChangeSignature, cancellationToken))
            {
                var streamingProgress = new StreamingProgressCollector(
                    StreamingFindReferencesProgress.Instance);

                var engine = new FindReferencesSearchEngine(
                    solution,
                    documents: null,
                    ReferenceFinders.DefaultReferenceFinders.Add(DelegateInvokeMethodReferenceFinder.DelegateInvokeMethod),
                    streamingProgress,
                    FindReferencesSearchOptions.Default,
                    cancellationToken);

                await engine.FindReferencesAsync(symbolAndProjectId).ConfigureAwait(false);
                return streamingProgress.GetReferencedSymbols();
            }
        }

#nullable enable

        private async Task<Solution?> TryCreateUpdatedSolutionAsync(
            ChangeSignatureAnalyzedSucceedContext context, ChangeSignatureOptionsResult options, CancellationToken cancellationToken)
        {
            var originalSolution = context.Solution;
            var declaredSymbol = context.Symbol;

            var nodesToUpdate = new Dictionary<DocumentId, List<SyntaxNode>>();
            var definitionToUse = new Dictionary<SyntaxNode, ISymbol>();

            var hasLocationsInMetadata = false;

            var symbols = await FindChangeSignatureReferencesAsync(
                SymbolAndProjectId.Create(declaredSymbol, context.Document.Project.Id),
                context.Solution, cancellationToken).ConfigureAwait(false);

            var declaredSymbolParametersCount = declaredSymbol.GetParameters().Length;

            foreach (var symbol in symbols)
            {
                var methodSymbol = symbol.Definition as IMethodSymbol;

                if (methodSymbol != null &&
                    (methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet))
                {
                    continue;
                }

                if (symbol.Definition.Kind == SymbolKind.NamedType)
                {
                    continue;
                }

                if (symbol.Definition.Locations.Any(loc => loc.IsInMetadata))
                {
                    hasLocationsInMetadata = true;
                    continue;
                }

                var symbolWithSyntacticParameters = symbol.Definition;
                var symbolWithSemanticParameters = symbol.Definition;

                var includeDefinitionLocations = true;

                if (symbol.Definition.Kind == SymbolKind.Field)
                {
                    includeDefinitionLocations = false;
                }

                if (symbolWithSyntacticParameters is IEventSymbol eventSymbol)
                {
                    if (eventSymbol.Type is INamedTypeSymbol type && type.DelegateInvokeMethod != null)
                    {
                        symbolWithSemanticParameters = type.DelegateInvokeMethod;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (methodSymbol != null)
                {
                    if (methodSymbol.MethodKind == MethodKind.DelegateInvoke)
                    {
                        symbolWithSyntacticParameters = methodSymbol.ContainingType;
                    }

                    if (methodSymbol.Name == WellKnownMemberNames.DelegateBeginInvokeName &&
                        methodSymbol.ContainingType != null &&
                        methodSymbol.ContainingType.IsDelegateType())
                    {
                        includeDefinitionLocations = false;
                    }

                    // We update delegates which may have different signature.
                    // It seems it is enough for now to compare delegates by parameter count only.
                    if (methodSymbol.Parameters.Length != declaredSymbolParametersCount)
                    {
                        includeDefinitionLocations = false;
                    }
                }

                // Find and annotate all the relevant definitions
                if (includeDefinitionLocations)
                {
                    foreach (var def in symbolWithSyntacticParameters.Locations)
                    {
                        if (!TryGetNodeWithEditableSignatureOrAttributes(def, originalSolution, out var nodeToUpdate, out var documentId))
                        {
                            continue;
                        }

                        if (!nodesToUpdate.ContainsKey(documentId))
                        {
                            nodesToUpdate.Add(documentId, new List<SyntaxNode>());
                        }

                        AddUpdatableNodeToDictionaries(nodesToUpdate, documentId, nodeToUpdate, definitionToUse, symbolWithSemanticParameters);
                    }
                }

                // Find and annotate all the relevant references
                foreach (var location in symbol.Locations)
                {
                    if (location.Location.IsInMetadata)
                    {
                        hasLocationsInMetadata = true;
                        continue;
                    }

                    if (!TryGetNodeWithEditableSignatureOrAttributes(location.Location, originalSolution, out var nodeToUpdate2, out var documentId2))
                    {
                        continue;
                    }

                    if (!nodesToUpdate.ContainsKey(documentId2))
                    {
                        nodesToUpdate.Add(documentId2, new List<SyntaxNode>());
                    }

                    AddUpdatableNodeToDictionaries(nodesToUpdate, documentId2, nodeToUpdate2, definitionToUse, symbolWithSemanticParameters);
                }
            }

            if (hasLocationsInMetadata)
            {
                var notificationService = context.Solution.Workspace.Services.GetRequiredService<INotificationService>();
                if (!notificationService.ConfirmMessageBox(FeaturesResources.This_symbol_has_related_definitions_or_references_in_metadata_Changing_its_signature_may_result_in_build_errors_Do_you_want_to_continue, severity: NotificationSeverity.Warning))
                {
                    return null;
                }
            }

            // Construct all the relevant syntax trees from the base solution
            var updatedRoots = new Dictionary<DocumentId, SyntaxNode>();
            foreach (var docId in nodesToUpdate.Keys)
            {
                var doc = originalSolution.GetDocument(docId)!;
                var updater = doc.Project.LanguageServices.GetRequiredService<AbstractChangeSignatureService>();
                var root = doc.GetSyntaxRootSynchronously(CancellationToken.None);
                if (root is null)
                {
                    throw new NotSupportedException(WorkspacesResources.Document_does_not_support_syntax_trees);
                }

                var nodes = nodesToUpdate[docId];

                var newRoot = root.ReplaceNodes(nodes, (originalNode, potentiallyUpdatedNode) =>
                {
                    return updater.ChangeSignatureAsync(
                        doc,
                        definitionToUse[originalNode],
                        potentiallyUpdatedNode,
                        originalNode,
                        CreateCompensatingSignatureChange(definitionToUse[originalNode], options.UpdatedSignature),
                        cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
                });

                var annotatedNodes = newRoot.GetAnnotatedNodes<SyntaxNode>(syntaxAnnotation: changeSignatureFormattingAnnotation);

                var formattedRoot = Formatter.Format(
                    newRoot,
                    changeSignatureFormattingAnnotation,
                    doc.Project.Solution.Workspace,
                    options: null,
                    rules: GetFormattingRules(doc),
                    cancellationToken: CancellationToken.None);

                updatedRoots[docId] = formattedRoot;
            }

            // Update the documents using the updated syntax trees
            var updatedSolution = originalSolution;
            foreach (var docId in nodesToUpdate.Keys)
            {
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(docId, updatedRoots[docId]);
            }

            (_, updatedSolution) = await updatedSolution.ExcludeDisallowedDocumentTextChangesAsync(originalSolution, cancellationToken).ConfigureAwait(false);
            return updatedSolution;
        }

#nullable restore

        private void AddUpdatableNodeToDictionaries(Dictionary<DocumentId, List<SyntaxNode>> nodesToUpdate, DocumentId documentId, SyntaxNode nodeToUpdate, Dictionary<SyntaxNode, ISymbol> definitionToUse, ISymbol symbolWithSemanticParameters)
        {
            nodesToUpdate[documentId].Add(nodeToUpdate);
            if (definitionToUse.TryGetValue(nodeToUpdate, out var sym) && sym != symbolWithSemanticParameters)
            {
                Debug.Assert(false, "Change Signature: Attempted to modify node twice with different semantic parameters.");
            }

            definitionToUse[nodeToUpdate] = symbolWithSemanticParameters;
        }

        private bool TryGetNodeWithEditableSignatureOrAttributes(Location location, Solution solution, out SyntaxNode nodeToUpdate, out DocumentId documentId)
        {
            var tree = location.SourceTree;
            documentId = solution.GetDocumentId(tree);
            var document = solution.GetDocument(documentId);

            var root = tree.GetRoot();
            var node = root.FindNode(location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
            var updater = document.GetLanguageService<AbstractChangeSignatureService>();
            nodeToUpdate = updater.FindNodeToUpdate(document, node);

            return nodeToUpdate != null;
        }

        protected List<IUnifiedArgumentSyntax> PermuteArguments(
            ISymbol declarationSymbol,
            List<IUnifiedArgumentSyntax> arguments,
            SignatureChange updatedSignature,
            Func<string, IUnifiedArgumentSyntax> createIUnifiedArgument,
            bool isReducedExtensionMethod = false)
        {
            // 1. Determine which parameters are permutable
            var declarationParameters = declarationSymbol.GetParameters().ToList();
            var declarationParametersToPermute = GetParametersToPermute(arguments, declarationParameters, isReducedExtensionMethod);
            var argumentsToPermute = arguments.Take(declarationParametersToPermute.Count).ToList();

            // 2. Create an argument to parameter map, and a parameter to index map for the sort.
            var argumentToParameterMap = new Dictionary<IUnifiedArgumentSyntax, IParameterSymbol>();
            var parameterToIndexMap = new Dictionary<IParameterSymbol, int>();

            for (var i = 0; i < declarationParametersToPermute.Count; i++)
            {
                var decl = declarationParametersToPermute[i];
                var arg = argumentsToPermute[i];

                argumentToParameterMap[arg] = decl;
                var originalIndex = declarationParameters.IndexOf(decl);

                var updatedIndex = updatedSignature.GetUpdatedIndex(originalIndex);

                // If there's no value, then we may be handling a method with more parameters than the original symbol (like BeginInvoke).
                parameterToIndexMap[decl] = updatedIndex ?? -1;
            }

            // 3. Sort the arguments that need to be reordered
            argumentsToPermute.Sort((a1, a2) => { return parameterToIndexMap[argumentToParameterMap[a1]].CompareTo(parameterToIndexMap[argumentToParameterMap[a2]]); });

            // 4. Add names to arguments where necessary.
            var newArguments = new List<IUnifiedArgumentSyntax>();
            var expectedIndex = 0 + (isReducedExtensionMethod ? 1 : 0);
            var seenNamedArgument = false;
            IUnifiedArgumentSyntax paramsArrayArgument = default;

            foreach (var argument in argumentsToPermute)
            {
                var param = argumentToParameterMap[argument];
                var actualIndex = updatedSignature.GetUpdatedIndex(declarationParameters.IndexOf(param));

                if (!actualIndex.HasValue)
                {
                    continue;
                }

                if (!param.IsParams)
                {
                    // If seen a named argument before, add names for subsequent ones.
                    if ((seenNamedArgument || actualIndex != expectedIndex) && !argument.IsNamed)
                    {

                        newArguments.Add(argument.WithName(param.Name).WithAdditionalAnnotations(Formatter.Annotation));
                        seenNamedArgument = true;
                    }
                    else
                    {
                        newArguments.Add(argument);
                    }
                }
                else
                {
                    paramsArrayArgument = argument;
                }

                seenNamedArgument |= argument.IsNamed;
                expectedIndex++;
            }

            // 5. Add added arguments (only at end for the moment)
            var brandNewParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters().Where(p => p is AddedParameter).Cast<AddedParameter>();

            foreach (var brandNewParameter in brandNewParameters)
            {
                newArguments.Add(createIUnifiedArgument(brandNewParameter.CallSiteValue).WithName(brandNewParameter.ParameterName));
            }

            // 6. Add the params argument with the first value:
            if (paramsArrayArgument != default)
            {
                var param = argumentToParameterMap[paramsArrayArgument];
                var actualIndex = updatedSignature.GetUpdatedIndex(declarationParameters.IndexOf(param));
                if (seenNamedArgument && !paramsArrayArgument.IsNamed)
                {
                    newArguments.Add(paramsArrayArgument.WithName(param.Name).WithAdditionalAnnotations(Formatter.Annotation));
                    seenNamedArgument = true;
                }
                else
                {
                    newArguments.Add(paramsArrayArgument);
                }
            }

            // 7. Add the remaining arguments. These will already have names or be params arguments, but may have been removed.
            var removedParams = updatedSignature.OriginalConfiguration.ParamsParameter != null && updatedSignature.UpdatedConfiguration.ParamsParameter == null;
            for (var i = declarationParametersToPermute.Count; i < arguments.Count; i++)
            {
                if (!arguments[i].IsNamed && removedParams && i >= updatedSignature.UpdatedConfiguration.ToListOfParameters().Count)
                {
                    break;
                }

                if (!arguments[i].IsNamed || updatedSignature.UpdatedConfiguration.ToListOfParameters().Any(p => p.Name == arguments[i].GetName()))
                {
                    newArguments.Add(arguments[i]);
                }
            }

            return newArguments;
        }

        private static SignatureChange CreateCompensatingSignatureChange(ISymbol declarationSymbol, SignatureChange updatedSignature)
        {
            if (declarationSymbol.GetParameters().Length > updatedSignature.OriginalConfiguration.ToListOfParameters().Count)
            {
                var originalConfigurationParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
                var updatedConfigurationParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

                var realParameters = declarationSymbol.GetParameters();

                var bonusParameters = realParameters.Skip(originalConfigurationParameters.Count);

                originalConfigurationParameters.AddRange(bonusParameters.Select(p => new ExistingParameter(p)));
                updatedConfigurationParameters.AddRange(bonusParameters.Select(p => new ExistingParameter(p)));

                var newOriginalParameters = ParameterConfiguration.Create(originalConfigurationParameters, updatedSignature.OriginalConfiguration.ThisParameter != null, selectedIndex: 0);
                var newUpdatedParams = ParameterConfiguration.Create(updatedConfigurationParameters, updatedSignature.OriginalConfiguration.ThisParameter != null, selectedIndex: 0);
                updatedSignature = new SignatureChange(newOriginalParameters, newUpdatedParams);
            }

            return updatedSignature;
        }

        private static List<IParameterSymbol> GetParametersToPermute(
            List<IUnifiedArgumentSyntax> arguments,
            List<IParameterSymbol> originalParameters,
            bool isReducedExtensionMethod)
        {
            var position = -1 + (isReducedExtensionMethod ? 1 : 0);
            var parametersToPermute = new List<IParameterSymbol>();

            foreach (var argument in arguments)
            {
                if (argument.IsNamed)
                {
                    var name = argument.GetName();

                    // TODO: file bug for var match = originalParameters.FirstOrDefault(p => p.Name == <ISymbol here>);
                    var match = originalParameters.FirstOrDefault(p => p.Name == name);
                    if (match == null || originalParameters.IndexOf(match) <= position)
                    {
                        break;
                    }
                    else
                    {
                        position = originalParameters.IndexOf(match);
                        parametersToPermute.Add(match);
                    }
                }
                else
                {
                    position++;

                    if (position >= originalParameters.Count)
                    {
                        break;
                    }

                    parametersToPermute.Add(originalParameters[position]);
                }
            }

            return parametersToPermute;
        }

        /// <summary>
        /// Given the cursor position, find which parameter is selected.
        /// Returns 0 as the default value. Note that the ChangeSignature dialog adjusts the selection for
        /// the `this` parameter in extension methods (the selected index won't remain 0).
        /// </summary>
        protected static int GetParameterIndex<TNode>(SeparatedSyntaxList<TNode> parameters, int position)
            where TNode : SyntaxNode
        {
            if (parameters.Count == 0)
            {
                return 0;
            }

            if (position < parameters.Span.Start)
            {
                return 0;
            }

            if (position > parameters.Span.End)
            {
                return 0;
            }

            for (var i = 0; i < parameters.Count - 1; i++)
            {
                // `$$,` points to the argument before the separator
                // but `,$$` points to the argument following the separator
                if (position <= parameters.GetSeparator(i).Span.Start)
                {
                    return i;
                }
            }

            return parameters.Count - 1;
        }

        protected (IEnumerable<T> parameters, IEnumerable<SyntaxToken> separators) PermuteDeclarationBase<T>(
            SeparatedSyntaxList<T> list,
            SignatureChange updatedSignature,
            Func<AddedParameter, T> createNewParameterMethod) where T : SyntaxNode
        {
            var originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
            var reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

            int numAddedParameters = 0;

            var newParameters = new List<T>();
            for (var index = 0; index < reorderedParameters.Count; index++)
            {
                var newParam = reorderedParameters[index];
                if (newParam is ExistingParameter existingParameter)
                {
                    var pos = originalParameters.IndexOf(p => p is ExistingParameter ep && ep.Symbol == existingParameter.Symbol);
                    if (pos >= 0)
                    {
                        var param = list[pos];

                        // copy whitespace trivia from original position
                        param = TransferLeadingWhitespaceTrivia(param, list[index - numAddedParameters]);
                        newParameters.Add(param);
                    }
                }
                else
                {
                    // Added parameter
                    numAddedParameters++;
                    var newParameter = createNewParameterMethod(newParam as AddedParameter);
                    newParameters.Add(newParameter);
                }
            }

            int numSeparatorsToSkip;
            if (originalParameters.Count == 0)
            {
                // () 
                // Adding X parameters, need to add X-1 separators.
                numSeparatorsToSkip = originalParameters.Count - reorderedParameters.Count + 1;
            }
            else
            {
                // (a,b,c)
                // Adding X parameters, need to add X separators.
                numSeparatorsToSkip = originalParameters.Count - reorderedParameters.Count;
            }

            return (newParameters, GetSeparators(list, numSeparatorsToSkip));
        }

        protected List<SyntaxToken> GetSeparators<T>(SeparatedSyntaxList<T> arguments, int numSeparatorsToSkip = 0) where T : SyntaxNode
        {
            var separators = new List<SyntaxToken>();

            for (int i = 0; i < arguments.SeparatorCount - numSeparatorsToSkip; i++)
            {
                if (i >= arguments.SeparatorCount)
                {
                    separators.Add(CreateSeparatorSyntaxToken());
                }
                else
                {
                    separators.Add(arguments.GetSeparator(i));
                }
            }

            return separators;
        }

        protected abstract SyntaxToken CreateSeparatorSyntaxToken();
    }
}
