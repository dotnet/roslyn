// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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
        bool IsValidIdentifier(string identifier);
    }

    internal abstract class AbstractMoveToNamespaceService<TNamespaceDeclarationSyntax, TNamedTypeDeclarationSyntax>
        : IMoveToNamespaceService
    {
        private IMoveToNamespaceOptionsService _moveToNamespaceOptionsService;

        protected abstract string GetNamespaceName(TNamespaceDeclarationSyntax syntax);
        protected abstract string GetNamespaceName(TNamedTypeDeclarationSyntax syntax);
        public abstract bool IsValidIdentifier(string identifier);

        public AbstractMoveToNamespaceService(IMoveToNamespaceOptionsService moveToNamespaceOptionsService)
        {
            _moveToNamespaceOptionsService = moveToNamespaceOptionsService;
        }

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
#if DEBUG // TODO: remove once the feature is done
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
                if (ContainsNamespaceDeclaration(node))
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
                @namespace = @namespace ?? GetNamespaceName(namedTypeDeclarationSyntax);
                var namespaces = await GetNamespacesAsync(document, cancellationToken).ConfigureAwait(false);
                return new MoveToNamespaceAnalysisResult(document, node, @namespace, namespaces.ToImmutableArray(), MoveToNamespaceAnalysisResult.ContainerType.NamedType);
            }

            return MoveToNamespaceAnalysisResult.Invalid;
#else

            return await Task.FromResult(MoveToNamespaceAnalysisResult.Invalid).ConfigureAwait(false);
#endif
        }

        private bool ContainsNamespaceDeclaration(SyntaxNode node)
            => node.DescendantNodes().OfType<TNamespaceDeclarationSyntax>().Any();

        public Task<MoveToNamespaceResult> MoveToNamespaceAsync(
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
            return _moveToNamespaceOptionsService.GetChangeNamespaceOptions(
                defaultNamespace,
                namespaces,
                this);
        }
    }
}
