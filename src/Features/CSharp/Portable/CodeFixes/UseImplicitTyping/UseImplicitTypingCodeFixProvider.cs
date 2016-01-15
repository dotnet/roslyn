// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.UseImplicitTyping
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseImplicitTyping), Shared]
    internal class UseImplicitTypingCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(IDEDiagnosticIds.UseImplicitTypingDiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return;
            }

            var node = token.GetAncestors<SyntaxNode>().First(n => n.Span.Contains(span));
            if (node == null ||
                !(node.IsKind(SyntaxKind.PredefinedType) ||
                  node.IsKind(SyntaxKind.ArrayType) ||
                  node.IsKind(SyntaxKind.IdentifierName) ||
                  node.IsKind(SyntaxKind.GenericName) || 
                  node.IsKind(SyntaxKind.AliasQualifiedName)))
            {
                return;
            }

            var codeAction = new MyCodeAction(
                                CSharpFeaturesResources.UseImplicitTyping,
                                c => ReplaceTypeWithVar(context, document, root, node));

            context.RegisterCodeFix(codeAction, context.Diagnostics.First());
        }

        private static Task<Document> ReplaceTypeWithVar(CodeFixContext context, Document document, SyntaxNode root, SyntaxNode node)
        {
            var implicitType = SyntaxFactory.IdentifierName("var")
                                            .WithLeadingTrivia(node.GetLeadingTrivia())
                                            .WithTrailingTrivia(node.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(node, implicitType);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}