// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
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
            var invocation = (InvocationExpressionSyntax)diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            ExpressionSyntax start = null, end = null;

            if (!diagnostic.Properties.ContainsKey(CSharpUseRangeOperatorDiagnosticAnalyzer.OmitStart))
            {
                start =(ExpressionSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
                start = MakeIndexExpression(start,
                    diagnostic.Properties.ContainsKey(CSharpUseRangeOperatorDiagnosticAnalyzer.StartFromEnd));
            }

            if (!diagnostic.Properties.ContainsKey(CSharpUseRangeOperatorDiagnosticAnalyzer.OmitEnd))
            {
                end = (ExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
                end = MakeIndexExpression(end,
                    diagnostic.Properties.ContainsKey(CSharpUseRangeOperatorDiagnosticAnalyzer.EndFromEnd));
            }

            var argList = invocation.ArgumentList;
            var elementAccess = SyntaxFactory.ElementAccessExpression(
                invocation.Expression,
                SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(argList.OpenParenToken),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.RangeExpression(start, end)).WithAdditionalAnnotations(Formatter.Annotation)),
                    SyntaxFactory.Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(argList.CloseParenToken)));

            editor.ReplaceNode(invocation, elementAccess);
        }

        private static ExpressionSyntax MakeIndexExpression(ExpressionSyntax value, bool fromEnd)
            => fromEnd
                ? SyntaxFactory.PrefixUnaryExpression(SyntaxKind.IndexExpression, value.Parenthesize())
                : value;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Use_range_operator, createChangedDocument, FeaturesResources.Use_range_operator)
            {
            }
        }
    }
}
