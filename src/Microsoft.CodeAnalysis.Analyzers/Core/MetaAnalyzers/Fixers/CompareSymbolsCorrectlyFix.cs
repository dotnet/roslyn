// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(CompareSymbolsCorrectlyFix))]
    [Shared]
    public class CompareSymbolsCorrectlyFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CompareSymbolsCorrectlyAnalyzer.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyCodeFix,
                        cancellationToken => ConvertToEqualsAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: nameof(CompareSymbolsCorrectlyFix)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> ConvertToEqualsAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var expression = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            if (!(semanticModel.GetOperation(expression, cancellationToken) is IBinaryOperation operation))
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var replacement = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.TypeExpression(semanticModel.Compilation.GetSpecialType(SpecialType.System_Object)),
                    nameof(object.Equals)),
                operation.LeftOperand.Syntax.WithoutLeadingTrivia(),
                operation.RightOperand.Syntax.WithoutTrailingTrivia());
            if (operation.OperatorKind == BinaryOperatorKind.NotEquals)
            {
                replacement = generator.LogicalNotExpression(replacement);
            }

            editor.ReplaceNode(expression, replacement.WithTriviaFrom(expression));
            return editor.GetChangedDocument();
        }
    }
}
