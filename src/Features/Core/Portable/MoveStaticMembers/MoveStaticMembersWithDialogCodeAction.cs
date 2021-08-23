// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.Simplification;
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

            // refactor references across the entire solution
            var memberReferenceLocations = await FindMemberReferencesAsync(moveOptions.SelectedMembers, newDoc.Project.Solution, cancellationToken).ConfigureAwait(false);
            var projectToLocations = memberReferenceLocations.ToLookup(loc => loc.location.Document.Project.Id);
            var reReferencedSolution = await RefactorReferencesAsync(projectToLocations, newDoc.Project.Solution, newType!, cancellationToken).ConfigureAwait(false);

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
        /// Finds what type kind new type should be. In case of C#, returning both class and module create a class
        /// For VB, we want a module unless there are class type params, in which case we want a class
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
            ILookup<ProjectId, (ReferenceLocation location, bool isExtensionMethod)> projectToLocations,
            Solution solution,
            INamedTypeSymbol newType,
            CancellationToken cancellationToken)
        {
            foreach (var (projectId, referencesForProject) in projectToLocations)
            {
                // organize by project first, so we can solve one project at a time
                var project = solution.GetRequiredProject(projectId);
                var documentToLocations = referencesForProject.ToLookup(reference => reference.location.Document.Id);
                foreach (var (docId, referencesForDoc) in documentToLocations)
                {
                    var doc = project.GetRequiredDocument(docId);
                    doc = await FixReferencesSingleDocumentAsync(referencesForDoc.ToImmutableArray(), doc, newType, cancellationToken).ConfigureAwait(false);
                    project = doc.Project;
                }

                solution = project.Solution;
            }

            return solution;
        }

        private static async Task<Document> FixReferencesSingleDocumentAsync(
            ImmutableArray<(ReferenceLocation location, bool isExtensionMethod)> referenceLocations,
            Document doc,
            INamedTypeSymbol newType,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = doc.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await doc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // keep extension method flag attached to node through dict
            var trackNodesDict = referenceLocations
                .Where(refLoc => !refLoc.location.IsCandidateLocation)
                .ToDictionary(refLoc => refLoc.location.Location.FindNode(
                    getInnermostNodeForTie: true,
                    cancellationToken));

            // track nodes as we may be processing multiple changes
            root = root.TrackNodes(trackNodesDict.Keys);

            var generator = doc.GetRequiredLanguageService<SyntaxGenerator>();

            doc = doc.WithSyntaxRoot(root);
            root = await doc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            foreach (var oldNode in trackNodesDict.Keys)
            {
                var (_, isExtensionMethod) = trackNodesDict[oldNode];
                var refNode = root.GetCurrentNode(oldNode);

                // now change the actual references to use the new type name, add a symbol annotation
                // for every reference we move so that if an import is necessary/possible,
                // we add it, and simplifiers so we don't over-qualify after import
                if (isExtensionMethod)
                {
                    // extension methods should be changed into their static class versions with
                    // full qualifications, then the qualification changed to the new type
                    if (syntaxFacts.IsNameOfAnyMemberAccessExpression(refNode) &&
                        syntaxFacts.IsAnyMemberAccessExpression(refNode!.Parent) &&
                        syntaxFacts.IsInvocationExpression(refNode.Parent!.Parent))
                    {
                        // get the entire expression, guaranteed not null based on earlier checks
                        var extensionMethodInvocation = refNode.GetRequiredParent().GetRequiredParent();
                        var expandedExtensionInvocation = await Simplifier.ExpandAsync(
                            extensionMethodInvocation,
                            doc,
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                        // should be an invocation of a simple member access expression with the expression as a type name
                        var memberAccessExpression = syntaxFacts.GetExpressionOfInvocationExpression(expandedExtensionInvocation);
                        var typeExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccessExpression)!;
                        expandedExtensionInvocation = expandedExtensionInvocation.ReplaceNode(typeExpression, generator.TypeExpression(newType)
                            .WithTriviaFrom(refNode)
                            .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, SymbolAnnotation.Create(newType)));

                        root = root.ReplaceNode(extensionMethodInvocation, expandedExtensionInvocation);
                    }
                }
                else if (syntaxFacts.IsNameOfSimpleMemberAccessExpression(refNode))
                {
                    // static member access should never be pointer or conditional member access,
                    // so syntax in this block should be of the form 'Class.Member'
                    var expression = syntaxFacts.GetExpressionOfMemberAccessExpression(refNode.Parent);
                    if (expression != null)
                    {
                        root = root.ReplaceNode(expression, generator.TypeExpression(newType)
                            .WithTriviaFrom(refNode)
                            .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, SymbolAnnotation.Create(newType)));
                    }
                }
                else if (syntaxFacts.IsIdentifierName(refNode))
                {
                    // We now are in an identifier name that isn't a member access expression
                    // This could either be because of a static using, module usage in VB, or because we are in the original source type
                    // either way, we want to change it to a member access expression for the type that is imported
                    root = root.ReplaceNode(
                        refNode,
                        generator.MemberAccessExpression(
                            generator.TypeExpression(newType)
                                .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, SymbolAnnotation.Create(newType)),
                            refNode));
                }
            }

            return doc.WithSyntaxRoot(root);
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
