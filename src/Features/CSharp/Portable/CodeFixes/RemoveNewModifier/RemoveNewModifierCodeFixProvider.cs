// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveNew), Shared]
    internal class RemoveNewModifierCodeFixProvider : CodeFixProvider
    {
        internal const string CS0109 = nameof(CS0109); // The member 'SomeClass.SomeMember' does not hide an accessible member. The new keyword is not required.

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public RemoveNewModifierCodeFixProvider()
        {
        }

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0109);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);

            var memberDeclarationSyntax = token.GetAncestor<MemberDeclarationSyntax>();
            if (memberDeclarationSyntax == null)
                return;

            var newModifier = GetNewModifier(memberDeclarationSyntax, CSharpSyntaxFacts.Instance);
            if (newModifier == default)
                return;

            context.RegisterCodeFix(
                new MyCodeAction(ct => FixAsync(context.Document, memberDeclarationSyntax, ct)),
                context.Diagnostics);
        }

        private async Task<Document> FixAsync(Document document, MemberDeclarationSyntax node, CancellationToken cancellationToken)
        {
            var syntaxFacts = CSharpSyntaxFacts.Instance;

            var newModifier = GetNewModifier(node, syntaxFacts);

            var newNode = node;

            if (newModifier.HasTrailingTrivia || newModifier.HasLeadingTrivia)
            {
                var newModifierTrivia = newModifier.GetAllTrivia().ToSyntaxTriviaList();
                var previousToken = newModifier.GetPreviousToken();
                var nextToken = newModifier.GetNextToken();

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var isFirstTokenOnLine = newModifier.IsFirstTokenOnLine(sourceText);

                var newTrivia = new SyntaxTriviaList();
                if (!isFirstTokenOnLine)
                    newTrivia = newTrivia.AddRange(previousToken.TrailingTrivia);
                newTrivia = newTrivia
                    .AddRange(newModifierTrivia)
                    .AddRange(nextToken.LeadingTrivia);
                newTrivia = CollapseSequentialWhitespaceTrivia(newTrivia);

                if (isFirstTokenOnLine)
                {
                    var nextTokenWithMovedTrivia = nextToken.WithLeadingTrivia(newTrivia);
                    newNode = newNode.ReplaceToken(nextToken, nextTokenWithMovedTrivia);
                }
                else
                {
                    var previousTokenWithMovedTrivia = previousToken.WithTrailingTrivia(newTrivia);
                    newNode = newNode.ReplaceToken(previousToken, previousTokenWithMovedTrivia);
                }
            }

            newNode = newNode.ReplaceToken(GetNewModifier(newNode, syntaxFacts), SyntaxFactory.Token(SyntaxKind.None));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxToken GetNewModifier(SyntaxNode fromNode, CSharpSyntaxFacts syntaxFacts) =>
            syntaxFacts.GetModifierTokens(fromNode).FirstOrDefault(m => m.IsKind(SyntaxKind.NewKeyword));

        private static SyntaxTriviaList CollapseSequentialWhitespaceTrivia(SyntaxTriviaList triviaList)
        {
            var result = new SyntaxTriviaList();
            var previous = default(SyntaxTrivia);
            foreach (var current in triviaList)
            {
                if (!(previous.IsWhitespace() && current.IsWhitespace()))
                    result = result.Add(current);
                previous = current;
            }

            return result;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Remove_new_modifier,
                    createChangedDocument,
                    CSharpFeaturesResources.Remove_new_modifier)
            {
            }
        }
    }
}
