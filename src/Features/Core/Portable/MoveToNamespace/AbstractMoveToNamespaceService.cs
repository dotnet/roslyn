// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract class AbstractMoveToNamespaceService : ILanguageService
    {
        internal abstract Task<ImmutableArray<MoveToNamespaceCodeAction>> GetCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
        internal abstract Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(Document document, int position, CancellationToken cancellationToken);
        public abstract Task<MoveToNamespaceResult> MoveToNamespaceAsync(MoveToNamespaceAnalysisResult analysisResult, string targetNamespace, CancellationToken cancellationToken);
        public abstract MoveToNamespaceOptionsResult GetOptions(Document document, string defaultNamespace, CancellationToken cancellationToken);
    }

    internal abstract class AbstractMoveToNamespaceService<TCompilationSyntax, TNamespaceDeclarationSyntax>
        : AbstractMoveToNamespaceService
    {
        private IMoveToNamespaceOptionsService _moveToNamespaceOptionsService;

        public AbstractMoveToNamespaceService(IMoveToNamespaceOptionsService moveToNamespaceOptionsService)
        {
            _moveToNamespaceOptionsService = moveToNamespaceOptionsService;
        }

        internal override async Task<ImmutableArray<MoveToNamespaceCodeAction>> GetCodeActionsAsync(
            Document document,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(document, span.Start, cancellationToken).ConfigureAwait(false);

            return typeAnalysisResult.CanPerform
                ? ImmutableArray.Create(new MoveToNamespaceCodeAction(this, typeAnalysisResult))
                : ImmutableArray<MoveToNamespaceCodeAction>.Empty;
        }

        internal override async Task<MoveToNamespaceAnalysisResult> AnalyzeTypeAtPositionAsync(
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

            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                node = node.FirstAncestorOrSelf<SyntaxNode>(a => a is TNamespaceDeclarationSyntax);
            }

            if (node is TNamespaceDeclarationSyntax declarationSyntax)
            {
                if (ContainsNamespaceDeclaration(node))
                {
                    return new MoveToNamespaceAnalysisResult("Container contains nested namespace declaration");
                }

                return new MoveToNamespaceAnalysisResult(document, node, declarationSyntax.GetTypeDisplayName());
            }

            return new MoveToNamespaceAnalysisResult("Not a valid position");
        }

        private bool ContainsNamespaceDeclaration(SyntaxNode node)
            => node.DescendantNodes(n => n is TCompilationSyntax || n is TNamespaceDeclarationSyntax)
                        .OfType<TNamespaceDeclarationSyntax>().Any();

        public override async Task<MoveToNamespaceResult> MoveToNamespaceAsync(
            MoveToNamespaceAnalysisResult analysisResult,
            string targetNamespace,
            CancellationToken cancellationToken)
        {
            if (!analysisResult.CanPerform)
            {
                return MoveToNamespaceResult.Failed;
            }

            var changeNamespaceService = analysisResult.Document.GetLanguageService<IChangeNamespaceService>();
            if (changeNamespaceService == null)
            {
                return MoveToNamespaceResult.Failed;
            }

            var changedSolution = await changeNamespaceService.ChangeNamespaceAsync(
                analysisResult.Document,
                analysisResult.Container,
                targetNamespace,
                cancellationToken).ConfigureAwait(false);

            return new MoveToNamespaceResult(changedSolution, analysisResult.Document.Id);
        }

        public override MoveToNamespaceOptionsResult GetOptions(
            Document document,
            string defaultNamespace,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();

            return _moveToNamespaceOptionsService.GetChangeNamespaceOptionsAsync(
                syntaxFactsService,
                notificationService,
                defaultNamespace,
                cancellationToken).WaitAndGetResult(cancellationToken);
        }
    }
}
