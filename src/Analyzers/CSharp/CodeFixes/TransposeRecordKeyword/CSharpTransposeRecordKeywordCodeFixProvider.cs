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

        internal override CodeFixCategory CodeFixCategory
            => CodeFixCategory.Compile;

        private static bool TryGetRecordDeclaration(
            Diagnostic diagnostic, CancellationToken cancellationToken, [NotNullWhen(true)] out RecordDeclarationSyntax? recordDeclaration)
        {
            recordDeclaration = diagnostic.Location.FindNode(cancellationToken) as RecordDeclarationSyntax;
            return recordDeclaration != null;
        }

        private static bool TryGetTokens(
            RecordDeclarationSyntax recordDeclaration,
            out SyntaxToken recordKeyword,
            out SyntaxToken classOrStructKeyword,
            out SyntaxTrivia whitespaceTrivia)
        {
            recordKeyword = recordDeclaration.Keyword;
            if (!recordKeyword.IsMissing)
            {
                var leadingTrivia = recordKeyword.LeadingTrivia;
                if (leadingTrivia.Count >= 2)
                {
                    var skippedTrivia = leadingTrivia[^2];
                    whitespaceTrivia = leadingTrivia[^1];
                    if (skippedTrivia.Kind() == SyntaxKind.SkippedTokensTrivia &&
                        whitespaceTrivia.Kind() == SyntaxKind.WhitespaceTrivia)
                    {
                        var structure = (SkippedTokensTriviaSyntax)skippedTrivia.GetStructure()!;
                        var tokens = structure.Tokens;
                        if (tokens.Count == 1)
                        {
                            var lastSkippedToken = tokens.Single();
                            if (lastSkippedToken.Kind() is SyntaxKind.ClassKeyword or SyntaxKind.StructKeyword)
                            {
                                classOrStructKeyword = lastSkippedToken;
                                return true;
                            }
                        }
                    }
                }
            }

            classOrStructKeyword = default;
            whitespaceTrivia = default;
            return false;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var diagnostic = context.Diagnostics.First();
            if (TryGetRecordDeclaration(diagnostic, cancellationToken, out var recordDeclaration) &&
                TryGetTokens(recordDeclaration, out _, out _, out _))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => this.FixAsync(document, diagnostic, c)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
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
                            if (!TryGetTokens(currentRecordDeclaration, out var recordKeyword, out var classOrStructKeyword, out var whitespace))
                                return currentRecordDeclaration;

                            return currentRecordDeclaration
                                .WithClassOrStructKeyword(classOrStructKeyword.WithAppendedTrailingTrivia(whitespace))
                                .WithKeyword(recordKeyword.WithLeadingTrivia(
                                    recordKeyword.LeadingTrivia.Take(recordKeyword.LeadingTrivia.Count - 2)));
                        });
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpCodeFixesResources.Fix_record_declaration, createChangedDocument, nameof(CSharpTransposeRecordKeywordCodeFixProvider))
            {
            }
        }
    }
}
