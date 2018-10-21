// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using static CSharpUseRangeOperatorDiagnosticAnalyzer;
    using static Helpers;
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseRangeOperatorCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseRangeOperatorDiagnosticId);
 
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                FixOne(diagnostic, editor, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private void FixOne(
            Diagnostic diagnostic, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var invocation = (InvocationExpressionSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var expression = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Expression
                : invocation.Expression;
            var start = (ExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var end = (ExpressionSyntax)diagnostic.AdditionalLocations[2].FindNode(getInnermostNodeForTie: true, cancellationToken);

            var rangeExpression = RangeExpression(
                GetExpression(diagnostic.Properties, start, OmitStart, StartFromEnd),
                GetExpression(diagnostic.Properties, end, OmitEnd, EndFromEnd));

            var argList = invocation.ArgumentList;
            var elementAccess = ElementAccessExpression(
                expression,
                BracketedArgumentList(
                    Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(argList.OpenParenToken),
                    SingletonSeparatedList(Argument(rangeExpression).WithAdditionalAnnotations(Formatter.Annotation)),
                    Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(argList.CloseParenToken)));

            editor.ReplaceNode(invocation, elementAccess);
        }

        private static ExpressionSyntax GetExpression(ImmutableDictionary<string, string> props, ExpressionSyntax expr, string omitKey, string fromEndKey)
            => props.ContainsKey(omitKey)
                ? null
                : props.ContainsKey(fromEndKey) ? IndexExpression(expr) : expr;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Use_range_operator, createChangedDocument, FeaturesResources.Use_range_operator)
            {
            }
        }
    }
}
