// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public abstract class ApplyDiagnosticAnalyzerAttributeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxToken token = root.FindToken(context.Span.Start);
            if (!token.Span.IntersectsWith(context.Span))
            {
                return;
            }

            SyntaxGenerator generator = SyntaxGenerator.GetGenerator(context.Document);
            SyntaxNode? classDecl = generator.GetDeclaration(token.Parent);
            if (classDecl == null)
            {
                return;
            }

            // Register fixes.

            // 1) Apply C# DiagnosticAnalyzerAttribute.
            string title = string.Format(CultureInfo.CurrentCulture, CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_1, LanguageNames.CSharp);
            AddFix(title, context, root, classDecl, generator, LanguageNames.CSharp);

            // 2) Apply VB DiagnosticAnalyzerAttribute.
            title = string.Format(CultureInfo.CurrentCulture, CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_1, LanguageNames.VisualBasic);
            AddFix(title, context, root, classDecl, generator, LanguageNames.VisualBasic);

            // 3) Apply both C# and VB DiagnosticAnalyzerAttributes.
            title = string.Format(CultureInfo.CurrentCulture, CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_2, LanguageNames.CSharp, LanguageNames.VisualBasic);
            AddFix(title, context, root, classDecl, generator, LanguageNames.CSharp, LanguageNames.VisualBasic);
        }

        protected abstract SyntaxNode ParseExpression(string expression);

        private void AddFix(string codeFixTitle, CodeFixContext context, SyntaxNode root, SyntaxNode classDecl, SyntaxGenerator generator, params string[] languages)
        {
            var fix = CodeAction.Create(
                codeFixTitle,
                c => GetFixAsync(context.Document, root, classDecl, generator, languages),
                equivalenceKey: codeFixTitle);
            context.RegisterCodeFix(fix, context.Diagnostics);
        }

        private Task<Document> GetFixAsync(Document document, SyntaxNode root, SyntaxNode classDecl, SyntaxGenerator generator, params string[] languages)
        {
            string languageNamesFullName = typeof(LanguageNames).FullName;
            var arguments = new SyntaxNode[languages.Length];

            for (int i = 0; i < languages.Length; i++)
            {
                string language = languages[i] == LanguageNames.CSharp ? nameof(LanguageNames.CSharp) : nameof(LanguageNames.VisualBasic);
                string expressionToParse = languageNamesFullName + "." + language;
                SyntaxNode parsedExpression = ParseExpression(expressionToParse);
                arguments[i] = generator.AttributeArgument(parsedExpression);
            }

            SyntaxNode attribute = generator.Attribute(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute, arguments);
            SyntaxNode newClassDecl = generator.AddAttributes(classDecl, attribute);
            SyntaxNode newRoot = root.ReplaceNode(classDecl, newClassDecl);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
