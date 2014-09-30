// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    [ExportCodeFixProvider(StaticTypeRulesDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.CSharp)]
    public class CA1052CSharpCodeFixProvider : CodeFixProvider
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(StaticTypeRulesDiagnosticAnalyzer.CA1052RuleId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var classDeclaration = root.FindToken(span.Start).GetAncestor<ClassDeclarationSyntax>();
            if (classDeclaration != null)
            {
                var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithAdditionalAnnotations(Formatter.Annotation);
                var newDeclaration = classDeclaration.AddModifiers(staticKeyword);
                var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
                return SpecializedCollections.SingletonEnumerable(
                    new MyCodeAction(string.Format(FxCopRulesResources.StaticHolderTypeIsNotStatic, classDeclaration.Identifier.Text), document.WithSyntaxRoot(newRoot)));
            }

            return null;
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
