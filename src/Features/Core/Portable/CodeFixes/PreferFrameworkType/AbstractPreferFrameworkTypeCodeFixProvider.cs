// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.PreferFrameworkType
{
    internal abstract class AbstractPreferFrameworkTypeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
                IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;

        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var node = root.FindNode(span, findInsideTrivia:true, getInnermostNodeForTie: true);
            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetLanguageService<SyntaxGenerator>();
            var codeAction = new CodeAction.DocumentChangeAction(
                FeaturesResources.Use_framework_type,
                c => document.ReplaceNodeAsync(node, GetReplacementSyntax(node, generator, semanticModel, c), c),
                FeaturesResources.Use_framework_type);
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private SyntaxNode GetReplacementSyntax(SyntaxNode node, SyntaxGenerator generator, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var typeSymbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol as ITypeSymbol;
            return GenerateTypeSyntax(typeSymbol).WithTriviaFrom(node);
        }
    }
}
