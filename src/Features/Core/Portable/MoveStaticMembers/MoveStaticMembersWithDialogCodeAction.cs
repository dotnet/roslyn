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
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveStaticMembers;

internal sealed class MoveStaticMembersWithDialogCodeAction(
    Document document,
    IMoveStaticMembersOptionsService service,
    INamedTypeSymbol selectedType,
    CleanCodeGenerationOptionsProvider fallbackOptions,
    ImmutableArray<ISymbol> selectedMembers) : CodeActionWithOptions
{
    private readonly Document _document = document;
    private readonly ImmutableArray<ISymbol> _selectedMembers = selectedMembers;
    private readonly INamedTypeSymbol _selectedType = selectedType;
    private readonly IMoveStaticMembersOptionsService _service = service;
    private readonly CleanCodeGenerationOptionsProvider _fallbackOptions = fallbackOptions;

    public override string Title => FeaturesResources.Move_static_members_to_another_type;

    public override object? GetOptions(CancellationToken cancellationToken)
    {
        return _service.GetMoveMembersToTypeOptions(_document, _selectedType, _selectedMembers);
    }

    protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
        object options, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        if (options is not MoveStaticMembersOptions moveOptions || moveOptions.IsCancelled)
            return [];

        // Find the original doc root
        var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // add annotations to the symbols that we selected so we can find them later to pull up
        // These symbols should all have (singular) definitions, but in the case that we can't find
        // any location, we just won't move that particular symbol
        var memberNodes = moveOptions.SelectedMembers
            .Select(symbol => symbol.Locations.FirstOrDefault())
            .WhereNotNull()
            .SelectAsArray(loc => loc.FindNode(cancellationToken));
        root = root.TrackNodes(memberNodes);
        var sourceDoc = _document.WithSyntaxRoot(root);

        if (!moveOptions.IsNewType)
        {
            // we already have our destination type, but we need to find the document it is in
            // When it is an existing type, "FileName" points to a full path rather than just the name
            // There should be no two docs that have the same file path
            var destinationDocId = sourceDoc.Project.Solution.GetDocumentIdsWithFilePath(moveOptions.FileName).Single();
            var fixedSolution = await RefactorAndMoveAsync(
                moveOptions.SelectedMembers,
                memberNodes,
                sourceDoc.Project.Solution,
                moveOptions.Destination!,
                // TODO: Find a way to merge/change generic type args for classes, or change PullMembersUp to handle instead
                typeArgIndices: [],
                sourceDoc.Id,
                destinationDocId,
                cancellationToken).ConfigureAwait(false);
            return [new ApplyChangesOperation(fixedSolution)];
        }

        // otherwise, we need to create a destination ourselves
        var typeParameters = ExtractTypeHelpers.GetRequiredTypeParametersForMembers(_selectedType, moveOptions.SelectedMembers);
        // which indices of the old type params should we keep for a new class reference, used for refactoring usages
        var typeArgIndices = Enumerable.Range(0, _selectedType.TypeParameters.Length)
            .Where(i => typeParameters.Contains(_selectedType.TypeParameters[i]))
            .ToImmutableArrayOrEmpty();

        // even though we can move members here, we will move them by calling PullMembersUp
        var newType = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
            [],
            Accessibility.NotApplicable,
            DeclarationModifiers.Static,
            GetNewTypeKind(_selectedType),
            moveOptions.TypeName!,
            typeParameters: typeParameters);

        var (newDoc, annotation) = await ExtractTypeHelpers.AddTypeToNewFileAsync(
            sourceDoc.Project.Solution,
            moveOptions.NamespaceDisplay,
            moveOptions.FileName,
            sourceDoc.Project.Id,
            sourceDoc.Folders,
            newType,
            sourceDoc,
            _fallbackOptions,
            cancellationToken).ConfigureAwait(false);

        // get back type declaration in the newly created file
        var destRoot = await newDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var destSemanticModel = await newDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        newType = (INamedTypeSymbol)destSemanticModel.GetRequiredDeclaredSymbol(destRoot.GetAnnotatedNodes(annotation).Single(), cancellationToken);

        // refactor references across the entire solution
        var memberReferenceLocations = await FindMemberReferencesAsync(newDoc.Project.Solution, newDoc.Project.Id, moveOptions.SelectedMembers, cancellationToken).ConfigureAwait(false);
        var projectToLocations = memberReferenceLocations.ToLookup(loc => loc.location.Document.Project.Id);
        var solutionWithFixedReferences = await RefactorReferencesAsync(projectToLocations, newDoc.Project.Solution, newType, typeArgIndices, cancellationToken).ConfigureAwait(false);

        sourceDoc = solutionWithFixedReferences.GetRequiredDocument(sourceDoc.Id);

        // get back nodes from our changes
        root = await sourceDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await sourceDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var members = memberNodes
            .Select(node => root.GetCurrentNode(node))
            .WhereNotNull()
            .SelectAsArray(node => (semanticModel.GetDeclaredSymbol(node, cancellationToken), false));

        var pullMembersUpOptions = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(newType, members);
        var movedSolution = await MembersPuller.PullMembersUpAsync(sourceDoc, pullMembersUpOptions, _fallbackOptions, cancellationToken).ConfigureAwait(false);

        return [new ApplyChangesOperation(movedSolution)];
    }

    /// <summary>
    /// Finds what type kind new type should be. Currently, we just select whatever type the source is.
    /// This means always a class for C#, and a module for VB iff we moved from a module
    /// This functionality can later be expanded or moved to language-specific implementations
    /// </summary>
    private static TypeKind GetNewTypeKind(INamedTypeSymbol oldType)
    {
        return oldType.TypeKind;
    }

    /// <summary>
    /// Finds references, refactors them, then moves the selected members to the destination.
    /// Used when the destination type/file already exists.
    /// </summary>
    /// <param name="selectedMembers">selected member symbols</param>
    /// <param name="oldMemberNodes">nodes corresponding to those symbols in the old solution, should have been annotated</param>
    /// <param name="oldSolution">solution without any members moved/refactored</param>
    /// <param name="newType">the type to move to, should be inserted into a document already</param>
    /// <param name="typeArgIndices">generic type arg indices to keep when refactoring generic class access to the new type. Empty if not relevant</param>
    /// <param name="sourceDocId">Id of the document where the mebers are being moved from</param>
    /// <returns>The solution with references refactored and members moved to the newType</returns>
    private async Task<Solution> RefactorAndMoveAsync(
        ImmutableArray<ISymbol> selectedMembers,
        ImmutableArray<SyntaxNode> oldMemberNodes,
        Solution oldSolution,
        INamedTypeSymbol newType,
        ImmutableArray<int> typeArgIndices,
        DocumentId sourceDocId,
        DocumentId newTypeDocId,
        CancellationToken cancellationToken)
    {
        // annotate our new type, in case our refactoring changes it
        var newTypeDoc = await oldSolution.GetRequiredDocumentAsync(newTypeDocId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var newTypeRoot = await newTypeDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newTypeNode = newType.DeclaringSyntaxReferences
            .SelectAsArray(sRef => sRef.GetSyntax(cancellationToken))
            .First(node => newTypeRoot.Contains(node));
        newTypeRoot = newTypeRoot.TrackNodes(newTypeNode);
        oldSolution = newTypeDoc.WithSyntaxRoot(newTypeRoot).Project.Solution;

        // refactor references across the entire solution
        var memberReferenceLocations = await FindMemberReferencesAsync(
            oldSolution, sourceDocId.ProjectId, selectedMembers, cancellationToken).ConfigureAwait(false);
        var projectToLocations = memberReferenceLocations.ToLookup(loc => loc.location.Document.Project.Id);
        var solutionWithFixedReferences = await RefactorReferencesAsync(projectToLocations, oldSolution, newType, typeArgIndices, cancellationToken).ConfigureAwait(false);

        var sourceDoc = solutionWithFixedReferences.GetRequiredDocument(sourceDocId);

        // get back tracked nodes from our changes
        var root = await sourceDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await sourceDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var members = oldMemberNodes
            .Select(node => root.GetCurrentNode(node))
            .WhereNotNull()
            .SelectAsArray(node => (semanticModel.GetDeclaredSymbol(node, cancellationToken), false));

        newTypeDoc = solutionWithFixedReferences.GetRequiredDocument(newTypeDoc.Id);
        newTypeRoot = await newTypeDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newTypeSemanticModel = await newTypeDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        newType = (INamedTypeSymbol)newTypeSemanticModel.GetRequiredDeclaredSymbol(newTypeRoot.GetCurrentNode(newTypeNode)!, cancellationToken);

        var pullMembersUpOptions = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(newType, members);
        return await MembersPuller.PullMembersUpAsync(sourceDoc, pullMembersUpOptions, _fallbackOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Solution> RefactorReferencesAsync(
        ILookup<ProjectId, (ReferenceLocation location, bool isExtensionMethod)> projectToLocations,
        Solution solution,
        INamedTypeSymbol newType,
        ImmutableArray<int> typeArgIndices,
        CancellationToken cancellationToken)
    {
        // keep our new solution separate, since each change can be performed separately
        var updatedSolution = solution;
        foreach (var (projectId, referencesForProject) in projectToLocations)
        {
            // organize by project first, so we can solve one project at a time
            var project = solution.GetRequiredProject(projectId);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var documentToLocations = referencesForProject.ToLookup(reference => reference.location.Document.Id);
            foreach (var (docId, referencesForDoc) in documentToLocations)
            {
                var doc = project.GetRequiredDocument(docId);
                var updatedRoot = await FixReferencesSingleDocumentAsync(
                    referencesForDoc.ToImmutableArray(),
                    doc,
                    newType,
                    typeArgIndices,
                    cancellationToken).ConfigureAwait(false);

                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(docId, updatedRoot);
            }

            // We keep the compilation until we are done with the project
            GC.KeepAlive(compilation);
        }

        return updatedSolution;
    }

    private static async Task<SyntaxNode> FixReferencesSingleDocumentAsync(
        ImmutableArray<(ReferenceLocation location, bool isExtensionMethod)> referenceLocations,
        Document doc,
        INamedTypeSymbol newType,
        ImmutableArray<int> typeArgIndices,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = doc.GetRequiredLanguageService<ISyntaxFactsService>();

        // keep extension method flag attached to node through dict
        var trackNodesDict = referenceLocations
            .ToImmutableDictionary(refLoc => refLoc.location.Location.FindNode(
                getInnermostNodeForTie: true,
                cancellationToken));

        var docEditor = await DocumentEditor.CreateAsync(doc, cancellationToken).ConfigureAwait(false);
        var generator = docEditor.Generator;

        foreach (var refNode in trackNodesDict.Keys)
        {
            var (_, isExtensionMethod) = trackNodesDict[refNode];

            // now change the actual references to use the new type name, add a symbol annotation
            // for every reference we move so that if an import is necessary/possible,
            // we add it, and simplifiers so we don't over-qualify after import
            if (isExtensionMethod)
            {
                // extension methods should be changed into their static class versions with
                // full qualifications, then the qualification changed to the new type
                if (syntaxFacts.IsNameOfAnyMemberAccessExpression(refNode) &&
                    syntaxFacts.IsMemberAccessExpression(refNode.Parent) &&
                    syntaxFacts.IsInvocationExpression(refNode.Parent.Parent))
                {
                    // get the entire expression, guaranteed not null based on earlier checks
                    var extensionMethodInvocation = refNode.Parent.Parent;
                    // expand using our (possibly outdated) document/syntaxes
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

                    docEditor.ReplaceNode(extensionMethodInvocation, expandedExtensionInvocation);
                }
            }
            else if (syntaxFacts.IsNameOfSimpleMemberAccessExpression(refNode))
            {
                // static member access should never be pointer or conditional member access,
                // so syntax in this block should be of the form 'Class.Member' or 'Class<TArg>.Member'
                var expression = syntaxFacts.GetExpressionOfMemberAccessExpression(refNode.GetRequiredParent());
                if (expression != null)
                {
                    SyntaxNode replacement;
                    if (syntaxFacts.IsGenericName(expression))
                    {
                        // if the access uses a generic name, then we copy only the type args we need
                        var typeArgs = syntaxFacts.GetTypeArgumentsOfGenericName(expression);
                        var newTypeArgs = typeArgIndices.SelectAsArray(i => typeArgs[i]);
                        replacement = generator.GenericName(newType.Name, newTypeArgs);
                    }
                    else
                    {
                        replacement = generator.TypeExpression(newType);
                    }

                    docEditor.ReplaceNode(expression, replacement
                        .WithTriviaFrom(refNode)
                        .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, SymbolAnnotation.Create(newType)));
                }
            }
            else if (syntaxFacts.IsIdentifierName(refNode))
            {
                // We now are in an identifier name that isn't a member access expression
                // This could either be because of a static using, module usage in VB, or because we are in the original source type
                // either way, we want to change it to a member access expression for the type that is imported
                docEditor.ReplaceNode(
                    refNode,
                    generator.MemberAccessExpression(
                        generator.TypeExpression(newType)
                            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, SymbolAnnotation.Create(newType)),
                        refNode));
            }
        }

        return docEditor.GetChangedRoot();
    }

    private static async Task<ImmutableArray<(ReferenceLocation location, bool isExtension)>> FindMemberReferencesAsync(
        Solution solution,
        ProjectId projectId,
        ImmutableArray<ISymbol> members,
        CancellationToken cancellationToken)
    {
        var project = solution.GetRequiredProject(projectId);
        var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<Task<IEnumerable<ReferencedSymbol>>>.GetInstance(out var tasks);
        foreach (var member in members)
        {
            tasks.Add(Task.Run(async () =>
            {
                var symbolKey = member.GetSymbolKey(cancellationToken);
                var resolvedMember = symbolKey.Resolve(compilation, ignoreAssemblyKey: false, cancellationToken).GetAnySymbol();
                return resolvedMember is null
                    ? []
                    : await SymbolFinder.FindReferencesAsync(resolvedMember, solution, cancellationToken).ConfigureAwait(false);
            }));
        }

        var symbolRefs = await Task.WhenAll(tasks).ConfigureAwait(false);
        return symbolRefs
            .Flatten()
            .SelectMany(refSymbol => refSymbol.Locations
                .Where(loc => !loc.IsCandidateLocation && !loc.IsImplicit)
                .Select(loc => (loc, refSymbol.Definition.IsExtensionMethod())))
            .ToImmutableArrayOrEmpty();
    }
}
