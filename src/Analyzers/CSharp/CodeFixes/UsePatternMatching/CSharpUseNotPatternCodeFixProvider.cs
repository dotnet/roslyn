// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseNotPatternCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseNotPatternCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseNotPatternDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessDiagnostic(editor, diagnostic, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private static void ProcessDiagnostic(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
#if CODE_STYLE
            Contract.Fail("We should have never gotten here as CODE_STYLE doesn't support C# 9 yet.");
#else

            var notExpressionLocation = diagnostic.AdditionalLocations[0];

            var notExpression = (PrefixUnaryExpressionSyntax)notExpressionLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var parenthesizedExpression = (ParenthesizedExpressionSyntax)notExpression.Operand;
            var isPattern = (IsPatternExpressionSyntax)parenthesizedExpression.Expression;

            var updatedPattern = isPattern.WithPattern(UnaryPattern(Token(SyntaxKind.NotKeyword), isPattern.Pattern));
            editor.ReplaceNode(
                notExpression,
                updatedPattern.WithPrependedLeadingTrivia(notExpression.GetLeadingTrivia())
                              .WithAppendedTrailingTrivia(notExpression.GetTrailingTrivia()));

#endif

        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_pattern_matching, createChangedDocument, CSharpAnalyzersResources.Use_pattern_matching)
            {
            }
        }
    }
}
