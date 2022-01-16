// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractClass
{
    internal class ExtractClassWithDialogCodeAction : CodeActionWithOptions
    {
        private readonly Document _document;
        private readonly ISymbol? _selectedMember;
        private readonly INamedTypeSymbol _selectedType;
        private readonly SyntaxNode _selectedTypeDeclarationNode;
        private readonly IExtractClassOptionsService _service;

        public TextSpan Span { get; }
        public override string Title => FeaturesResources.Extract_base_class;

        public ExtractClassWithDialogCodeAction(
            Document document,
            TextSpan span,
            IExtractClassOptionsService service,
            INamedTypeSymbol selectedType,
            SyntaxNode selectedTypeDeclarationNode,
            ISymbol? selectedMember = null)
        {
            _document = document;
            _service = service;
            _selectedType = selectedType;
            _selectedTypeDeclarationNode = selectedTypeDeclarationNode;
            _selectedMember = selectedMember;
            Span = span;
        }

        public override object? GetOptions(CancellationToken cancellationToken)
        {
            var extractClassService = _service ?? _document.Project.Solution.Workspace.Services.GetRequiredService<IExtractClassOptionsService>();
            return extractClassService.GetExtractClassOptionsAsync(_document, _selectedType, _selectedMember, cancellationToken)
                .WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is ExtractClassOptions extractClassOptions)
            {
                // Map the symbols we're removing to annotations
                // so we can find them easily
                var codeGenerator = _document.GetRequiredLanguageService<ICodeGenerationService>();
                var symbolMapping = await AnnotatedSymbolMapping.CreateAsync(
                    extractClassOptions.MemberAnalysisResults.Select(m => m.Member),
                    _document.Project.Solution,
                    _selectedTypeDeclarationNode,
                    cancellationToken).ConfigureAwait(false);

                var namespaceService = _document.GetRequiredLanguageService<AbstractExtractInterfaceService>();

                // Create the symbol for the new type 
                var newType = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                    _selectedType.GetAttributes(),
                    _selectedType.DeclaredAccessibility,
                    _selectedType.GetSymbolModifiers(),
                    TypeKind.Class,
                    extractClassOptions.TypeName);

                var containingNamespaceDisplay = namespaceService.GetContainingNamespaceDisplay(
                    _selectedType,
                    _document.Project.CompilationOptions);

                // Add the new type to the solution. It can go in a new file or
                // be added to an existing. The returned document is always the document
                // containing the new type
                var (updatedDocument, typeAnnotation) = extractClassOptions.SameFile
                    ? await ExtractTypeHelpers.AddTypeToExistingFileAsync(
                        symbolMapping.AnnotatedSolution.GetRequiredDocument(_document.Id),
                        newType,
                        symbolMapping,
                        cancellationToken).ConfigureAwait(false)
                    : await ExtractTypeHelpers.AddTypeToNewFileAsync(
                        symbolMapping.AnnotatedSolution,
                        containingNamespaceDisplay,
                        extractClassOptions.FileName,
                        _document.Project.Id,
                        _document.Folders,
                        newType,
                        _document,
                        cancellationToken).ConfigureAwait(false);

                // Update the original type to have the new base
                var solutionWithUpdatedOriginalType = await GetSolutionWithBaseAddedAsync(
                    updatedDocument.Project.Solution,
                    symbolMapping,
                    newType,
                    extractClassOptions.MemberAnalysisResults,
                    cancellationToken).ConfigureAwait(false);

                // After all the changes, make sure we're using the most up to date symbol 
                // as the destination for pulling members into
                var documentWithTypeDeclaration = solutionWithUpdatedOriginalType.GetRequiredDocument(updatedDocument.Id);
                var newTypeAfterEdits = await GetNewTypeSymbolAsync(documentWithTypeDeclaration, typeAnnotation, cancellationToken).ConfigureAwait(false);

                // Use Members Puller to move the members to the new symbol
                var finalSolution = await PullMembersUpAsync(
                    solutionWithUpdatedOriginalType,
                    newTypeAfterEdits,
                    symbolMapping,
                    extractClassOptions.MemberAnalysisResults,
                    cancellationToken).ConfigureAwait(false);

                return new[] { new ApplyChangesOperation(finalSolution) };
            }
            else
            {
                // If user click cancel button, options will be null and hit this branch
                return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
            }
        }

        private async Task<Solution> PullMembersUpAsync(
            Solution solution,
            INamedTypeSymbol newType,
            AnnotatedSymbolMapping symbolMapping,
            ImmutableArray<ExtractClassMemberAnalysisResult> memberAnalysisResults,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<(ISymbol member, bool makeAbstract)>.GetInstance(out var pullMembersBuilder);
            using var _1 = ArrayBuilder<ExtractClassMemberAnalysisResult>.GetInstance(memberAnalysisResults.Length, out var remainingResults);
            remainingResults.AddRange(memberAnalysisResults);

            // For each document in the symbol mappings, we want to find the annotated nodes
            // of the members and get the current symbol that represents those after
            // any changes we made before members get pulled up into the base class.
            // We only need to worry about symbols that are actually being moved, so we track
            // the symbols from the member analysis. 
            foreach (var (documentId, symbols) in symbolMapping.DocumentIdsToSymbolMap)
            {
                if (remainingResults.Count == 0)
                {
                    // All symbols have been taken care of
                    break;
                }

                var document = solution.GetRequiredDocument(documentId);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                using var _2 = ArrayBuilder<ExtractClassMemberAnalysisResult>.GetInstance(remainingResults.Count, out var resultsToRemove);

                // Out of the remaining members that we need to move, does this
                // document contain the definition for that symbol? If so, add it to the builder
                // and remove it from the symbols we're looking for
                var memberAnalysisForDocumentSymbols = remainingResults.Where(analysis => symbols.Contains(analysis.Member));

                foreach (var memberAnalysis in memberAnalysisForDocumentSymbols)
                {
                    var annotation = symbolMapping.SymbolToDeclarationAnnotationMap[memberAnalysis.Member];

                    var nodeOrToken = root.GetAnnotatedNodesAndTokens(annotation).SingleOrDefault();
                    var node = nodeOrToken.IsNode
                        ? nodeOrToken.AsNode()
                        : nodeOrToken.AsToken().Parent;

                    // If the node is null then the symbol mapping was wrong about
                    // the document containing the symbol.
                    RoslynDebug.AssertNotNull(node);

                    var currentSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

                    // If currentSymbol is null then no symbol is declared at the node and 
                    // symbol mapping state is not right.
                    RoslynDebug.AssertNotNull(currentSymbol);

                    pullMembersBuilder.Add((currentSymbol, memberAnalysis.MakeAbstract));
                    resultsToRemove.Add(memberAnalysis);
                }

                // Remove the symbols we found in this document from the list 
                // that we are looking for
                foreach (var resultToRemove in resultsToRemove)
                {
                    remainingResults.Remove(resultToRemove);
                }
            }

            // If we didn't find all of the symbols then something went really wrong
            Contract.ThrowIfFalse(remainingResults.Count == 0);

            var pullMemberUpOptions = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(newType, pullMembersBuilder.ToImmutable());
            var updatedOriginalDocument = solution.GetRequiredDocument(_document.Id);

            return await MembersPuller.PullMembersUpAsync(updatedOriginalDocument, pullMemberUpOptions, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<INamedTypeSymbol> GetNewTypeSymbolAsync(Document document, SyntaxAnnotation typeAnnotation, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationNode = root.GetAnnotatedNodes(typeAnnotation).Single();

            return (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(declarationNode, cancellationToken);
        }

        private static async Task<Solution> GetSolutionWithBaseAddedAsync(
            Solution solution,
            AnnotatedSymbolMapping symbolMapping,
            INamedTypeSymbol newType,
            ImmutableArray<ExtractClassMemberAnalysisResult> memberAnalysisResults,
            CancellationToken cancellationToken)
        {
            var unformattedSolution = solution;
            var remainingResults = new List<ExtractClassMemberAnalysisResult>(memberAnalysisResults);

            foreach (var documentId in symbolMapping.DocumentIdsToSymbolMap.Keys)
            {
                if (remainingResults.IsEmpty())
                {
                    // All results have been taken care of
                    break;
                }

                var document = solution.GetRequiredDocument(documentId);
                var currentRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var typeDeclaration = currentRoot.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).SingleOrDefault();
                if (typeDeclaration == null)
                {
                    continue;
                }

                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var typeReference = syntaxGenerator.TypeExpression(newType);

                currentRoot = currentRoot.ReplaceNode(typeDeclaration,
                    syntaxGenerator.AddBaseType(typeDeclaration, typeReference));

                unformattedSolution = document.WithSyntaxRoot(currentRoot).Project.Solution;

                // Only need to update on declaration of the type
                break;
            }

            return unformattedSolution;
        }
    }
}
