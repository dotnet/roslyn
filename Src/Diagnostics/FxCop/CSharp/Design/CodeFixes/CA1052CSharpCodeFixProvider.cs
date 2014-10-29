// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    [ExportCodeFixProvider(StaticTypeRulesDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.CSharp), Shared]
    public class CA1052CSharpCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(StaticTypeRulesDiagnosticAnalyzer.CA1052RuleId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            cancellationToken.ThrowIfCancellationRequested();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var classDeclaration = root.FindToken(span.Start).GetAncestor<ClassDeclarationSyntax>();
            if (classDeclaration != null)
            {
                var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithAdditionalAnnotations(Formatter.Annotation);
                var newDeclaration = classDeclaration.AddModifiers(staticKeyword);
                var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
                context.RegisterFix(
                    new MyCodeAction(string.Format(FxCopRulesResources.StaticHolderTypeIsNotStatic, classDeclaration.Identifier.Text), document.WithSyntaxRoot(newRoot)),
                    context.Diagnostics);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Document newDocument) :
                base(title, c => Task.FromResult(newDocument))
            {
            }
        }
    }
}