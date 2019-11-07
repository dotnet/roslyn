// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    /// <summary>
    /// RS1016: Code fix providers should provide FixAll support.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(FixerWithFixAllFix)), Shared]
    public sealed class FixerWithFixAllFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.OverrideGetFixAllProviderRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxToken token = root.FindToken(context.Span.Start);
            if (!token.Span.IntersectsWith(context.Span))
            {
                return;
            }

            SyntaxGenerator generator = SyntaxGenerator.GetGenerator(context.Document);
            SyntaxNode classDecl = generator.GetDeclaration(token.Parent);
            if (classDecl == null)
            {
                return;
            }

            // Register code fix.
            var title = CodeAnalysisDiagnosticsResources.OverrideGetFixAllProviderCodeFixTitle;
            context.RegisterCodeFix(CodeAction.Create(title, c => AddMethodAsync(context.Document, classDecl, c), equivalenceKey: title), context.Diagnostics);
        }

        private static async Task<Document> AddMethodAsync(Document document, SyntaxNode classDecl, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeIsSealed = ((INamedTypeSymbol)model.GetDeclaredSymbol(classDecl)).IsSealed;

            INamedTypeSymbol? codeFixProviderSymbol = model.Compilation.GetOrCreateTypeByMetadataName(FixerWithFixAllAnalyzer.CodeFixProviderMetadataName);
            IMethodSymbol? getFixAllProviderMethod = codeFixProviderSymbol?.GetMembers(FixerWithFixAllAnalyzer.GetFixAllProviderMethodName).OfType<IMethodSymbol>().FirstOrDefault();
            if (getFixAllProviderMethod == null)
            {
                return document;
            }

            var returnStatement = generator.ReturnStatement(generator.MemberAccessExpression(
                generator.IdentifierName("WellKnownFixAllProviders"), "BatchFixer"));
            var statements = new SyntaxNode[] { returnStatement };

            var methodDeclaration = generator.MethodDeclaration(getFixAllProviderMethod, statements);
            var methodModifiers = typeIsSealed ? DeclarationModifiers.Override : DeclarationModifiers.Sealed + DeclarationModifiers.Override;
            methodDeclaration = generator.WithModifiers(methodDeclaration, methodModifiers);

            editor.AddMember(classDecl, methodDeclaration);

            return editor.GetChangedDocument();
        }
    }
}
