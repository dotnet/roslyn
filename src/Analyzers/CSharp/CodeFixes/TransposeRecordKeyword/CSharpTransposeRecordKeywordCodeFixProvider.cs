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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.TransposeRecordKeyword), Shared]
    internal class CSharpTransposeRecordKeywordCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS9012 = nameof(CS9012); // Unexpected keyword 'record'. Did you mean 'record struct' or 'record class'?

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpTransposeRecordKeywordCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS9012);

        private static bool TryGetRecordDeclaration(
            Diagnostic diagnostic, CancellationToken cancellationToken, [NotNullWhen(true)] out RecordDeclarationSyntax? recordDeclaration)
        {
            recordDeclaration = diagnostic.Location.FindNode(cancellationToken) as RecordDeclarationSyntax;
            return recordDeclaration != null;
        }

        private static bool TryGetTokens(
            RecordDeclarationSyntax recordDeclaration,
            out SyntaxToken classOrStructKeyword,
            out SyntaxToken recordKeyword)
        {
            recordKeyword = recordDeclaration.Keyword;
            if (!recordKeyword.IsMissing)
            {
                var leadingTrivia = recordKeyword.LeadingTrivia;
                var skippedTriviaIndex = leadingTrivia.IndexOf(SyntaxKind.SkippedTokensTrivia);
                if (skippedTriviaIndex >= 0)
                {
                    var skippedTrivia = leadingTrivia[skippedTriviaIndex];
                    var structure = (SkippedTokensTriviaSyntax)skippedTrivia.GetStructure()!;
                    var tokens = structure.Tokens;
                    if (tokens.Count == 1)
                    {
                        classOrStructKeyword = tokens.Single();
                        if (classOrStructKeyword.Kind() is SyntaxKind.ClassKeyword or SyntaxKind.StructKeyword)
                        {
                            // Because the class/struct keyword is skipped trivia on the record keyword, it will
                            // not have trivia of it's own.  So we need to move the other trivia appropriate trivia
                            // on the record keyword to it.
                            var remainingLeadingTrivia = SyntaxFactory.TriviaList(leadingTrivia.Skip(skippedTriviaIndex + 1));
                            var trailingTriviaTakeUntil = remainingLeadingTrivia.IndexOf(SyntaxKind.EndOfLineTrivia) is >= 0 and var eolIndex
                                ? eolIndex + 1
                                : remainingLeadingTrivia.Count;

                            classOrStructKeyword = classOrStructKeyword
                                .WithLeadingTrivia(SyntaxFactory.TriviaList(remainingLeadingTrivia.Skip(trailingTriviaTakeUntil)))
                                .WithTrailingTrivia(recordKeyword.TrailingTrivia);
                            recordKeyword = recordKeyword
                                .WithLeadingTrivia(leadingTrivia.Take(skippedTriviaIndex))
                                .WithTrailingTrivia(SyntaxFactory.TriviaList(remainingLeadingTrivia.Take(trailingTriviaTakeUntil)));

                            return true;
                        }
                    }
                }
            }

            classOrStructKeyword = default;
            return false;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;

            var diagnostic = context.Diagnostics.First();
            if (TryGetRecordDeclaration(diagnostic, cancellationToken, out var recordDeclaration) &&
                TryGetTokens(recordDeclaration, out _, out _))
            {
                RegisterCodeFix(context, CSharpCodeFixesResources.Fix_record_declaration, nameof(CSharpCodeFixesResources.Fix_record_declaration));
            }

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (TryGetRecordDeclaration(diagnostic, cancellationToken, out var recordDeclaration))
                {
                    editor.ReplaceNode(
                        recordDeclaration,
                        (current, _) =>
                        {
                            var currentRecordDeclaration = (RecordDeclarationSyntax)current;
                            if (!TryGetTokens(currentRecordDeclaration, out var classOrStructKeyword, out var recordKeyword))
                                return currentRecordDeclaration;

                            return currentRecordDeclaration
                                .WithClassOrStructKeyword(classOrStructKeyword)
                                .WithKeyword(recordKeyword);
                        });
                }
            }

            return Task.CompletedTask;
        }
    }
}
