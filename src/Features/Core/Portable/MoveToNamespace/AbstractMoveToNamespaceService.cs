// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveToNamespace;

internal interface IMoveToNamespaceService : ILanguageService
{
    Task<ImmutableArray<MoveToNamespaceCodeAction>> GetCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
    Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(Document document, int position, CancellationToken cancellationToken);
    Task<MoveToNamespaceResult> MoveToNamespaceAsync(MoveToNamespaceAnalysisResult analysisResult, string targetNamespace, CancellationToken cancellationToken);
    MoveToNamespaceOptionsResult GetChangeNamespaceOptions(Document document, string defaultNamespace, ImmutableArray<string> namespaces);
    IMoveToNamespaceOptionsService OptionsService { get; }
}

internal abstract class AbstractMoveToNamespaceService<TCompilationUnitSyntax, TNamespaceDeclarationSyntax, TNamedTypeDeclarationSyntax>(
    IMoveToNamespaceOptionsService moveToNamespaceOptionsService)
    : IMoveToNamespaceService
    where TCompilationUnitSyntax : SyntaxNode
    where TNamespaceDeclarationSyntax : SyntaxNode
    where TNamedTypeDeclarationSyntax : SyntaxNode
{
    protected abstract string GetNamespaceName(SyntaxNode namespaceSyntax);
    protected abstract bool IsContainedInNamespaceDeclaration(TNamespaceDeclarationSyntax namespaceSyntax, int position);
    protected abstract TNamedTypeDeclarationSyntax? GetNamedTypeDeclarationSyntax(SyntaxNode node);

    public IMoveToNamespaceOptionsService OptionsService { get; } = moveToNamespaceOptionsService;

    public async Task<ImmutableArray<MoveToNamespaceCodeAction>> GetCodeActionsAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        // Code actions cannot be completed without the options needed
        // to fill in missing information.
        if (OptionsService != null)
        {
            var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(document, span.Start, cancellationToken).ConfigureAwait(false);

            if (typeAnalysisResult.CanPerform)
                return [MoveToNamespaceCodeAction.Generate(this, typeAnalysisResult)];
        }

        return [];
    }

    public async Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(position);
        var node = token.Parent;
        if (node is null)
        {
            return MoveToNamespaceAnalysisResult.Invalid;
        }

        var moveToNamespaceAnalysisResult = await TryAnalyzeNamespaceAsync(document, node, position, cancellationToken).ConfigureAwait(false);

        if (moveToNamespaceAnalysisResult != null)
        {
            return moveToNamespaceAnalysisResult;
        }

        moveToNamespaceAnalysisResult = await TryAnalyzeNamedTypeAsync(document, node, cancellationToken).ConfigureAwait(false);
        return moveToNamespaceAnalysisResult ?? MoveToNamespaceAnalysisResult.Invalid;
    }
    private async Task<MoveToNamespaceAnalysisResult?> TryAnalyzeNamespaceAsync(
        Document document, SyntaxNode node, int position, CancellationToken cancellationToken)
    {
        var declarationSyntax = node.FirstAncestorOrSelf<TNamespaceDeclarationSyntax>();
        if (declarationSyntax == null || !IsContainedInNamespaceDeclaration(declarationSyntax, position))
        {
            return null;
        }

        // The underlying ChangeNamespace service doesn't support nested namespace declaration.
        if (GetNamespaceInSpineCount(declarationSyntax) == 1)
        {
            var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();
            if (await changeNamespaceService.CanChangeNamespaceAsync(document, declarationSyntax, cancellationToken).ConfigureAwait(false))
            {
                var namespaceName = GetNamespaceName(declarationSyntax);
                var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);

                return new MoveToNamespaceAnalysisResult(document, declarationSyntax, namespaceName, [.. namespaces], MoveToNamespaceAnalysisResult.ContainerType.Namespace);
            }
        }

        return MoveToNamespaceAnalysisResult.Invalid;
    }

    private async Task<MoveToNamespaceAnalysisResult> TryAnalyzeNamedTypeAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var namespaceInSpineCount = GetNamespaceInSpineCount(node);

        // Nested namespaces are currently not supported by the underlying ChangeNamespace service
        if (namespaceInSpineCount > 1 || ContainsMultipleTypesInSpine(node))
        {
            return MoveToNamespaceAnalysisResult.Invalid;
        }

        SyntaxNode? container = null;

        // Moving one of the many members declared in global namespace is not currently supported,
        // but if it's the only member declared, then that's fine.
        if (namespaceInSpineCount == 0)
        {
            container = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            if (syntaxFacts.GetMembersOfCompilationUnit(container).Count > 1)
            {
                return MoveToNamespaceAnalysisResult.Invalid;
            }
        }

        var namedTypeDeclarationSyntax = GetNamedTypeDeclarationSyntax(node);
        if (namedTypeDeclarationSyntax is null)
        {
            return MoveToNamespaceAnalysisResult.Invalid;
        }

        // If we are inside a namespace declaration, then find it as the container.
        container ??= GetContainingNamespace(namedTypeDeclarationSyntax);
        if (container is null)
        {
            return MoveToNamespaceAnalysisResult.Invalid;
        }

        var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();
        if (await changeNamespaceService.CanChangeNamespaceAsync(document, container, cancellationToken).ConfigureAwait(false))
        {
            var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);
            return new MoveToNamespaceAnalysisResult(document, namedTypeDeclarationSyntax, GetNamespaceName(container), [.. namespaces], MoveToNamespaceAnalysisResult.ContainerType.NamedType);
        }

        return MoveToNamespaceAnalysisResult.Invalid;
    }

    private static TNamespaceDeclarationSyntax? GetContainingNamespace(TNamedTypeDeclarationSyntax namedTypeSyntax)
        => namedTypeSyntax.FirstAncestorOrSelf<TNamespaceDeclarationSyntax>();

    private static int GetNamespaceInSpineCount(SyntaxNode node)
        => node.AncestorsAndSelf().OfType<TNamespaceDeclarationSyntax>().Count() + node.DescendantNodes().OfType<TNamespaceDeclarationSyntax>().Count();

    private static bool ContainsMultipleTypesInSpine(SyntaxNode node)
        => node.AncestorsAndSelf().OfType<TNamedTypeDeclarationSyntax>().Count() > 1;

    public async Task<MoveToNamespaceResult> MoveToNamespaceAsync(
        MoveToNamespaceAnalysisResult analysisResult,
        string targetNamespace,
        CancellationToken cancellationToken)
    {
        if (!analysisResult.CanPerform)
            return MoveToNamespaceResult.Failed;

        return await (analysisResult.Container switch
        {
            MoveToNamespaceAnalysisResult.ContainerType.Namespace => MoveItemsInNamespaceAsync(analysisResult.Document, analysisResult.SyntaxNode, targetNamespace, cancellationToken),
            MoveToNamespaceAnalysisResult.ContainerType.NamedType => MoveTypeToNamespaceAsync(analysisResult.Document, analysisResult.SyntaxNode, targetNamespace, cancellationToken),
            _ => throw new InvalidOperationException(),
        }).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<ISymbol>> GetMemberSymbolsAsync(Document document, SyntaxNode container, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        switch (container)
        {
            case TNamespaceDeclarationSyntax namespaceNode:
                var namespaceMembers = syntaxFacts.GetMembersOfBaseNamespaceDeclaration(namespaceNode);
                return namespaceMembers
                    .Select(member => semanticModel.GetDeclaredSymbol(member, cancellationToken))
                    .WhereNotNull()
                    .ToImmutableArray();
            case TCompilationUnitSyntax compilationUnit:
                var compilationUnitMembers = syntaxFacts.GetMembersOfCompilationUnit(compilationUnit);
                // We are trying to move a selected type from global namespace to the target namespace.
                // This is supported if the selected type is the only member declared in the global namespace in this document.
                // (See `TryAnalyzeNamedTypeAsync`)
                Debug.Assert(compilationUnitMembers.Count == 1);
                return compilationUnitMembers
                    .Select(member => semanticModel.GetDeclaredSymbol(member, cancellationToken))
                    .WhereNotNull()
                    .ToImmutableArray();

            default:
                throw ExceptionUtilities.UnexpectedValue(container);
        }
    }

    private static async Task<MoveToNamespaceResult> MoveItemsInNamespaceAsync(
        Document document,
        SyntaxNode container,
        string targetNamespace,
        CancellationToken cancellationToken)
    {
        var memberSymbols = await GetMemberSymbolsAsync(document, container, cancellationToken).ConfigureAwait(false);
        var newNameOriginalSymbolMapping = memberSymbols
            .ToImmutableDictionary(symbol => GetNewSymbolName(symbol, targetNamespace), symbol => symbol);

        var changeNamespaceService = document.GetRequiredLanguageService<IChangeNamespaceService>();

        var originalSolution = document.Project.Solution;

        var changedSolution = await changeNamespaceService.ChangeNamespaceAsync(
            document,
            container,
            targetNamespace,
            cancellationToken).ConfigureAwait(false);

        return new MoveToNamespaceResult(originalSolution, changedSolution, document.Id, newNameOriginalSymbolMapping);
    }

    private static async Task<MoveToNamespaceResult> MoveTypeToNamespaceAsync(
        Document document,
        SyntaxNode container,
        string targetNamespace,
        CancellationToken cancellationToken)
    {
        var moveTypeService = document.GetRequiredLanguageService<IMoveTypeService>();

        // The move service expects a single position, not a full selection
        // See https://github.com/dotnet/roslyn/issues/34643
        var moveSpan = new TextSpan(container.FullSpan.Start, 0);

        var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(
            document,
            moveSpan,
            MoveTypeOperationKind.MoveTypeNamespaceScope,
            cancellationToken).ConfigureAwait(false);
        var modifiedDocument = modifiedSolution.GetRequiredDocument(document.Id);

        // Since MoveTypeService doesn't handle linked files, we need to merge the diff ourselves, 
        // otherwise, we will end up with multiple linked documents with different content.
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var mergedSolution = await PropagateChangeToLinkedDocumentsAsync(modifiedDocument, formattingOptions, cancellationToken).ConfigureAwait(false);
        var mergedDocument = mergedSolution.GetRequiredDocument(document.Id);

        var syntaxRoot = await mergedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxNode = syntaxRoot.GetAnnotatedNodes(AbstractMoveTypeService.NamespaceScopeMovedAnnotation).SingleOrDefault();

        // The type might be declared in global namespace
        syntaxNode ??= container.FirstAncestorOrSelf<TNamespaceDeclarationSyntax>() ?? syntaxRoot;

        return await MoveItemsInNamespaceAsync(
            mergedDocument,
            syntaxNode,
            targetNamespace,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Solution> PropagateChangeToLinkedDocumentsAsync(Document document, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
    {
        // Need to make sure elastic trivia is formatted properly before pushing the text to other documents.
        var formattedDocument = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation, formattingOptions, cancellationToken).ConfigureAwait(false);
        var formattedText = await formattedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var solution = formattedDocument.Project.Solution;

        var finalSolution = solution.WithDocumentTexts(
            formattedDocument.GetLinkedDocumentIds().SelectAsArray(id => (id, formattedText)));
        return finalSolution;
    }

    private static string GetNewSymbolName(ISymbol symbol, string targetNamespace)
    {
        Debug.Assert(symbol != null && !string.IsNullOrEmpty(targetNamespace));

        var offset = symbol.ContainingNamespace.IsGlobalNamespace
            ? 0
            : symbol.ContainingNamespace.ToDisplayString().Length + 1;

        return $"{targetNamespace}.{symbol.ToDisplayString()[offset..]}";
    }

    private static readonly SymbolDisplayFormat QualifiedNamespaceFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    protected static string GetQualifiedName(INamespaceSymbol namespaceSymbol)
        => namespaceSymbol.ToDisplayString(QualifiedNamespaceFormat);

    private static async Task<IEnumerable<string>> GetNamespacesAsync(Document document, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        return compilation.GlobalNamespace.GetAllNamespaces(cancellationToken)
            .Where(n => n.NamespaceKind == NamespaceKind.Module && n.ContainingAssembly == compilation.Assembly)
            .Select(GetQualifiedName);
    }

    public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(
        Document document,
        string defaultNamespace,
        ImmutableArray<string> namespaces)
    {
        var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

        return OptionsService.GetChangeNamespaceOptions(
            defaultNamespace,
            namespaces,
            syntaxFactsService);
    }
}
