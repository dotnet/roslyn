// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal interface IMoveToNamespaceService : ILanguageService
    {
        Task<ImmutableArray<AbstractMoveToNamespaceCodeAction>> GetCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
        Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(Document document, int position, CancellationToken cancellationToken);
        Task<MoveToNamespaceResult> MoveToNamespaceAsync(MoveToNamespaceAnalysisResult analysisResult, string targetNamespace, CancellationToken cancellationToken);
        MoveToNamespaceOptionsResult GetChangeNamespaceOptions(Document document, string defaultNamespace, ImmutableArray<string> namespaces);
    }

    internal abstract class AbstractMoveToNamespaceService<TNamespaceDeclarationSyntax, TNamedTypeDeclarationSyntax>
        : IMoveToNamespaceService
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TNamedTypeDeclarationSyntax : SyntaxNode

    {
        protected abstract string GetNamespaceName(TNamespaceDeclarationSyntax syntax);
        protected abstract string GetNamespaceName(TNamedTypeDeclarationSyntax syntax);

        public async Task<ImmutableArray<AbstractMoveToNamespaceCodeAction>> GetCodeActionsAsync(
            Document document,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(document, span.Start, cancellationToken).ConfigureAwait(false);

            if (typeAnalysisResult.CanPerform)
            {
                return ImmutableArray.Create(AbstractMoveToNamespaceCodeAction.Generate(this, typeAnalysisResult));
            }

            return ImmutableArray<AbstractMoveToNamespaceCodeAction>.Empty;
        }

        public async Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(position);
            var node = token.Parent;

            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken: cancellationToken);
            var symbol = symbolInfo.Symbol;
            string @namespace = null;

            if (symbol is INamespaceSymbol namespaceSymbol && !(node is TNamespaceDeclarationSyntax))
            {
                node = node.FirstAncestorOrSelf<SyntaxNode>(a => a is TNamespaceDeclarationSyntax);
            }

            if (node is TNamespaceDeclarationSyntax declarationSyntax)
            {
                if (ContainsNamespaceDeclaration(node) || ContainsMultipleNamespaceInSpine(node))
                {
                    return MoveToNamespaceAnalysisResult.Invalid;
                }

                @namespace = GetNamespaceName(declarationSyntax);
                var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);
                return new MoveToNamespaceAnalysisResult(document, node, @namespace, namespaces.ToImmutableArray(), MoveToNamespaceAnalysisResult.ContainerType.Namespace);
            }

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                node = node.FirstAncestorOrSelf<SyntaxNode>(a => a is TNamedTypeDeclarationSyntax);
                @namespace = GetQualifiedName(namedTypeSymbol.ContainingNamespace);
            }

            if (node is TNamedTypeDeclarationSyntax namedTypeDeclarationSyntax)
            {
                if (ContainsMultipleNamespaceInSpine(node))
                {
                    return MoveToNamespaceAnalysisResult.Invalid;
                }

                @namespace = @namespace ?? GetNamespaceName(namedTypeDeclarationSyntax);
                var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);
                return new MoveToNamespaceAnalysisResult(document, node, @namespace, namespaces.ToImmutableArray(), MoveToNamespaceAnalysisResult.ContainerType.NamedType);
            }

            return MoveToNamespaceAnalysisResult.Invalid;
        }

        private bool ContainsNamespaceDeclaration(SyntaxNode node)
            => node.DescendantNodes().OfType<TNamespaceDeclarationSyntax>().Any();

        private static bool ContainsMultipleNamespaceInSpine(SyntaxNode node)
            => node.AncestorsAndSelf().OfType<TNamespaceDeclarationSyntax>().Count() > 1;

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
                    return MoveTypeToNamespace(analysisResult.Document, analysisResult.SyntaxNode, targetNamespace, cancellationToken);
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
            var changeNamespaceService = document.GetLanguageService<IChangeNamespaceService>();
            if (changeNamespaceService == null)
            {
                return MoveToNamespaceResult.Failed;
            }

            var changedSolution = await changeNamespaceService.ChangeNamespaceAsync(
                document,
                container,
                targetNamespace,
                cancellationToken).ConfigureAwait(false);

            return new MoveToNamespaceResult(changedSolution, document.Id);
        }

        private static async Task<MoveToNamespaceResult> MoveTypeToNamespace(
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

        private static SymbolDisplayFormat QualifiedNamespaceFormat = new SymbolDisplayFormat(
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
            var moveToNamespaceOptionsService = document.Project.Solution.Workspace.Services.GetService<IMoveToNamespaceOptionsService>();

            if (moveToNamespaceOptionsService == null)
            {
                return MoveToNamespaceOptionsResult.Cancelled;
            }

            return moveToNamespaceOptionsService.GetChangeNamespaceOptions(
                defaultNamespace,
                namespaces,
                syntaxFactsService);
        }
    }
}
