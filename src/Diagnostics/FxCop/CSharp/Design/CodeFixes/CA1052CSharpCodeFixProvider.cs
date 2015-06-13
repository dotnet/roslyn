// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.AnalyzerPowerPack.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = CA1052DiagnosticAnalyzer.DiagnosticId), Shared]
    public class CA1052CSharpCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CA1052DiagnosticAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            cancellationToken.ThrowIfCancellationRequested();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var classDeclaration = root.FindToken(span.Start).Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration != null)
            {
                var title = string.Format(AnalyzerPowerPackRulesResources.StaticHolderTypeIsNotStatic, classDeclaration.Identifier.Text);
                var codeAction = new MyCodeAction(title, ct => AddStaticKeyword(document, root, classDeclaration));
                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private Task<Document> AddStaticKeyword(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration)
        {
            var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithAdditionalAnnotations(Formatter.Annotation);
            var newDeclaration = classDeclaration.AddModifiers(staticKeyword);
            var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
