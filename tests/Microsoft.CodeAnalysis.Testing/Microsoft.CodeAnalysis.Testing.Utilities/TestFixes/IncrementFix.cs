// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing.TestFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    [PartNotDiscoverable]
    public class IncrementFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(LiteralUnderFiveAnalyzer.Descriptor.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "LiteralUnderFive",
                        cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        nameof(IncrementFix)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var tree = (await document.GetSyntaxTreeAsync(cancellationToken))!;
            var root = await tree.GetRootAsync(cancellationToken);
            var token = root.FindToken(sourceSpan.Start);
            var replacement = int.Parse(token.ValueText) + 1;
            var generator = SyntaxGenerator.GetGenerator(document);

            var originalLeadingTrivia = token.LeadingTrivia;
            SyntaxTriviaList newLeadingTrivia;
            Assert.Equal(0, originalLeadingTrivia.Count);
            if (document.Project.Language == LanguageNames.CSharp)
            {
                newLeadingTrivia = CSharp.SyntaxFactory.TriviaList(CSharp.SyntaxFactory.Space);
            }
            else
            {
                newLeadingTrivia = VisualBasic.SyntaxFactory.TriviaList(VisualBasic.SyntaxFactory.Space);
            }

            var newExpression = generator.LiteralExpression(replacement).WithLeadingTrivia(newLeadingTrivia).WithTrailingTrivia(token.TrailingTrivia);
            return document.WithSyntaxRoot(root.ReplaceNode(token.Parent!, newExpression));
        }
    }
}
