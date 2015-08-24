// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public abstract ISymbol GetInvocationSymbol(Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken);

        /// <summary>
        /// Given a SyntaxNode for which we want to reorder parameters/arguments, find the 
        /// SyntaxNode of a kind where we know how to reorder parameters/arguments.
        /// </summary>
        public abstract SyntaxNode FindNodeToUpdate(Document document, SyntaxNode node);

        public abstract Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsFromDelegateInvoke(IMethodSymbol symbol, Document document, CancellationToken cancellationToken);

        public abstract SyntaxNode ChangeSignature(
            Document document,
            ISymbol declarationSymbol,
            SyntaxNode potentiallyUpdatedNode,
            SyntaxNode originalNode,
            SignatureChange signaturePermutation,
            CancellationToken cancellationToken);

        protected abstract IEnumerable<IFormattingRule> GetFormattingRules(Document document);

        public async Task<IEnumerable<ChangeSignatureCodeAction>> GetChangeSignatureCodeActionAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var context = await GetContextAsync(document, span.Start, restrictToDeclarations: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            return context.CanChangeSignature
                ? SpecializedCollections.SingletonEnumerable(new ChangeSignatureCodeAction(this, context))
                : SpecializedCollections.EmptyEnumerable<ChangeSignatureCodeAction>();
        }

        internal ChangeSignatureResult ChangeSignature(Document document, int position, Action<string, NotificationSeverity> errorHandler, CancellationToken cancellationToken)
        {
            var context = GetContextAsync(document, position, restrictToDeclarations: false, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

            if (context.CanChangeSignature)
            {
                return ChangeSignatureWithContext(context, cancellationToken);
            }
            else
            {
                switch (context.CannotChangeSignatureReason)
                {
                    case CannotChangeSignatureReason.DefinedInMetadata:
                        errorHandler(FeaturesResources.TheMemberIsDefinedInMetadata, NotificationSeverity.Error);
                        break;
                    case CannotChangeSignatureReason.IncorrectKind:
                        errorHandler(FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate, NotificationSeverity.Error);
                        break;
                    case CannotChangeSignatureReason.InsufficientParameters:
                        errorHandler(FeaturesResources.ThisSignatureDoesNotContainParametersThatCanBeChanged, NotificationSeverity.Error);
                        break;
                }

                return new ChangeSignatureResult(succeeded: false);
            }
        }

        private async Task<ChangeSignatureAnalyzedContext> GetContextAsync(Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken)
        {
            var symbol = GetInvocationSymbol(document, position, restrictToDeclarations, cancellationToken);

            // Cross-lang symbols will show as metadata, so map it to source if possible.
            symbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false) ?? symbol;

            if (symbol == null)
            {
                return new ChangeSignatureAnalyzedContext(CannotChangeSignatureReason.IncorrectKind);
            }

            if (symbol is IMethodSymbol)
            {
                var method = symbol as IMethodSymbol;
                var containingType = method.ContainingType;

                if (method.Name == WellKnownMemberNames.DelegateBeginInvokeName &&
                    containingType != null &&
                    containingType.IsDelegateType() &&
                    containingType.DelegateInvokeMethod != null)
                {
                    symbol = containingType.DelegateInvokeMethod;
                }
            }

            if (symbol is IEventSymbol)
            {
                symbol = (symbol as IEventSymbol).Type;
            }

            if (symbol is INamedTypeSymbol)
            {
                var typeSymbol = symbol as INamedTypeSymbol;
                if (typeSymbol.IsDelegateType() && typeSymbol.DelegateInvokeMethod != null)
                {
                    symbol = typeSymbol.DelegateInvokeMethod;
                }
            }

            if (symbol.Locations.Any(loc => loc.IsInMetadata))
            {
                return new ChangeSignatureAnalyzedContext(CannotChangeSignatureReason.DefinedInMetadata);
            }

            if (!symbol.MatchesKind(SymbolKind.Method, SymbolKind.Property, SymbolKind.NamedType))
            {
                return new ChangeSignatureAnalyzedContext(CannotChangeSignatureReason.IncorrectKind);
            }

            var parameterConfiguration = ParameterConfiguration.Create(symbol.GetParameters().ToList(), symbol is IMethodSymbol && (symbol as IMethodSymbol).IsExtensionMethod);
            if (!parameterConfiguration.IsChangeable())
            {
                return new ChangeSignatureAnalyzedContext(CannotChangeSignatureReason.InsufficientParameters);
            }

            return new ChangeSignatureAnalyzedContext(document.Project.Solution, symbol, parameterConfiguration);
        }

        private ChangeSignatureResult ChangeSignatureWithContext(ChangeSignatureAnalyzedContext context, CancellationToken cancellationToken)
        {
            var options = GetChangeSignatureOptions(context, CancellationToken.None);
            if (options.IsCancelled)
            {
                return new ChangeSignatureResult(succeeded: false);
            }

            return ChangeSignatureWithContext(context, options, cancellationToken);
        }

        internal ChangeSignatureResult ChangeSignatureWithContext(ChangeSignatureAnalyzedContext context, ChangeSignatureOptionsResult options, CancellationToken cancellationToken)
        {
            Solution updatedSolution;
            var succeeded = TryCreateUpdatedSolution(context, options, cancellationToken, out updatedSolution);
            return new ChangeSignatureResult(succeeded, updatedSolution, context.Symbol.ToDisplayString(), context.Symbol.GetGlyph(), options.PreviewChanges);
        }

        internal ChangeSignatureOptionsResult GetChangeSignatureOptions(ChangeSignatureAnalyzedContext context, CancellationToken cancellationToken)
        {
            var notificationService = context.Solution.Workspace.Services.GetService<INotificationService>();
            var changeSignatureOptionsService = context.Solution.Workspace.Services.GetService<IChangeSignatureOptionsService>();

            var isExtensionMethod = context.Symbol is IMethodSymbol && (context.Symbol as IMethodSymbol).IsExtensionMethod;
            return changeSignatureOptionsService.GetChangeSignatureOptions(context.Symbol, context.ParameterConfiguration, notificationService);
        }

        private static Task<IEnumerable<ReferencedSymbol>> FindChangeSignatureReferencesAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ChangeSignature, cancellationToken))
            {
                IImmutableSet<Document> documents = null;
                var engine = new FindReferencesSearchEngine(
                    solution,
                    documents,
                    ReferenceFinders.DefaultReferenceFinders.Add(DelegateInvokeMethodReferenceFinder.DelegateInvokeMethod),
                    FindReferencesProgress.Instance,
                    cancellationToken);

                return engine.FindReferencesAsync(symbol);
            }
        }

        private bool TryCreateUpdatedSolution(ChangeSignatureAnalyzedContext context, ChangeSignatureOptionsResult options, CancellationToken cancellationToken, out Solution updatedSolution)
        {
            updatedSolution = context.Solution;
            var declaredSymbol = context.Symbol;

            var nodesToUpdate = new Dictionary<DocumentId, List<SyntaxNode>>();
            var definitionToUse = new Dictionary<SyntaxNode, ISymbol>();

            bool hasLocationsInMetadata = false;

            var symbols = FindChangeSignatureReferencesAsync(declaredSymbol, context.Solution, cancellationToken).WaitAndGetResult(cancellationToken);

            foreach (var symbol in symbols)
            {
                if (symbol.Definition.Kind == SymbolKind.Method &&
                    ((symbol.Definition as IMethodSymbol).MethodKind == MethodKind.PropertyGet || (symbol.Definition as IMethodSymbol).MethodKind == MethodKind.PropertySet))
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

                if (symbolWithSyntacticParameters.Kind == SymbolKind.Event)
                {
                    var eventSymbol = symbolWithSyntacticParameters as IEventSymbol;
                    var type = eventSymbol.Type as INamedTypeSymbol;
                    if (type != null && type.DelegateInvokeMethod != null)
                    {
                        symbolWithSemanticParameters = type.DelegateInvokeMethod;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (symbolWithSyntacticParameters.Kind == SymbolKind.Method)
                {
                    var methodSymbol = symbolWithSyntacticParameters as IMethodSymbol;
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
                }

                // Find and annotate all the relevant definitions

                if (includeDefinitionLocations)
                {
                    foreach (var def in symbolWithSyntacticParameters.Locations)
                    {
                        DocumentId documentId;
                        SyntaxNode nodeToUpdate;
                        if (!TryGetNodeWithEditableSignatureOrAttributes(def, updatedSolution, out nodeToUpdate, out documentId))
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

                    DocumentId documentId2;
                    SyntaxNode nodeToUpdate2;
                    if (!TryGetNodeWithEditableSignatureOrAttributes(location.Location, updatedSolution, out nodeToUpdate2, out documentId2))
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
                var notificationService = context.Solution.Workspace.Services.GetService<INotificationService>();
                if (!notificationService.ConfirmMessageBox(FeaturesResources.ThisSymbolHasRelatedDefinitionsOrReferencesInMetadata, severity: NotificationSeverity.Warning))
                {
                    return false;
                }
            }

            // Construct all the relevant syntax trees from the base solution

            var updatedRoots = new Dictionary<DocumentId, SyntaxNode>();
            foreach (var docId in nodesToUpdate.Keys)
            {
                var doc = updatedSolution.GetDocument(docId);
                var updater = doc.Project.LanguageServices.GetService<AbstractChangeSignatureService>();
                var root = doc.GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

                var nodes = nodesToUpdate[docId];

                var newRoot = root.ReplaceNodes(nodes, (originalNode, potentiallyUpdatedNode) =>
                    {
                        return updater.ChangeSignature(doc, definitionToUse[originalNode], potentiallyUpdatedNode, originalNode, CreateCompensatingSignatureChange(definitionToUse[originalNode], options.UpdatedSignature), cancellationToken);
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

            foreach (var docId in nodesToUpdate.Keys)
            {
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(docId, updatedRoots[docId]);
            }

            return true;
        }

        private void AddUpdatableNodeToDictionaries(Dictionary<DocumentId, List<SyntaxNode>> nodesToUpdate, DocumentId documentId, SyntaxNode nodeToUpdate, Dictionary<SyntaxNode, ISymbol> definitionToUse, ISymbol symbolWithSemanticParameters)
        {
            nodesToUpdate[documentId].Add(nodeToUpdate);

            ISymbol sym;
            if (definitionToUse.TryGetValue(nodeToUpdate, out sym) && sym != symbolWithSemanticParameters)
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
            SyntaxNode node = root.FindNode(location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
            var updater = document.Project.LanguageServices.GetService<AbstractChangeSignatureService>();
            nodeToUpdate = updater.FindNodeToUpdate(document, node);

            return nodeToUpdate != null;
        }

        protected static List<IUnifiedArgumentSyntax> PermuteArguments(
            Document document,
            ISymbol declarationSymbol,
            List<IUnifiedArgumentSyntax> arguments,
            SignatureChange updatedSignature,
            bool isReducedExtensionMethod = false)
        {
            // 1. Determine which parameters are permutable

            var declarationParameters = declarationSymbol.GetParameters().ToList();
            var declarationParametersToPermute = GetParametersToPermute(arguments, declarationParameters, isReducedExtensionMethod);
            var argumentsToPermute = arguments.Take(declarationParametersToPermute.Count).ToList();

            // 2. Create an argument to parameter map, and a parameter to index map for the sort.
            var argumentToParameterMap = new Dictionary<IUnifiedArgumentSyntax, IParameterSymbol>();
            var parameterToIndexMap = new Dictionary<IParameterSymbol, int>();

            for (int i = 0; i < declarationParametersToPermute.Count; i++)
            {
                var decl = declarationParametersToPermute[i];
                var arg = argumentsToPermute[i];

                argumentToParameterMap[arg] = decl;
                var originalIndex = declarationParameters.IndexOf(decl);

                var updatedIndex = updatedSignature.GetUpdatedIndex(originalIndex);

                // If there's no value, then we may be handling a method with more parameters than the original symbol (like BeginInvoke).
                parameterToIndexMap[decl] = updatedIndex.HasValue ? updatedIndex.Value : -1;
            }

            // 3. Sort the arguments that need to be reordered

            argumentsToPermute.Sort((a1, a2) => { return parameterToIndexMap[argumentToParameterMap[a1]].CompareTo(parameterToIndexMap[argumentToParameterMap[a2]]); });

            // 4. Add names to arguments where necessary.

            var newArguments = new List<IUnifiedArgumentSyntax>();
            int expectedIndex = 0 + (isReducedExtensionMethod ? 1 : 0);
            bool seenNamedArgument = false;

            foreach (var argument in argumentsToPermute)
            {
                var param = argumentToParameterMap[argument];
                var actualIndex = updatedSignature.GetUpdatedIndex(declarationParameters.IndexOf(param));

                if (!actualIndex.HasValue)
                {
                    continue;
                }

                if ((seenNamedArgument || actualIndex != expectedIndex) && !argument.IsNamed)
                {
                    newArguments.Add(argument.WithName(param.Name).WithAdditionalAnnotations(Formatter.Annotation));
                    seenNamedArgument = true;
                }
                else
                {
                    newArguments.Add(argument);
                }

                seenNamedArgument |= argument.IsNamed;
                expectedIndex++;
            }

            // 5. Add the remaining arguments. These will already have names or be params arguments, but may have been removed.

            bool removedParams = updatedSignature.OriginalConfiguration.ParamsParameter != null && updatedSignature.UpdatedConfiguration.ParamsParameter == null;

            for (int i = declarationParametersToPermute.Count; i < arguments.Count; i++)
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
                var origStuff = updatedSignature.OriginalConfiguration.ToListOfParameters();
                var newStuff = updatedSignature.UpdatedConfiguration.ToListOfParameters();

                var realStuff = declarationSymbol.GetParameters();

                var bonusParameters = realStuff.Skip(origStuff.Count);

                origStuff.AddRange(bonusParameters);
                newStuff.AddRange(bonusParameters);

                var newOrigParams = ParameterConfiguration.Create(origStuff, updatedSignature.OriginalConfiguration.ThisParameter != null);
                var newUpdatedParams = ParameterConfiguration.Create(newStuff, updatedSignature.OriginalConfiguration.ThisParameter != null);
                updatedSignature = new SignatureChange(newOrigParams, newUpdatedParams);
            }

            return updatedSignature;
        }

        private static List<IParameterSymbol> GetParametersToPermute(
            List<IUnifiedArgumentSyntax> arguments,
            List<IParameterSymbol> originalParameters,
            bool isReducedExtensionMethod)
        {
            int position = -1 + (isReducedExtensionMethod ? 1 : 0);
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
    }
}
