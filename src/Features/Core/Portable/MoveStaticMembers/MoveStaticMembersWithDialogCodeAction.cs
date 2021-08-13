// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveStaticMembers
{
    internal class MoveStaticMembersWithDialogCodeAction : CodeActionWithOptions
    {
        private readonly Document _document;
        private readonly ISymbol? _selectedMember;
        private readonly INamedTypeSymbol _selectedType;
        private readonly IMoveStaticMembersOptionsService _service;

        public TextSpan Span { get; }
        public override string Title => FeaturesResources.Move_static_members_to_another_type;

        public MoveStaticMembersWithDialogCodeAction(
            Document document,
            TextSpan span,
            IMoveStaticMembersOptionsService service,
            INamedTypeSymbol selectedType,
            ISymbol? selectedMember = null)
        {
            _document = document;
            _service = service;
            _selectedType = selectedType;
            _selectedMember = selectedMember;
            Span = span;
        }

        public override object? GetOptions(CancellationToken cancellationToken)
        {
            return _service.GetMoveMembersToTypeOptions(_document, _selectedType, _selectedMember);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is not MoveStaticMembersOptions moveOptions || moveOptions.IsCancelled)
            {
                return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
            }

            // Find the original doc root
            var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var fileBanner = syntaxFacts.GetFileBanner(root);

            // add annotations to the symbols that we selected so we can find them later to pull up
            // These symbols should all have (singular) definitions, but in the case that we can't find
            // any location, we just won't move that particular symbol
            var memberNodes = moveOptions.SelectedMembers
                .Select(symbol => symbol.Locations.FirstOrDefault())
                .WhereNotNull()
                .SelectAsArray(loc => loc.FindNode(cancellationToken));
            root = root.TrackNodes(memberNodes);
            var sourceDoc = _document.WithSyntaxRoot(root);

            var typeParameters = ExtractTypeHelpers.GetRequiredTypeParametersForMembers(_selectedType, moveOptions.SelectedMembers);
            // even though we can move members here, we will move them by calling PullMembersUp
            var newType = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                ImmutableArray.Create<AttributeData>(),
                Accessibility.NotApplicable,
                DeclarationModifiers.Static,
                GetNewTypeKind(typeParameters),
                moveOptions.TypeName,
                typeParameters: typeParameters);

            var (newDoc, annotation) = await ExtractTypeHelpers.AddTypeToNewFileAsync(
                sourceDoc.Project.Solution,
                moveOptions.NamespaceDisplay,
                moveOptions.FileName,
                _document.Project.Id,
                _document.Folders,
                newType,
                fileBanner,
                cancellationToken).ConfigureAwait(false);

            // get back type declaration in the newly created file
            var destRoot = await newDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var destSemanticModel = await newDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            newType = destSemanticModel.GetRequiredDeclaredSymbol(destRoot.GetAnnotatedNodes(annotation).Single(), cancellationToken) as INamedTypeSymbol;

            // refactor member access references in other files
            var memberReferenceLocations = await FindMemberReferencesAsync(moveOptions.SelectedMembers, newDoc.Project.Solution, cancellationToken).ConfigureAwait(false);
            var locationsToDoc = memberReferenceLocations.ToLookup(loc => loc.location.Document.Id);
            var reReferencedSolution = await RefactorReferencesAsync(locationsToDoc, newDoc.Project.Solution, newType!, cancellationToken).ConfigureAwait(false);

            // Possibly convert members to non-static or static if we move to/from a module
            sourceDoc = await CorrectStaticMembersAsync(reReferencedSolution.GetRequiredDocument(sourceDoc.Id), memberNodes, newType!, cancellationToken).ConfigureAwait(false);

            // get back nodes from our changes
            root = await sourceDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await sourceDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var members = memberNodes
                .Select(node => root.GetCurrentNode(node))
                .WhereNotNull()
                .SelectAsArray(node => (semanticModel.GetDeclaredSymbol(node!, cancellationToken), false));

            var pullMembersUpOptions = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(newType!, members);
            var movedSolution = await MembersPuller.PullMembersUpAsync(sourceDoc, pullMembersUpOptions, cancellationToken).ConfigureAwait(false);

            return new CodeActionOperation[] { new ApplyChangesOperation(movedSolution) };
        }

        /// <summary>
        /// What type kind we should make our new type be. Passing Module when our language is C# will still create a class.
        /// Modules cannot be generic however, so for VB we choose class if there are generic type params.
        /// This could be later extended to a language-service feature if there is more complex behavior.
        /// </summary>
        private static TypeKind GetNewTypeKind(ImmutableArray<ITypeParameterSymbol> typeParameters)
        {
            return typeParameters.IsEmpty ? TypeKind.Module : TypeKind.Class;
        }

        private async Task<Document> CorrectStaticMembersAsync(
            Document sourceDoc,
            ImmutableArray<SyntaxNode> memberNodes,
            INamedTypeSymbol newType,
            CancellationToken cancellationToken)
        {
            // We have two cases:
            // 1. Moving from a class to a module. We need to remove the shared modifier as it is implied
            // 2. Moving from a module to a class. We need to add the shared modifier as it is no longer implied
            // If neither of these apply (the movement types are both classes or both modules), we return early
            if (newType.TypeKind == _selectedType.TypeKind)
            {
                return sourceDoc;
            }

            var docEditor = await DocumentEditor.CreateAsync(sourceDoc, cancellationToken).ConfigureAwait(false);
            memberNodes = docEditor.OriginalRoot.GetCurrentNodes(memberNodes).ToImmutableArray();
            // need to make members non-static if we're moving to a module, static if we're moving away from one
            foreach (var node in memberNodes)
            {
                docEditor.ReplaceNode(node, (n, generator) => generator.WithModifiers(n, generator.GetModifiers(n).WithIsStatic(newType.TypeKind != TypeKind.Module)));
            }

            return docEditor.GetChangedDocument();
        }

        private static async Task<Solution> RefactorReferencesAsync(
            ILookup<DocumentId, (ReferenceLocation location, bool isExtension)> locationsToDoc,
            Solution solution,
            INamedTypeSymbol newType,
            CancellationToken cancellationToken)
        {
            var solutionEditor = new SolutionEditor(solution);
            foreach (var docGroup in locationsToDoc)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var docEditor = await solutionEditor.GetDocumentEditorAsync(docGroup.Key, cancellationToken).ConfigureAwait(false);
                FixReferencesSingleDocument(docGroup.AsImmutable(), docEditor, newType, cancellationToken);
            }

            return solutionEditor.GetChangedSolution();
        }

        private static void FixReferencesSingleDocument(
            ImmutableArray<(ReferenceLocation location, bool isExtension)> referenceLocations,
            DocumentEditor docEditor,
            INamedTypeSymbol newType,
            CancellationToken cancellationToken)
        {
            foreach (var (refLoc, isExtension) in referenceLocations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // All of these references are not easily fixable and possibly incorrect, so we are ok with ignoring these, even if it produces an error
                if (refLoc.IsCandidateLocation || refLoc.IsImplicit || refLoc.Alias != null)
                {
                    continue;
                }

                var doc = docEditor.OriginalDocument;

                var syntaxFacts = doc.GetRequiredLanguageService<ISyntaxFactsService>();
                var refNode = docEditor.GetChangedRoot().FindNode(refLoc.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
                // track this node in case the doc has changed
                docEditor.ReplaceNode(refNode, refNode.TrackNodes(refNode));

                // add imports for each reference as there may be multiple locations that we need to add to
                // if the import is already there, we shouldn't add it again
                var addImports = doc.GetRequiredLanguageService<IAddImportsService>();
                docEditor.ReplaceNode(docEditor.OriginalRoot, (node, generator) => addImports.AddImport(
                    docEditor.SemanticModel.Compilation,
                    node,
                    node.GetCurrentNode(refNode)!,
                    generator.NamespaceImportDeclaration(
                        newType.ContainingNamespace.ToDisplayString(SymbolDisplayFormats.NameFormat))
                        .WithAdditionalAnnotations(Simplification.Simplifier.Annotation, Formatting.Formatter.Annotation),
                    generator,
                    true,
                    doc.CanAddImportsInHiddenRegions(),
                    cancellationToken));

                // now change the actual references to use the new type name
                // extension methods do not need to be changed as we do not reference the class name
                if (isExtension)
                {
                    continue;
                }

                if (syntaxFacts.IsNameOfAnyMemberAccessExpression(refNode))
                {
                    var expression = syntaxFacts.GetExpressionOfMemberAccessExpression(refNode.Parent);
                    if (expression != null)
                    {
                        docEditor.ReplaceNode(expression, (node, generator) => generator.TypeExpression(newType).WithTriviaFrom(node));
                    }
                }
            }
        }

        private static async Task<ImmutableArray<(ReferenceLocation location, bool isExtension)>> FindMemberReferencesAsync(
            ImmutableArray<ISymbol> members,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var tasks = members.Select(symbol => SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken));
            var symbolRefs = await Task.WhenAll(tasks).ConfigureAwait(false);
            return symbolRefs
                .Flatten()
                .SelectMany(refSymbol => refSymbol.Locations.Select(loc => (loc, refSymbol.Definition.IsExtensionMethod())))
                .ToImmutableArray();
        }
    }
}
