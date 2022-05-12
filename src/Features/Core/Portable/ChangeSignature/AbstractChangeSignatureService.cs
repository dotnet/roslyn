// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class AbstractChangeSignatureService : ILanguageService
    {
        protected SyntaxAnnotation changeSignatureFormattingAnnotation = new("ChangeSignatureFormatting");

        /// <summary>
        /// Determines the symbol on which we are invoking ReorderParameters
        /// </summary>
        public abstract Task<(ISymbol? symbol, int selectedIndex)> GetInvocationSymbolAsync(Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken);

        /// <summary>
        /// Given a SyntaxNode for which we want to reorder parameters/arguments, find the 
        /// SyntaxNode of a kind where we know how to reorder parameters/arguments.
        /// </summary>
        public abstract SyntaxNode? FindNodeToUpdate(Document document, SyntaxNode node);

        public abstract Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsFromDelegateInvokeAsync(
            IMethodSymbol symbol, Document document, CancellationToken cancellationToken);

        public abstract Task<SyntaxNode> ChangeSignatureAsync(
            Document document,
            ISymbol declarationSymbol,
            SyntaxNode potentiallyUpdatedNode,
            SyntaxNode originalNode,
            SignatureChange signaturePermutation,
            CancellationToken cancellationToken);

        protected abstract IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document);

        protected abstract T TransferLeadingWhitespaceTrivia<T>(T newArgument, SyntaxNode oldArgument) where T : SyntaxNode;

        protected abstract SyntaxToken CommaTokenWithElasticSpace();

        /// <summary>
        /// For some Foo(int x, params int[] p), this helps convert the "1, 2, 3" in Foo(0, 1, 2, 3)
        /// to "new int[] { 1, 2, 3 }" in Foo(0, new int[] { 1, 2, 3 });
        /// </summary>
        protected abstract SyntaxNode CreateExplicitParamsArrayFromIndividualArguments(SeparatedSyntaxList<SyntaxNode> newArguments, int startingIndex, IParameterSymbol parameterSymbol);

        protected abstract SyntaxNode AddNameToArgument(SyntaxNode argument, string name);

        /// <summary>
        /// Only some languages support:
        ///   - Optional parameters and params arrays simultaneously in declarations
        ///   - Passing the params array as a named argument
        /// </summary>
        protected abstract bool SupportsOptionalAndParamsArrayParametersSimultaneously();

        protected abstract bool TryGetRecordPrimaryConstructor(INamedTypeSymbol typeSymbol, [NotNullWhen(true)] out IMethodSymbol? primaryConstructor);

        /// <summary>
        /// A temporarily hack that should be removed once/if https://github.com/dotnet/roslyn/issues/53092 is fixed.
        /// </summary>
        protected abstract ImmutableArray<IParameterSymbol> GetParameters(ISymbol declarationSymbol);

        protected abstract SyntaxGenerator Generator { get; }
        protected abstract ISyntaxFacts SyntaxFacts { get; }

        public async Task<ImmutableArray<ChangeSignatureCodeAction>> GetChangeSignatureCodeActionAsync(Document document, TextSpan span, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var context = await GetChangeSignatureContextAsync(document, span.Start, restrictToDeclarations: true, fallbackOptions, cancellationToken).ConfigureAwait(false);

            return context is ChangeSignatureAnalysisSucceededContext changeSignatureAnalyzedSucceedContext
                ? ImmutableArray.Create(new ChangeSignatureCodeAction(this, changeSignatureAnalyzedSucceedContext))
                : ImmutableArray<ChangeSignatureCodeAction>.Empty;
        }

        internal async Task<ChangeSignatureAnalyzedContext> GetChangeSignatureContextAsync(
            Document document, int position, bool restrictToDeclarations, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var (symbol, selectedIndex) = await GetInvocationSymbolAsync(
                document, position, restrictToDeclarations, cancellationToken).ConfigureAwait(false);

            // Cross-language symbols will show as metadata, so map it to source if possible.
            symbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false) ?? symbol;

            if (symbol == null)
            {
                return new CannotChangeSignatureAnalyzedContext(ChangeSignatureFailureKind.IncorrectKind);
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
                else if (TryGetRecordPrimaryConstructor(typeSymbol, out var primaryConstructor))
                {
                    symbol = primaryConstructor;
                }
            }

            if (!symbol.MatchesKind(SymbolKind.Method, SymbolKind.Property))
            {
                return new CannotChangeSignatureAnalyzedContext(ChangeSignatureFailureKind.IncorrectKind);
            }

            if (symbol.Locations.Any(loc => loc.IsInMetadata))
            {
                return new CannotChangeSignatureAnalyzedContext(ChangeSignatureFailureKind.DefinedInMetadata);
            }

            // This should be called after the metadata check above to avoid looking for nodes in metadata.
            var declarationLocation = symbol.Locations.FirstOrDefault();
            if (declarationLocation == null)
            {
                return new CannotChangeSignatureAnalyzedContext(ChangeSignatureFailureKind.DefinedInMetadata);
            }

            var solution = document.Project.Solution;
            var declarationDocument = solution.GetRequiredDocument(declarationLocation.SourceTree!);
            var declarationChangeSignatureService = declarationDocument.GetRequiredLanguageService<AbstractChangeSignatureService>();

            int positionForTypeBinding;
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();

            if (reference != null)
            {
                var syntax = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                positionForTypeBinding = syntax.SpanStart;
            }
            else
            {
                // There may be no declaring syntax reference, for example delegate Invoke methods.
                // The user may need to fully-qualify type names, including the type(s) defined in
                // this document.
                positionForTypeBinding = 0;
            }

            var parameterConfiguration = ParameterConfiguration.Create(
                GetParameters(symbol).Select(p => new ExistingParameter(p)).ToImmutableArray<Parameter>(),
                symbol.IsExtensionMethod(), selectedIndex);

            return new ChangeSignatureAnalysisSucceededContext(
                declarationDocument, positionForTypeBinding, symbol, parameterConfiguration, fallbackOptions);
        }

        internal async Task<ChangeSignatureResult> ChangeSignatureWithContextAsync(ChangeSignatureAnalyzedContext context, ChangeSignatureOptionsResult? options, CancellationToken cancellationToken)
        {
            return context switch
            {
                ChangeSignatureAnalysisSucceededContext changeSignatureAnalyzedSucceedContext => await GetChangeSignatureResultAsync(changeSignatureAnalyzedSucceedContext, options, cancellationToken).ConfigureAwait(false),
                CannotChangeSignatureAnalyzedContext cannotChangeSignatureAnalyzedContext => new ChangeSignatureResult(succeeded: false, changeSignatureFailureKind: cannotChangeSignatureAnalyzedContext.CannotChangeSignatureReason),
                _ => throw ExceptionUtilities.Unreachable,
            };

            async Task<ChangeSignatureResult> GetChangeSignatureResultAsync(ChangeSignatureAnalysisSucceededContext context, ChangeSignatureOptionsResult? options, CancellationToken cancellationToken)
            {
                if (options == null)
                {
                    return new ChangeSignatureResult(succeeded: false);
                }

                var (updatedSolution, confirmationMessage) = await CreateUpdatedSolutionAsync(context, options, cancellationToken).ConfigureAwait(false);
                return new ChangeSignatureResult(updatedSolution != null, updatedSolution, context.Symbol.ToDisplayString(), context.Symbol.GetGlyph(), options.PreviewChanges, confirmationMessage: confirmationMessage);
            }
        }

        /// <returns>Returns <c>null</c> if the operation is cancelled.</returns>
        internal static ChangeSignatureOptionsResult? GetChangeSignatureOptions(ChangeSignatureAnalyzedContext context)
        {
            if (context is not ChangeSignatureAnalysisSucceededContext succeededContext)
            {
                return null;
            }

            var changeSignatureOptionsService = succeededContext.Solution.Workspace.Services.GetRequiredService<IChangeSignatureOptionsService>();

            return changeSignatureOptionsService.GetChangeSignatureOptions(
                succeededContext.Document, succeededContext.PositionForTypeBinding, succeededContext.Symbol, succeededContext.ParameterConfiguration);
        }

        private static async Task<ImmutableArray<ReferencedSymbol>> FindChangeSignatureReferencesAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ChangeSignature, cancellationToken))
            {
                var streamingProgress = new StreamingProgressCollector();

                var engine = new FindReferencesSearchEngine(
                    solution,
                    documents: null,
                    ReferenceFinders.DefaultReferenceFinders.Add(DelegateInvokeMethodReferenceFinder.DelegateInvokeMethod),
                    streamingProgress,
                    FindReferencesSearchOptions.Default);

                await engine.FindReferencesAsync(symbol, cancellationToken).ConfigureAwait(false);
                return streamingProgress.GetReferencedSymbols();
            }
        }

#nullable enable

        private async Task<(Solution updatedSolution, string? confirmationMessage)> CreateUpdatedSolutionAsync(
            ChangeSignatureAnalysisSucceededContext context, ChangeSignatureOptionsResult options, CancellationToken cancellationToken)
        {
            var telemetryTimer = Stopwatch.StartNew();

            var currentSolution = context.Solution;
            var declaredSymbol = context.Symbol;

            var nodesToUpdate = new Dictionary<DocumentId, List<SyntaxNode>>();
            var definitionToUse = new Dictionary<SyntaxNode, ISymbol>();

            string? confirmationMessage = null;

            var symbols = await FindChangeSignatureReferencesAsync(
                declaredSymbol, context.Solution, cancellationToken).ConfigureAwait(false);

            var declaredSymbolParametersCount = GetParameters(declaredSymbol).Length;

            var telemetryNumberOfDeclarationsToUpdate = 0;
            var telemetryNumberOfReferencesToUpdate = 0;

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
                    confirmationMessage = FeaturesResources.This_symbol_has_related_definitions_or_references_in_metadata_Changing_its_signature_may_result_in_build_errors_Do_you_want_to_continue;
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
                        if (!TryGetNodeWithEditableSignatureOrAttributes(def, currentSolution, out var nodeToUpdate, out var documentId))
                        {
                            continue;
                        }

                        if (!nodesToUpdate.ContainsKey(documentId))
                        {
                            nodesToUpdate.Add(documentId, new List<SyntaxNode>());
                        }

                        telemetryNumberOfDeclarationsToUpdate++;
                        AddUpdatableNodeToDictionaries(nodesToUpdate, documentId, nodeToUpdate, definitionToUse, symbolWithSemanticParameters);
                    }
                }

                // Find and annotate all the relevant references
                foreach (var location in symbol.Locations)
                {
                    if (location.Location.IsInMetadata)
                    {
                        confirmationMessage = FeaturesResources.This_symbol_has_related_definitions_or_references_in_metadata_Changing_its_signature_may_result_in_build_errors_Do_you_want_to_continue;
                        continue;
                    }

                    if (!TryGetNodeWithEditableSignatureOrAttributes(location.Location, currentSolution, out var nodeToUpdate2, out var documentId2))
                    {
                        continue;
                    }

                    if (!nodesToUpdate.ContainsKey(documentId2))
                    {
                        nodesToUpdate.Add(documentId2, new List<SyntaxNode>());
                    }

                    telemetryNumberOfReferencesToUpdate++;
                    AddUpdatableNodeToDictionaries(nodesToUpdate, documentId2, nodeToUpdate2, definitionToUse, symbolWithSemanticParameters);
                }
            }

            // Construct all the relevant syntax trees from the base solution
            var updatedRoots = new Dictionary<DocumentId, SyntaxNode>();
            foreach (var docId in nodesToUpdate.Keys)
            {
                var doc = currentSolution.GetRequiredDocument(docId);
                var updater = doc.Project.LanguageServices.GetRequiredService<AbstractChangeSignatureService>();
                var root = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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
                        UpdateSignatureChangeToIncludeExtraParametersFromTheDeclarationSymbol(definitionToUse[originalNode], options.UpdatedSignature),
                        cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
                });

                var annotatedNodes = newRoot.GetAnnotatedNodes<SyntaxNode>(syntaxAnnotation: changeSignatureFormattingAnnotation);
                var formattingOptions = await doc.GetSyntaxFormattingOptionsAsync(context.FallbackOptions, cancellationToken).ConfigureAwait(false);

                var formattedRoot = Formatter.Format(
                    newRoot,
                    changeSignatureFormattingAnnotation,
                    doc.Project.Solution.Workspace.Services,
                    options: formattingOptions,
                    rules: GetFormattingRules(doc),
                    cancellationToken: CancellationToken.None);

                updatedRoots[docId] = formattedRoot;
            }

            // Update the documents using the updated syntax trees
            foreach (var docId in nodesToUpdate.Keys)
            {
                var updatedDoc = currentSolution.GetRequiredDocument(docId).WithSyntaxRoot(updatedRoots[docId]);
                var cleanupOptions = await updatedDoc.GetCodeCleanupOptionsAsync(context.FallbackOptions, cancellationToken).ConfigureAwait(false);

                var docWithImports = await ImportAdder.AddImportsFromSymbolAnnotationAsync(updatedDoc, cleanupOptions.AddImportOptions, cancellationToken).ConfigureAwait(false);
                var reducedDoc = await Simplifier.ReduceAsync(docWithImports, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                var formattedDoc = await Formatter.FormatAsync(reducedDoc, SyntaxAnnotation.ElasticAnnotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

                currentSolution = currentSolution.WithDocumentSyntaxRoot(docId, (await formattedDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!);
            }

            telemetryTimer.Stop();
            ChangeSignatureLogger.LogCommitInformation(telemetryNumberOfDeclarationsToUpdate, telemetryNumberOfReferencesToUpdate, (int)telemetryTimer.ElapsedMilliseconds);

            return (currentSolution, confirmationMessage);
        }

#nullable disable

        private static void AddUpdatableNodeToDictionaries(Dictionary<DocumentId, List<SyntaxNode>> nodesToUpdate, DocumentId documentId, SyntaxNode nodeToUpdate, Dictionary<SyntaxNode, ISymbol> definitionToUse, ISymbol symbolWithSemanticParameters)
        {
            nodesToUpdate[documentId].Add(nodeToUpdate);
            if (definitionToUse.TryGetValue(nodeToUpdate, out var sym) && sym != symbolWithSemanticParameters)
            {
                Debug.Assert(false, "Change Signature: Attempted to modify node twice with different semantic parameters.");
            }

            definitionToUse[nodeToUpdate] = symbolWithSemanticParameters;
        }

        private static bool TryGetNodeWithEditableSignatureOrAttributes(Location location, Solution solution, out SyntaxNode nodeToUpdate, out DocumentId documentId)
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

        protected ImmutableArray<IUnifiedArgumentSyntax> PermuteArguments(
            ISymbol declarationSymbol,
            ImmutableArray<IUnifiedArgumentSyntax> arguments,
            SignatureChange updatedSignature,
            bool isReducedExtensionMethod = false)
        {
            // 1. Determine which parameters are permutable
            var declarationParameters = GetParameters(declarationSymbol);
            var declarationParametersToPermute = GetParametersToPermute(arguments, declarationParameters, isReducedExtensionMethod);
            var argumentsToPermute = arguments.Take(declarationParametersToPermute.Length).ToList();

            // 2. Create an argument to parameter map, and a parameter to index map for the sort.
            var argumentToParameterMap = new Dictionary<IUnifiedArgumentSyntax, IParameterSymbol>();
            var parameterToIndexMap = new Dictionary<IParameterSymbol, int>();

            for (var i = 0; i < declarationParametersToPermute.Length; i++)
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
            var newArguments = ArrayBuilder<IUnifiedArgumentSyntax>.GetInstance();
            var expectedIndex = 0 + (isReducedExtensionMethod ? 1 : 0);
            var seenNamedArgument = false;

            // Holds the params array argument so it can be
            // added at the end.
            IUnifiedArgumentSyntax paramsArrayArgument = null;

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

            // 5. Add the params argument with the first value:
            if (paramsArrayArgument != null)
            {
                var param = argumentToParameterMap[paramsArrayArgument];
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

            // 6. Add the remaining arguments. These will already have names or be params arguments, but may have been removed.
            var removedParams = updatedSignature.OriginalConfiguration.ParamsParameter != null && updatedSignature.UpdatedConfiguration.ParamsParameter == null;
            for (var i = declarationParametersToPermute.Length; i < arguments.Length; i++)
            {
                if (!arguments[i].IsNamed && removedParams && i >= updatedSignature.UpdatedConfiguration.ToListOfParameters().Length)
                {
                    break;
                }

                if (!arguments[i].IsNamed || updatedSignature.UpdatedConfiguration.ToListOfParameters().Any(p => p.Name == arguments[i].GetName()))
                {
                    newArguments.Add(arguments[i]);
                }
            }

            return newArguments.ToImmutableAndFree();
        }

        /// <summary>
        /// Sometimes signature changes can cascade from a declaration with m parameters to one with n > m parameters, such as
        /// delegate Invoke methods (m) and delegate BeginInvoke methods (n = m + 2). This method adds on those extra parameters
        /// to the base <see cref="SignatureChange"/>.
        /// </summary>
        private SignatureChange UpdateSignatureChangeToIncludeExtraParametersFromTheDeclarationSymbol(ISymbol declarationSymbol, SignatureChange updatedSignature)
        {
            var realParameters = GetParameters(declarationSymbol);
            if (realParameters.Length > updatedSignature.OriginalConfiguration.ToListOfParameters().Length)
            {
                var originalConfigurationParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
                var updatedConfigurationParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

                var bonusParameters = realParameters.Skip(originalConfigurationParameters.Length);

                var originalConfigurationParametersWithExtraParameters = originalConfigurationParameters.AddRange(bonusParameters.Select(p => new ExistingParameter(p)));
                var updatedConfigurationParametersWithExtraParameters = updatedConfigurationParameters.AddRange(bonusParameters.Select(p => new ExistingParameter(p)));

                updatedSignature = new SignatureChange(
                    ParameterConfiguration.Create(originalConfigurationParametersWithExtraParameters, updatedSignature.OriginalConfiguration.ThisParameter != null, selectedIndex: 0),
                    ParameterConfiguration.Create(updatedConfigurationParametersWithExtraParameters, updatedSignature.OriginalConfiguration.ThisParameter != null, selectedIndex: 0));
            }

            return updatedSignature;
        }

        private static ImmutableArray<IParameterSymbol> GetParametersToPermute(
            ImmutableArray<IUnifiedArgumentSyntax> arguments,
            ImmutableArray<IParameterSymbol> originalParameters,
            bool isReducedExtensionMethod)
        {
            var position = -1 + (isReducedExtensionMethod ? 1 : 0);
            var parametersToPermute = ArrayBuilder<IParameterSymbol>.GetInstance();

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

                    if (position >= originalParameters.Length)
                    {
                        break;
                    }

                    parametersToPermute.Add(originalParameters[position]);
                }
            }

            return parametersToPermute.ToImmutableAndFree();
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

        protected (ImmutableArray<T> parameters, ImmutableArray<SyntaxToken> separators) UpdateDeclarationBase<T>(
            SeparatedSyntaxList<T> list,
            SignatureChange updatedSignature,
            Func<AddedParameter, T> createNewParameterMethod) where T : SyntaxNode
        {
            var originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
            var reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

            var numAddedParameters = 0;

            // Iterate through the list of new parameters and combine any
            // preexisting parameters with added parameters to construct
            // the full updated list.
            var newParameters = ImmutableArray.CreateBuilder<T>();
            for (var index = 0; index < reorderedParameters.Length; index++)
            {
                var newParam = reorderedParameters[index];
                if (newParam is ExistingParameter existingParameter)
                {
                    var pos = originalParameters.IndexOf(p => p is ExistingParameter ep && ep.Symbol.Equals(existingParameter.Symbol));
                    var param = list[pos];

                    if (index < list.Count)
                    {
                        param = TransferLeadingWhitespaceTrivia(param, list[index]);
                    }
                    else
                    {
                        param = param.WithLeadingTrivia();
                    }

                    newParameters.Add(param);
                }
                else
                {
                    // Added parameter
                    var newParameter = createNewParameterMethod((AddedParameter)newParam);

                    if (index < list.Count)
                    {
                        newParameter = TransferLeadingWhitespaceTrivia(newParameter, list[index]);
                    }
                    else
                    {
                        newParameter = newParameter.WithLeadingTrivia();
                    }

                    newParameters.Add(newParameter);
                    numAddedParameters++;
                }
            }

            // (a,b,c)
            // Adding X parameters, need to add X separators.
            var numSeparatorsToSkip = originalParameters.Length - reorderedParameters.Length;

            if (originalParameters.Length == 0)
            {
                // () 
                // Adding X parameters, need to add X-1 separators.
                numSeparatorsToSkip++;
            }

            return (newParameters.ToImmutable(), GetSeparators(list, numSeparatorsToSkip));
        }

        protected ImmutableArray<SyntaxToken> GetSeparators<T>(SeparatedSyntaxList<T> arguments, int numSeparatorsToSkip) where T : SyntaxNode
        {
            var separators = ImmutableArray.CreateBuilder<SyntaxToken>();

            for (var i = 0; i < arguments.SeparatorCount - numSeparatorsToSkip; i++)
            {
                separators.Add(i < arguments.SeparatorCount
                    ? arguments.GetSeparator(i)
                    : CommaTokenWithElasticSpace());
            }

            return separators.ToImmutable();
        }

        protected virtual async Task<SeparatedSyntaxList<SyntaxNode>> AddNewArgumentsToListAsync(
            ISymbol declarationSymbol,
            SeparatedSyntaxList<SyntaxNode> newArguments,
            SignatureChange signaturePermutation,
            bool isReducedExtensionMethod,
            bool isParamsArrayExpanded,
            bool generateAttributeArguments,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var fullList = ArrayBuilder<SyntaxNode>.GetInstance();
            var separators = ArrayBuilder<SyntaxToken>.GetInstance();

            var updatedParameters = signaturePermutation.UpdatedConfiguration.ToListOfParameters();

            var indexInListOfPreexistingArguments = 0;

            var seenNamedArguments = false;
            var seenOmitted = false;
            var paramsHandled = false;

            for (var i = 0; i < updatedParameters.Length; i++)
            {
                // Skip this parameter in list of arguments for extension method calls but not for reduced ones.
                if (updatedParameters[i] != signaturePermutation.UpdatedConfiguration.ThisParameter
                    || !isReducedExtensionMethod)
                {
                    var parameters = GetParameters(declarationSymbol);
                    if (updatedParameters[i] is AddedParameter addedParameter)
                    {
                        // Omitting an argument only works in some languages, depending on whether
                        // there is a params array. We sometimes need to reinterpret an requested 
                        // omitted parameter as one with a TODO requested.
                        var forcedCallsiteErrorDueToParamsArray = addedParameter.CallSiteKind == CallSiteKind.Omitted &&
                            parameters.LastOrDefault()?.IsParams == true &&
                            !SupportsOptionalAndParamsArrayParametersSimultaneously();

                        var isCallsiteActuallyOmitted = addedParameter.CallSiteKind == CallSiteKind.Omitted && !forcedCallsiteErrorDueToParamsArray;
                        var isCallsiteActuallyTODO = addedParameter.CallSiteKind == CallSiteKind.Todo || forcedCallsiteErrorDueToParamsArray;

                        if (isCallsiteActuallyOmitted)
                        {
                            seenOmitted = true;
                            seenNamedArguments = true;
                            continue;
                        }

                        var expression = await GenerateInferredCallsiteExpressionAsync(
                                document,
                                position,
                                addedParameter,
                                cancellationToken).ConfigureAwait(false);

                        if (expression == null)
                        {
                            // If we tried to infer the expression but failed, use a TODO instead.
                            isCallsiteActuallyTODO |= addedParameter.CallSiteKind == CallSiteKind.Inferred;

                            expression = Generator.ParseExpression(isCallsiteActuallyTODO ? "TODO" : addedParameter.CallSiteValue);
                        }

                        // TODO: Need to be able to specify which kind of attribute argument it is to the SyntaxGenerator.
                        // https://github.com/dotnet/roslyn/issues/43354
                        var argument = generateAttributeArguments ?
                            Generator.AttributeArgument(
                                name: seenNamedArguments || addedParameter.CallSiteKind == CallSiteKind.ValueWithName ? addedParameter.Name : null,
                                expression: expression) :
                            Generator.Argument(
                                name: seenNamedArguments || addedParameter.CallSiteKind == CallSiteKind.ValueWithName ? addedParameter.Name : null,
                                refKind: RefKind.None,
                                expression: expression);

                        fullList.Add(argument);
                        separators.Add(CommaTokenWithElasticSpace());
                    }
                    else
                    {
                        if (indexInListOfPreexistingArguments == parameters.Length - 1 &&
                            parameters[indexInListOfPreexistingArguments].IsParams)
                        {
                            // Handling params array
                            if (seenOmitted)
                            {
                                // Need to ensure the params array is an actual array, and that the argument is named.
                                if (isParamsArrayExpanded)
                                {
                                    var newArgument = CreateExplicitParamsArrayFromIndividualArguments(newArguments, indexInListOfPreexistingArguments, parameters[indexInListOfPreexistingArguments]);
                                    newArgument = AddNameToArgument(newArgument, parameters[indexInListOfPreexistingArguments].Name);
                                    fullList.Add(newArgument);
                                }
                                else if (indexInListOfPreexistingArguments < newArguments.Count)
                                {
                                    var newArgument = newArguments[indexInListOfPreexistingArguments];
                                    newArgument = AddNameToArgument(newArgument, parameters[indexInListOfPreexistingArguments].Name);
                                    fullList.Add(newArgument);
                                }

                                paramsHandled = true;
                            }
                            else
                            {
                                // Normal case. Handled later.
                            }
                        }
                        else if (indexInListOfPreexistingArguments < newArguments.Count)
                        {
                            if (SyntaxFacts.IsNamedArgument(newArguments[indexInListOfPreexistingArguments]))
                            {
                                seenNamedArguments = true;
                            }

                            if (indexInListOfPreexistingArguments < newArguments.SeparatorCount)
                            {
                                separators.Add(newArguments.GetSeparator(indexInListOfPreexistingArguments));
                            }

                            var newArgument = newArguments[indexInListOfPreexistingArguments];

                            if (seenNamedArguments && !SyntaxFacts.IsNamedArgument(newArgument))
                            {
                                newArgument = AddNameToArgument(newArgument, parameters[indexInListOfPreexistingArguments].Name);
                            }

                            fullList.Add(newArgument);
                            indexInListOfPreexistingArguments++;
                        }
                    }
                }
            }

            if (!paramsHandled)
            {
                // Add the rest of existing parameters, e.g. from the params argument.
                while (indexInListOfPreexistingArguments < newArguments.Count)
                {
                    if (indexInListOfPreexistingArguments < newArguments.SeparatorCount)
                    {
                        separators.Add(newArguments.GetSeparator(indexInListOfPreexistingArguments));
                    }

                    fullList.Add(newArguments[indexInListOfPreexistingArguments++]);
                }
            }

            if (fullList.Count == separators.Count && separators.Count != 0)
            {
                separators.Remove(separators.Last());
            }

            return Generator.SeparatedList(fullList.ToImmutableAndFree(), separators.ToImmutableAndFree());
        }

        private async Task<SyntaxNode> GenerateInferredCallsiteExpressionAsync(
            Document document,
            int position,
            AddedParameter addedParameter,
            CancellationToken cancellationToken)
        {
            if (addedParameter.CallSiteKind != CallSiteKind.Inferred || !addedParameter.TypeBinds)
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var recommender = document.GetRequiredLanguageService<IRecommendationService>();

            var options = RecommendationServiceOptions.From(document.Project);
            var recommendations = recommender.GetRecommendedSymbolsAtPosition(document, semanticModel, position, options, cancellationToken).NamedSymbols;

            var sourceSymbols = recommendations.Where(r => r.IsNonImplicitAndFromSource());

            // For locals, prefer the one with the closest declaration. Because we used the Recommender,
            // we do not have to worry about filtering out inaccessible locals.
            // TODO: Support range variables here as well: https://github.com/dotnet/roslyn/issues/44689
            var orderedLocalAndParameterSymbols = sourceSymbols
                .Where(s => s.IsKind(SymbolKind.Local) || s.IsKind(SymbolKind.Parameter))
                .OrderByDescending(s => s.Locations.First().SourceSpan.Start);

            // No particular ordering preference for properties/fields.
            var orderedPropertiesAndFields = sourceSymbols
                .Where(s => s.IsKind(SymbolKind.Property) || s.IsKind(SymbolKind.Field));

            var fullyOrderedSymbols = orderedLocalAndParameterSymbols.Concat(orderedPropertiesAndFields);

            foreach (var symbol in fullyOrderedSymbols)
            {
                var symbolType = symbol.GetSymbolType();
                if (symbolType == null)
                {
                    continue;
                }

                if (semanticModel.Compilation.ClassifyCommonConversion(symbolType, addedParameter.Type).IsImplicit)
                {
                    return Generator.IdentifierName(symbol.Name);
                }
            }

            return null;
        }

        protected ImmutableArray<SyntaxTrivia> GetPermutedDocCommentTrivia(Document document, SyntaxNode node, ImmutableArray<SyntaxNode> permutedParamNodes)
        {
            var updatedLeadingTrivia = ImmutableArray.CreateBuilder<SyntaxTrivia>();
            var index = 0;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            foreach (var trivia in node.GetLeadingTrivia())
            {
                if (!trivia.HasStructure)
                {
                    updatedLeadingTrivia.Add(trivia);
                    continue;
                }

                var structuredTrivia = trivia.GetStructure();
                if (!syntaxFacts.IsDocumentationComment(structuredTrivia))
                {
                    updatedLeadingTrivia.Add(trivia);
                    continue;
                }

                var updatedNodeList = ArrayBuilder<SyntaxNode>.GetInstance();
                var structuredContent = syntaxFacts.GetContentFromDocumentationCommentTriviaSyntax(trivia);
                for (var i = 0; i < structuredContent.Count; i++)
                {
                    var content = structuredContent[i];
                    if (!syntaxFacts.IsParameterNameXmlElementSyntax(content))
                    {
                        updatedNodeList.Add(content);
                        continue;
                    }

                    // Found a param tag, so insert the next one from the reordered list
                    if (index < permutedParamNodes.Length)
                    {
                        updatedNodeList.Add(permutedParamNodes[index].WithLeadingTrivia(content.GetLeadingTrivia()).WithTrailingTrivia(content.GetTrailingTrivia()));
                        index++;
                    }
                    else
                    {
                        // Inspecting a param element that we are deleting but not replacing.
                    }
                }

                var newDocComments = Generator.DocumentationCommentTriviaWithUpdatedContent(trivia, updatedNodeList.ToImmutableAndFree());
                newDocComments = newDocComments.WithLeadingTrivia(structuredTrivia.GetLeadingTrivia()).WithTrailingTrivia(structuredTrivia.GetTrailingTrivia());
                var newTrivia = Generator.Trivia(newDocComments);
                updatedLeadingTrivia.Add(newTrivia);
            }

            var extraNodeList = ArrayBuilder<SyntaxNode>.GetInstance();
            while (index < permutedParamNodes.Length)
            {
                extraNodeList.Add(permutedParamNodes[index]);
                index++;
            }

            if (extraNodeList.Any())
            {
                var extraDocComments = Generator.DocumentationCommentTrivia(
                    extraNodeList,
                    node.GetTrailingTrivia(),
                    document.Project.Solution.Options.GetOption(FormattingOptions.NewLine, document.Project.Language));
                var newTrivia = Generator.Trivia(extraDocComments);

                updatedLeadingTrivia.Add(newTrivia);
            }

            extraNodeList.Free();

            return updatedLeadingTrivia.ToImmutable();
        }

        protected static bool IsParamsArrayExpandedHelper(ISymbol symbol, int argumentCount, bool lastArgumentIsNamed, SemanticModel semanticModel, SyntaxNode lastArgumentExpression, CancellationToken cancellationToken)
        {
            if (symbol is IMethodSymbol methodSymbol && methodSymbol.Parameters.LastOrDefault()?.IsParams == true)
            {
                if (argumentCount > methodSymbol.Parameters.Length)
                {
                    return true;
                }

                if (argumentCount == methodSymbol.Parameters.Length)
                {
                    if (lastArgumentIsNamed)
                    {
                        // If the last argument is named, then it cannot be part of an expanded params array.
                        return false;
                    }
                    else
                    {
                        var fromType = semanticModel.GetTypeInfo(lastArgumentExpression, cancellationToken);
                        var toType = methodSymbol.Parameters.Last().Type;
                        return !semanticModel.Compilation.HasImplicitConversion(fromType.Type, toType);
                    }
                }
            }

            return false;
        }
    }
}
