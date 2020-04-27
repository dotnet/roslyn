// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal abstract class AbstractMoveMembersService : ILanguageService
    {
        protected abstract Task<SyntaxNode?> GetSelectedMemberNodeAsync(Document document, TextSpan selection, CancellationToken cancellationToken);

        protected abstract Task<Solution> UpdateMembersWithExplicitImplementationsAsync(
            Solution unformattedSolution,
            IReadOnlyList<DocumentId> documentId,
            INamedTypeSymbol extractedInterfaceSymbol,
            IEnumerable<ISymbol> includedMembers,
            Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            CancellationToken cancellationToken);

        /// <summary>
        /// Analyzes the selection to find selected member or type for items to move. 
        /// 
        /// Selected member is prioritized first, but if a member is not found selected a containing type 
        /// for the selection is used.
        /// </summary>
        public async Task<MoveMembersAnalysisResult?> AnalyzeAsync(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var selectedMemberNode = await GetSelectedMemberNodeAsync(document, selection, cancellationToken).ConfigureAwait(false);
            var selectedMember = selectedMemberNode is null
                ? null
                : semanticModel.GetDeclaredSymbol(selectedMemberNode) ?? semanticModel.GetSymbolInfo(selectedMemberNode).Symbol;

            if (MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                var destinations = FindAllValidDestinations(selectedMember, document.Project.Solution, cancellationToken);
                var selectedType = selectedMember.ContainingType;
                var destinationAnalysisResults = AnalyzeDestinations(destinations);

                return new MoveMembersAnalysisResult(selectedType, selectedMember, selectedMemberNode!, destinationAnalysisResults);
            }
            else
            {
                var typeDeclaration = await GetTypeDeclarationAsync(document, selection, cancellationToken).ConfigureAwait(false);

                if (typeDeclaration is null)
                {
                    return null;
                }

                var selectedType = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(typeDeclaration);
                return new MoveMembersAnalysisResult(selectedType, selectedMember: null, typeDeclaration, ImmutableArray<DestinationAnalysisResult>.Empty);
            }
        }

        public async Task<MoveMembersResult> MoveMembersAsync(Document document, MoveMembersOptions membersOptions, CancellationToken cancellationToken)
        {
            var errorResult = CheckOptionValidity(document, membersOptions, cancellationToken);
            if (errorResult is object)
            {
                return errorResult;
            }

            if (membersOptions.DestinationIsNewType)
            {
                return await ExtractToNewTypeAsync(document, membersOptions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await ExtractToExistingTypeAsync(document, membersOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<SyntaxNode?> GetTypeDeclarationAsync(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(selection);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return node.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration);
        }

        private MoveMembersResult? CheckOptionValidity(Document document, MoveMembersOptions membersOptions, CancellationToken cancellationToken)
        {
            if (membersOptions.Destination.TypeKind == TypeKind.Interface)
            {
                if (!membersOptions.MembersToMove.Any(m => IsExtractableMemberToInterface(m.Member)))
                {
                    var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_type_does_not_contain_any_member_that_can_be_extracted_to_an_interface;
                    return new MoveMembersResult(errorMessage);
                }
            }


            return null;

            static bool IsExtractableMemberToInterface(ISymbol member)
            {
                if (member.IsStatic ||
                    member.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }

                if (member.Kind == SymbolKind.Event || member.IsOrdinaryMethod())
                {
                    return true;
                }

                if (member is IPropertySymbol prop)
                {
                    return (prop.GetMethod != null && prop.GetMethod.DeclaredAccessibility == Accessibility.Public) ||
                        (prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public);
                }

                return false;
            }
        }

        private async Task<MoveMembersResult> ExtractToExistingTypeAsync(Document document, MoveMembersOptions membersOptions, CancellationToken cancellationToken)
        {
            return new MoveMembersResult(
                await MembersPuller.PullMembersUpAsync(document, membersOptions, cancellationToken).ConfigureAwait(false),
                document.Id);
        }

        private async Task<MoveMembersResult> ExtractToNewTypeAsync(Document document, MoveMembersOptions membersOptions, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var extractedMembers = membersOptions.MembersToMove.SelectAsArray(m => m.Member);

            // Assume we need to create a new document for the type
            // Track all of the symbols we need to modify, which includes the original type declaration being modified
            var symbolMapping = await CreateSymbolMappingAsync(
                extractedMembers,
                solution,
                membersOptions.FromTypeNode,
                cancellationToken).ConfigureAwait(false);

            var navigationId = membersOptions.DestinationDocument;

            // If navigationId is set, we're going to put the interface in an existing document
            if (navigationId is object)
            {
                var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var typeDeclaration = originalRoot.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).Single();

                var trackedDocument = document.WithSyntaxRoot(originalRoot.TrackNodes(typeDeclaration));

                var currentRoot = await trackedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(currentRoot, symbolMapping.AnnotatedSolution.Workspace);

                var codeGenService = trackedDocument.GetRequiredLanguageService<ICodeGenerationService>();
                var interfaceNode = codeGenService.CreateNamedTypeDeclaration(membersOptions.Destination)
                    .WithAdditionalAnnotations(SimplificationHelpers.SimplifyModuleNameAnnotation);

                typeDeclaration = currentRoot.GetCurrentNode(typeDeclaration);
                editor.InsertBefore(typeDeclaration, interfaceNode);

                solution = document.WithSyntaxRoot(editor.GetChangedRoot()).Project.Solution;
            }
            else
            {
                Contract.ThrowIfNull(membersOptions.NewFileName);

                var newDocumentId = DocumentId.CreateNewId(document.Project.Id, debugName: membersOptions.NewFileName);
                navigationId = newDocumentId;

                var solutionWithInterfaceDocument = solution.AddDocument(newDocumentId, membersOptions.NewFileName, text: "", folders: document.Folders);
                var newDocument = solutionWithInterfaceDocument.GetRequiredDocument(newDocumentId);
                var interfaceDocumentSemanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var containingNamespace = membersOptions.OriginalType.ContainingNamespace.IsGlobalNamespace
                    ? string.Empty
                    : membersOptions.OriginalType.ContainingNamespace.ToDisplayString();

                var namespaceParts = containingNamespace.Split('.').Where(s => !string.IsNullOrEmpty(s));

                var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var fileBanner = syntaxFactsService.GetFileBanner(originalRoot);

                var unformattedNewTypeDocument = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                    newDocument.Project.Solution,
                    interfaceDocumentSemanticModel.GetEnclosingNamespace(0, cancellationToken),
                    membersOptions.Destination.GenerateRootNamespaceOrType(namespaceParts.ToArray()),
                    options: new CodeGenerationOptions(contextLocation: interfaceDocumentSemanticModel.SyntaxTree.GetLocation(new TextSpan()), generateMethodBodies: false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var syntaxRoot = await unformattedNewTypeDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                newDocument = unformattedNewTypeDocument.WithSyntaxRoot(syntaxRoot.WithPrependedLeadingTrivia(fileBanner));

                solution = newDocument.Project.Solution;
            }

            // After the interface is inserted, update the original type to show it implements the new interface
            var unformattedSolutionWithUpdatedType = await GetSolutionWithOriginalTypeUpdatedAsync(
                    solution,
                    symbolMapping.DocumentIds,
                    symbolMapping.TypeNodeAnnotation,
                    membersOptions.OriginalType,
                    membersOptions.Destination,
                    extractedMembers,
                    symbolMapping.SymbolToDeclarationAnnotationMap,
                    cancellationToken).ConfigureAwait(false);

            var completedSolution = await GetFormattedSolutionAsync(
                unformattedSolutionWithUpdatedType,
                symbolMapping.DocumentIds.Concat(document.Id),
                cancellationToken).ConfigureAwait(false);

            return new MoveMembersResult(completedSolution, navigationId!);
        }

        private async Task<SymbolMapping> CreateSymbolMappingAsync(
                IEnumerable<ISymbol> includedMembers,
                Solution solution,
                SyntaxNode typeNode,
                CancellationToken cancellationToken)
        {
            var symbolToDeclarationAnnotationMap = new Dictionary<ISymbol, SyntaxAnnotation>();
            var currentRoots = new Dictionary<SyntaxTree, SyntaxNode>();
            var documentIds = new List<DocumentId>();

            var typeNodeRoot = await typeNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var typeNodeAnnotation = new SyntaxAnnotation();
            currentRoots[typeNode.SyntaxTree] = typeNodeRoot.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(typeNodeAnnotation));
            documentIds.Add(solution.GetRequiredDocument(typeNode.SyntaxTree).Id);

            foreach (var includedMember in includedMembers)
            {
                var location = includedMember.Locations.Single();
                var tree = location.SourceTree!;
                if (!currentRoots.TryGetValue(tree, out var root))
                {
                    root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    documentIds.Add(solution.GetRequiredDocument(tree).Id);
                }

                var token = root.FindToken(location.SourceSpan.Start);

                var annotation = new SyntaxAnnotation();
                symbolToDeclarationAnnotationMap.Add(includedMember, annotation);
                currentRoots[tree] = root.ReplaceToken(token, token.WithAdditionalAnnotations(annotation));
            }

            var annotatedSolution = solution;
            foreach (var root in currentRoots)
            {
                var document = annotatedSolution.GetRequiredDocument(root.Key);
                annotatedSolution = document.WithSyntaxRoot(root.Value).Project.Solution;
            }

            return new SymbolMapping(symbolToDeclarationAnnotationMap, annotatedSolution, documentIds, typeNodeAnnotation);
        }

        private async Task<Solution> GetSolutionWithOriginalTypeUpdatedAsync(
            Solution solution,
            List<DocumentId> documentIds,
            SyntaxAnnotation typeNodeAnnotation,
            INamedTypeSymbol typeToExtractFrom,
            INamedTypeSymbol extractedTypeSymbol,
            IEnumerable<ISymbol> includedMembers,
            Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            CancellationToken cancellationToken)
        {
            // If an interface "INewInterface" is extracted from an interface "IExistingInterface",
            // then "INewInterface" is not marked as implementing "IExistingInterface" and its 
            // extracted members are also not updated.
            if (typeToExtractFrom.TypeKind == TypeKind.Interface)
            {
                return solution;
            }

            var unformattedSolution = solution;
            foreach (var documentId in documentIds)
            {
                var document = solution.GetRequiredDocument(documentId);
                var currentRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(currentRoot, solution.Workspace);

                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var typeReference = syntaxGenerator.TypeExpression(extractedTypeSymbol);

                var typeDeclaration = currentRoot.GetAnnotatedNodes(typeNodeAnnotation).SingleOrDefault();

                if (typeDeclaration == null)
                {
                    continue;
                }

                var unformattedTypeDeclaration = syntaxGenerator.AddInterfaceType(typeDeclaration, typeReference).WithAdditionalAnnotations(Formatter.Annotation);
                editor.ReplaceNode(typeDeclaration, unformattedTypeDeclaration);

                unformattedSolution = document.WithSyntaxRoot(editor.GetChangedRoot()).Project.Solution;

                // Only update the first instance of the typedeclaration,
                // since it's not needed in all declarations
                break;
            }

            var updatedUnformattedSolution = await UpdateMembersWithExplicitImplementationsAsync(
                unformattedSolution,
                documentIds,
                extractedTypeSymbol,
                includedMembers,
                symbolToDeclarationAnnotationMap,
                cancellationToken).ConfigureAwait(false);

            return updatedUnformattedSolution;
        }

        private static ImmutableArray<DestinationAnalysisResult> AnalyzeDestinations(ImmutableArray<INamedTypeSymbol> destinations)
        {
            return destinations.Select(
                d => new DestinationAnalysisResult(
                    d,
                    d
                    .GetMembers()
                    .Select(m => new MemberAnalysisResult(member: m))
                    .ToImmutableArray()))
            .ToImmutableArray();
        }

        private static ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
                ISymbol selectedMember,
                Solution solution,
                CancellationToken cancellationToken)
        {
            var containingType = selectedMember.ContainingType;
            var allDestinations = selectedMember.IsKind(SymbolKind.Field)
                ? containingType.GetBaseTypes().ToImmutableArray()
                : containingType.AllInterfaces.Concat(containingType.GetBaseTypes()).ToImmutableArray();

            return allDestinations.WhereAsArray(destination => MemberAndDestinationValidator.IsDestinationValid(solution, destination, cancellationToken));
        }

        private static async Task<Solution> GetFormattedSolutionAsync(Solution unformattedSolution, IEnumerable<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            // Since code action performs formatting and simplification on a single document, 
            // this ensures that anything marked with formatter or simplifier annotations gets 
            // correctly handled as long as it it's in the listed documents
            var formattedSolution = unformattedSolution;
            foreach (var documentId in documentIds)
            {
                var document = formattedSolution.GetDocument(documentId);
                var formattedDocument = await Formatter.FormatAsync(
                    document,
                    Formatter.Annotation,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var simplifiedDocument = await Simplifier.ReduceAsync(
                    formattedDocument,
                    Simplifier.Annotation,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                formattedSolution = simplifiedDocument.Project.Solution;
            }

            return formattedSolution;
        }

        private readonly struct SymbolMapping
        {
            public SymbolMapping(
                Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
                Solution annotatedSolution,
                List<DocumentId> documentIds,
                SyntaxAnnotation typeNodeAnnotation)
            {
                SymbolToDeclarationAnnotationMap = symbolToDeclarationAnnotationMap;
                AnnotatedSolution = annotatedSolution;
                DocumentIds = documentIds;
                TypeNodeAnnotation = typeNodeAnnotation;
            }

            public Dictionary<ISymbol, SyntaxAnnotation> SymbolToDeclarationAnnotationMap { get; }
            public Solution AnnotatedSolution { get; }
            public List<DocumentId> DocumentIds { get; }
            public SyntaxAnnotation TypeNodeAnnotation { get; }
        }
    }
}
