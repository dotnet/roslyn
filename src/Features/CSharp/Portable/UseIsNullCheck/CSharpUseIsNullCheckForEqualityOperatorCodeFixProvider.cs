// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseIsNullCheck;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseIsNullCheckForEqualityOperatorCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public const string RemoveObjectCast = nameof(RemoveObjectCast);

        [ImportingConstructor]
        public CSharpUseIsNullCheckForEqualityOperatorCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseIsNullCheckDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
            => diagnostic.Properties[UseIsNullConstants.Kind] == UseIsNullConstants.CastAndEqualityKey;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            if (IsSupportedDiagnostic(diagnostic))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(CSharpFeaturesResources.Use_is_null_check,
                    c => this.FixAsync(context.Document, diagnostic, c)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (!IsSupportedDiagnostic(diagnostic))
                {
                    continue;
                }

                var binary = (BinaryExpressionSyntax)diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                var removeObjectCast = diagnostic.Properties.ContainsKey(RemoveObjectCast);
                editor.ReplaceNode(
                    binary,
                    (current, g, p) => Rewrite((BinaryExpressionSyntax)current, p),
                    removeObjectCast);
            }

            return Task.CompletedTask;
        }

        private static ExpressionSyntax Rewrite(BinaryExpressionSyntax binary, bool removeObjectCast)
        {
            var negate = !binary.IsKind(SyntaxKind.EqualsExpression);
            return binary.Right.IsKind(SyntaxKind.NullLiteralExpression)
                ? Rewrite(binary, binary.Left, binary.Right, negate, removeObjectCast)
                : Rewrite(binary, binary.Right, binary.Left, negate, removeObjectCast);
        }

        private static IsPatternExpressionSyntax Rewrite(
            BinaryExpressionSyntax binary, ExpressionSyntax expr, ExpressionSyntax nullLiteral,
            bool negate, bool removeObjectCast)
        {
            var coreExpr = removeObjectCast ? ((CastExpressionSyntax)expr).Expression : expr;
            var constant = negate ?
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)) :
                nullLiteral;
            return SyntaxFactory.IsPatternExpression(
                coreExpr.WithTriviaFrom(binary.Left),
                SyntaxFactory.Token(SyntaxKind.IsKeyword).WithTriviaFrom(binary.OperatorToken),
                SyntaxFactory.ConstantPattern(constant).WithTriviaFrom(binary.Right));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
