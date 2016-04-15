// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.TypeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitType), Shared]
    internal class UseExplicitTypeCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(IDEDiagnosticIds.UseExplicitTypeDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span, getInnermostNodeForTie: true);

            var codeAction = new MyCodeAction(
                CSharpFeaturesResources.UseExplicitType,
                c => HandleDeclarationAsync(document, root, node, context.CancellationToken));

            context.RegisterCodeFix(codeAction, context.Diagnostics.First());
        }

        private async Task<Document> HandleDeclarationAsync(Document document, SyntaxNode root, SyntaxNode node, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationContext = node.Parent;

            TypeSyntax typeSyntax = null;
            if (declarationContext is VariableDeclarationSyntax)
            {
                typeSyntax = ((VariableDeclarationSyntax)declarationContext).Type;
            }
            else if (declarationContext is ForEachStatementSyntax)
            {
                typeSyntax = ((ForEachStatementSyntax)declarationContext).Type;
            }
            else
            {
                Contract.Fail($"unhandled kind {declarationContext.Kind().ToString()}");
            }

            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).ConvertedType;

            var typeName = typeSymbol.GenerateTypeSyntax()
                                     .WithLeadingTrivia(node.GetLeadingTrivia())
                                     .WithTrailingTrivia(node.GetTrailingTrivia());

            Debug.Assert(!typeName.ContainsDiagnostics, "Explicit type replacement likely introduced an error in code");

            var newRoot = root.ReplaceNode(node, typeName);
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}