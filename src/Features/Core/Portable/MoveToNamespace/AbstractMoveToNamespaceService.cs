

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract class AbstractMoveToNamespaceService : ILanguageService
    {
        internal abstract Task<ImmutableArray<AbstractMoveToNamespaceCodeAction>> GetCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
        internal abstract Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(Document document, int position, CancellationToken cancellationToken);
        public abstract Task<MoveToNamespaceResult> MoveToNamespaceAsync(MoveToNamespaceAnalysisResult analysisResult, string targetNamespace, CancellationToken cancellationToken);
        public abstract Task<MoveToNamespaceOptionsResult> GetOptionsAsync(Document document, string defaultNamespace, CancellationToken cancellationToken);
    }

    internal abstract class AbstractMoveToNamespaceService<TCompilationSyntax, TNamespaceDeclarationSyntax, TNamedTypeDeclarationSyntax>
        : AbstractMoveToNamespaceService
    {
        private IMoveToNamespaceOptionsService _moveToNamespaceOptionsService;

        protected abstract string GetNamespaceName(TNamespaceDeclarationSyntax syntax);
        protected abstract string GetNamespaceName(TNamedTypeDeclarationSyntax syntax);

        public AbstractMoveToNamespaceService(IMoveToNamespaceOptionsService moveToNamespaceOptionsService)
        {
            _moveToNamespaceOptionsService = moveToNamespaceOptionsService;
        }

        internal override async Task<ImmutableArray<AbstractMoveToNamespaceCodeAction>> GetCodeActionsAsync(
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

        internal override async Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
#if DEBUG // TODO: remove once the feature is done
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(position);
            var node = token.Parent;

            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken: cancellationToken);
            var symbol = symbolInfo.Symbol;
            var @namespace = symbol?.Name;

            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                node = node.FirstAncestorOrSelf<SyntaxNode>(a => a is TNamespaceDeclarationSyntax);
                @namespace = GetQualifiedName(namespaceSymbol);
            }

            if (node is TNamespaceDeclarationSyntax declarationSyntax)
            {
                if (ContainsNamespaceDeclaration(node))
                {
                    return new MoveToNamespaceAnalysisResult("Namespace container contains nested namespace declaration");
                }

                @namespace = @namespace ?? GetNamespaceName(declarationSyntax);
                return new MoveToNamespaceAnalysisResult(document, node, @namespace, MoveToNamespaceAnalysisResult.ContainerType.Namespace);
            }

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                node = node.FirstAncestorOrSelf<SyntaxNode>(a => a is TNamedTypeDeclarationSyntax);
                @namespace = GetQualifiedName(namedTypeSymbol.ContainingNamespace);
            }

            if (node is TNamedTypeDeclarationSyntax namedTypeDeclarationSyntax)
            {
                @namespace = @namespace ?? GetNamespaceName(namedTypeDeclarationSyntax);
                return new MoveToNamespaceAnalysisResult(document, node, @namespace, MoveToNamespaceAnalysisResult.ContainerType.NamedType);
            }

            return new MoveToNamespaceAnalysisResult("Not a valid position");
#else 

            return await Task.FromResult(new MoveToNamespaceAnalysisResult("Feature is not complete yet")).ConfigureAwait(false);
#endif
        }

        private bool ContainsNamespaceDeclaration(SyntaxNode node)
            => node.DescendantNodes(n => n is TCompilationSyntax || n is TNamespaceDeclarationSyntax)
                        .OfType<TNamespaceDeclarationSyntax>().Any();

        public override Task<MoveToNamespaceResult> MoveToNamespaceAsync(
            MoveToNamespaceAnalysisResult analysisResult,
            string targetNamespace,
            CancellationToken cancellationToken)
        {
            // TODO: Implementation will be in a separate PR
            return Task.FromResult(MoveToNamespaceResult.Failed);
        }

        private static SymbolDisplayFormat QualifiedNamespaceFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        protected static string GetQualifiedName(INamespaceSymbol namespaceSymbol)
            => namespaceSymbol.ToDisplayString(QualifiedNamespaceFormat);

        public override async Task<MoveToNamespaceOptionsResult> GetOptionsAsync(
            Document document,
            string defaultNamespace,
            CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var namespaces = compilation.GlobalNamespace.GetAllNamespaces(cancellationToken)
                .Where(n => n.NamespaceKind == NamespaceKind.Module && n.ContainingAssembly == compilation.Assembly)
                .Select(GetQualifiedName);

            return await _moveToNamespaceOptionsService.GetChangeNamespaceOptionsAsync(
                defaultNamespace,
                namespaces.ToImmutableArray(),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
