// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal interface IMoveToNamespaceService : ILanguageService
    {
        Task<ImmutableArray<AbstractMoveToNamespaceCodeAction>> GetCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
        Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(Document document, int position, CancellationToken cancellationToken);
        Task<MoveToNamespaceResult> MoveToNamespaceAsync(MoveToNamespaceAnalysisResult analysisResult, string targetNamespace, CancellationToken cancellationToken);
        MoveToNamespaceOptionsResult GetChangeNamespaceOptions(Document document, string defaultNamespace, ImmutableArray<string> namespaces);
        IMoveToNamespaceOptionsService OptionsService { get; }
    }

    internal abstract class AbstractMoveToNamespaceService<TNamespaceDeclarationSyntax, TNamedTypeDeclarationSyntax>
        : IMoveToNamespaceService
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TNamedTypeDeclarationSyntax : SyntaxNode

    {
        protected abstract string GetNamespaceName(TNamespaceDeclarationSyntax namespaceSyntax);
        protected abstract string GetNamespaceName(TNamedTypeDeclarationSyntax namedTypeSyntax);
        protected abstract bool IsContainedInNamespaceDeclaration(TNamespaceDeclarationSyntax namespaceSyntax, int position);

        public IMoveToNamespaceOptionsService OptionsService { get; }

        protected AbstractMoveToNamespaceService(IMoveToNamespaceOptionsService moveToNamespaceOptionsService)
        {
            OptionsService = moveToNamespaceOptionsService;
        }

        public async Task<ImmutableArray<AbstractMoveToNamespaceCodeAction>> GetCodeActionsAsync(
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
                {
                    return ImmutableArray.Create(AbstractMoveToNamespaceCodeAction.Generate(this, typeAnalysisResult));
                }
            }

            return ImmutableArray<AbstractMoveToNamespaceCodeAction>.Empty;
        }

        public async Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

            var token = root.FindToken(position);
            var node = token.Parent;

            var moveToNamespaceAnalysisResult = await TryAnalyzeNamespaceAsync(document, node, position, cancellationToken).ConfigureAwait(false);

            if (moveToNamespaceAnalysisResult != null)
            {
                return moveToNamespaceAnalysisResult;
            }

            moveToNamespaceAnalysisResult = await TryAnalyzeNamedTypeAsync(document, node, cancellationToken).ConfigureAwait(false);
            return moveToNamespaceAnalysisResult ?? MoveToNamespaceAnalysisResult.Invalid;
        }

        private async Task<MoveToNamespaceAnalysisResult> TryAnalyzeNamespaceAsync(
            Document document, SyntaxNode node, int position, CancellationToken cancellationToken)
        {
            var declarationSyntax = node.FirstAncestorOrSelf<TNamespaceDeclarationSyntax>();
            if (declarationSyntax == default || !IsContainedInNamespaceDeclaration(declarationSyntax, position))
            {
                return null;
            }

            if (ContainsNamespaceDeclaration(declarationSyntax) || ContainsMultipleNamespaceInSpine(declarationSyntax))
            {
                return MoveToNamespaceAnalysisResult.Invalid;
            }
            else
            {
                var namespaceName = GetNamespaceName(declarationSyntax);
                var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);
                return new MoveToNamespaceAnalysisResult(document, declarationSyntax, namespaceName, namespaces.ToImmutableArray(), MoveToNamespaceAnalysisResult.ContainerType.Namespace);
            }
        }

        private async Task<MoveToNamespaceAnalysisResult> TryAnalyzeNamedTypeAsync(
            Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            // Multiple nested namespaces are currently not supported
            if (ContainsMultipleNamespaceInSpine(node) || ContainsMultipleTypesInSpine(node))
            {
                return MoveToNamespaceAnalysisResult.Invalid;
            }

            if (node is TNamedTypeDeclarationSyntax namedTypeDeclarationSyntax)
            {
                var namespaceName = GetNamespaceName(namedTypeDeclarationSyntax);
                var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);
                return new MoveToNamespaceAnalysisResult(document, namedTypeDeclarationSyntax, namespaceName, namespaces.ToImmutableArray(), MoveToNamespaceAnalysisResult.ContainerType.NamedType);
            }

            return null;
        }

        private bool ContainsNamespaceDeclaration(SyntaxNode node)
            => node.DescendantNodes().OfType<TNamespaceDeclarationSyntax>().Any();

        private static bool ContainsMultipleNamespaceInSpine(SyntaxNode node)
            => node.AncestorsAndSelf().OfType<TNamespaceDeclarationSyntax>().Count() > 1;

        private static bool ContainsMultipleTypesInSpine(SyntaxNode node)
            => node.AncestorsAndSelf().OfType<TNamedTypeDeclarationSyntax>().Count() > 1;

        public Task<MoveToNamespaceResult> MoveToNamespaceAsync(
            MoveToNamespaceAnalysisResult analysisResult,
            string targetNamespace,
            CancellationToken cancellationToken)
        {
            if (!analysisResult.CanPerform)
            {
                return Task.FromResult(MoveToNamespaceResult.Failed);
            }

            switch (analysisResult.Container)
            {
                case MoveToNamespaceAnalysisResult.ContainerType.Namespace:
                    return MoveItemsInNamespaceAsync(analysisResult.Document, analysisResult.SyntaxNode, targetNamespace, cancellationToken);
                case MoveToNamespaceAnalysisResult.ContainerType.NamedType:
                    return MoveTypeToNamespaceAsync(analysisResult.Document, analysisResult.SyntaxNode, targetNamespace, cancellationToken);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static async Task<MoveToNamespaceResult> MoveItemsInNamespaceAsync(
            Document document,
            SyntaxNode container,
            string targetNamespace,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var containerSymbol = (INamespaceSymbol)semanticModel.GetDeclaredSymbol(container);
            var members = containerSymbol.GetMembers();
            var newNameOriginalSymbolMapping = members
                .ToImmutableDictionary(symbol => GetNewSymbolName(symbol, targetNamespace), symbol => (ISymbol)symbol);

            var changeNamespaceService = document.GetLanguageService<IChangeNamespaceService>();
            if (changeNamespaceService == null)
            {
                return MoveToNamespaceResult.Failed;
            }

            var originalSolution = document.Project.Solution;
            var typeDeclarationsInContainer = container.DescendantNodes(syntaxNode => syntaxNode is TNamedTypeDeclarationSyntax).ToImmutableArray();

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
            var moveTypeService = document.GetLanguageService<IMoveTypeService>();
            if (moveTypeService == null)
            {
                return MoveToNamespaceResult.Failed;
            }

            // The move service expects a single position, not a full selection
            // See https://github.com/dotnet/roslyn/issues/34643
            var moveSpan = new TextSpan(container.FullSpan.Start, 0);

            var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(
                document,
                moveSpan,
                MoveTypeOperationKind.MoveTypeNamespaceScope,
                cancellationToken).ConfigureAwait(false);

            var modifiedDocument = modifiedSolution.GetDocument(document.Id);
            var syntaxRoot = await modifiedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxNode = syntaxRoot.GetAnnotatedNodes(AbstractMoveTypeService.NamespaceScopeMovedAnnotation).SingleOrDefault();
            if (syntaxNode == null)
            {
                syntaxNode = container.FirstAncestorOrSelf<TNamespaceDeclarationSyntax>();
            }

            return await MoveItemsInNamespaceAsync(
                modifiedDocument,
                syntaxNode,
                targetNamespace,
                cancellationToken).ConfigureAwait(false);
        }

        private static string GetNewSymbolName(ISymbol symbol, string targetNamespace)
        {
            Debug.Assert(symbol != null);
            return targetNamespace + symbol.ToDisplayString().Substring(symbol.ContainingNamespace.ToDisplayString().Length);
        }

        private static readonly SymbolDisplayFormat QualifiedNamespaceFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        protected static string GetQualifiedName(INamespaceSymbol namespaceSymbol)
            => namespaceSymbol.ToDisplayString(QualifiedNamespaceFormat);

        private static async Task<IEnumerable<string>> GetNamespacesAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            return compilation.GlobalNamespace.GetAllNamespaces(cancellationToken)
                .Where(n => n.NamespaceKind == NamespaceKind.Module && n.ContainingAssembly == compilation.Assembly)
                .Select(GetQualifiedName);
        }

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(
            Document document,
            string defaultNamespace,
            ImmutableArray<string> namespaces)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            return OptionsService.GetChangeNamespaceOptions(
                defaultNamespace,
                namespaces,
                syntaxFactsService);
        }
    }
}
