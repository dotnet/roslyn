// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

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
        private readonly TextSpan _span;
        private readonly IExtractClassOptionsService _service;

        public ExtractClassWithDialogCodeAction(Document document, TextSpan span, IExtractClassOptionsService service, INamedTypeSymbol selectedType, ISymbol selectedMember)
            : this(document, span, service, selectedType)
        {
            _selectedMember = selectedMember;

            Title = string.Format(FeaturesResources.Pull_0_up_to_new_base_class, selectedMember.ToNameDisplayString());
        }

        public ExtractClassWithDialogCodeAction(Document document, TextSpan span, IExtractClassOptionsService service, INamedTypeSymbol selectedType)
        {
            _document = document;
            _span = span;
            _service = service;
            _selectedType = selectedType;

            Title = FeaturesResources.Extract_new_base_class;
        }

        public override string Title { get; }

        public override object? GetOptions(CancellationToken cancellationToken)
        {
            var extractClassService = _service ?? _document.Project.Solution.Workspace.Services.GetRequiredService<IExtractClassOptionsService>();
            return extractClassService.GetExtractClassOptionsAsync(_document, _selectedType, _selectedMember)
                .WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is ExtractClassOptions extractClassOptions)
            {
                // Find the original type
                var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
                var root = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var originalTypeDeclaration = root.FindNode(_span).FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration);

                if (originalTypeDeclaration is null)
                    throw new InvalidOperationException();

                // Map the symbols we're removing to annotations
                // so we can find them easily
                var codeGenerator = _document.GetRequiredLanguageService<ICodeGenerationService>();
                var symbolMapping = await AnnotatedSymbolMapping.CreateAsync(
                    extractClassOptions.MemberAnalysisResults.Select(m => m.Member),
                    _document.Project.Solution,
                    originalTypeDeclaration,
                    cancellationToken).ConfigureAwait(false);

                var fileBanner = syntaxFacts.GetFileBanner(root);
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
                var updatedDocument = extractClassOptions.SameFile
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
                        fileBanner,
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
                newType = await GetNewTypeSymbolAsync(documentWithTypeDeclaration, newType, cancellationToken).ConfigureAwait(false);

                // Use Members Puller to move the members to the new symbol
                var finalSolution = await PullMembersUpAsync(
                    solutionWithUpdatedOriginalType,
                    newType,
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

            var remainingResults = new List<ExtractClassMemberAnalysisResult>(memberAnalysisResults);

            foreach (var documentId in symbolMapping.DocumentIds)
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

                var typeDeclaration = root.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).SingleOrDefault();
                if (typeDeclaration == null)
                {
                    continue;
                }

                foreach (var memberAnalysis in remainingResults.ToArray())
                {
                    var annotation = symbolMapping.SymbolToDeclarationAnnotationMap[memberAnalysis.Member];

                    var nodeOrToken = typeDeclaration.GetAnnotatedNodesAndTokens(annotation).SingleOrDefault();
                    var node = nodeOrToken.IsNode
                        ? nodeOrToken.AsNode()
                        : nodeOrToken.AsToken().Parent;

                    if (node is null)
                    {
                        continue;
                    }

                    var currentSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                    if (currentSymbol is null)
                    {
                        continue;
                    }

                    pullMembersBuilder.Add((currentSymbol, memberAnalysis.MakeAbstract));
                    remainingResults.Remove(memberAnalysis);
                }
            }

            Contract.ThrowIfFalse(remainingResults.Count == 0);

            var pullMemberUpOptions = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(newType, pullMembersBuilder.ToImmutable());
            var updatedOriginalDocument = solution.GetRequiredDocument(_document.Id);

            return await MembersPuller.PullMembersUpAsync(updatedOriginalDocument, pullMemberUpOptions, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<INamedTypeSymbol> GetNewTypeSymbolAsync(Document document, INamedTypeSymbol typeToBeLookedFor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return (INamedTypeSymbol)semanticModel.Compilation.GetSymbolsWithName(typeToBeLookedFor.Name, SymbolFilter.Type, cancellationToken).Single();
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

            foreach (var documentId in symbolMapping.DocumentIds)
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
