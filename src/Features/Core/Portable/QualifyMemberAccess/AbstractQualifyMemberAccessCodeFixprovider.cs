// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QualifyMemberAccess
{
    internal abstract class AbstractQualifyMemberAccessCodeFixprovider<TSyntaxNode> : CodeFixProvider where TSyntaxNode : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.AddQualificationDiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return;
            }

            var node = token.GetAncestor<TSyntaxNode>();
            if (node == null)
            {
                return;
            }

            var generator = document.GetLanguageService<SyntaxGenerator>();
            var title = this.GetTitle();
            var codeAction = new MyCodeAction(
                title, c => document.ReplaceNodeAsync(node, GetReplacementSyntax(node, generator), c));
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        protected abstract string GetTitle();

        public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;

        private static SyntaxNode GetReplacementSyntax(SyntaxNode node, SyntaxGenerator generator)
        {
            var qualifiedAccess =
                generator.MemberAccessExpression(
                    generator.ThisExpression(),
                    node.WithLeadingTrivia())
                .WithLeadingTrivia(node.GetLeadingTrivia());
            return qualifiedAccess;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
