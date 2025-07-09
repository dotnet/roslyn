// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    /// <summary>
    /// RS1016: Code fix providers should provide FixAll support.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(FixerWithFixAllFix)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class FixerWithFixAllFix() : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = [DiagnosticIds.OverrideGetFixAllProviderRuleId];

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);
            if (!token.Span.IntersectsWith(context.Span))
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(context.Document);
            var classDecl = generator.GetDeclaration(token.Parent);
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

            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeIsSealed = ((INamedTypeSymbol)model.GetDeclaredSymbol(classDecl, cancellationToken)!).IsSealed;

            var codeFixProviderSymbol = model.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCodeFixesCodeFixProvider);
            var getFixAllProviderMethod = codeFixProviderSymbol?.GetMembers(FixerWithFixAllAnalyzer.GetFixAllProviderMethodName).OfType<IMethodSymbol>().FirstOrDefault();
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
