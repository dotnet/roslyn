// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                FixOne(semanticModel, editor, diagnostic, cancellationToken);
            }
        }

        private void FixOne(
            SemanticModel semanticModel, SyntaxEditor editor,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var invocation = (InvocationExpressionSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var expression = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Expression
                : invocation.Expression;

            var rangeExpression = CreateRangeExpression(
                semanticModel, diagnostic, invocation, cancellationToken);

            var argList = invocation.ArgumentList;
            var elementAccess = ElementAccessExpression(
                expression,
                BracketedArgumentList(
                    Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(argList.OpenParenToken),
                    SingletonSeparatedList(Argument(rangeExpression).WithAdditionalAnnotations(Formatter.Annotation)),
                    Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(argList.CloseParenToken)));

            editor.ReplaceNode(invocation, elementAccess);
        }

        private RangeExpressionSyntax CreateRangeExpression(
            SemanticModel semanticModel, Diagnostic diagnostic,
            InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var properties = diagnostic.Properties;
            if (properties.ContainsKey(ComputedRange))
            {
                return CreateComputedRange(semanticModel, diagnostic, invocation, cancellationToken);
            }
            else if (properties.ContainsKey(ConstantRange))
            {
                return CreateConstantRange(semanticModel, diagnostic, cancellationToken);
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private RangeExpressionSyntax CreateComputedRange(
            SemanticModel semanticModel, Diagnostic diagnostic,
            InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var startExpr = (ExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var endExpr = (ExpressionSyntax)diagnostic.AdditionalLocations[2].FindNode(getInnermostNodeForTie: true, cancellationToken);

            // We have enough information now to generate `start..end`.  However, this will often
            // not be what the user wants.  For example, generating `start..expr.Length` is not as
            // desirable as `start..`.  Similarly, `start..(expr.Length - 1)` is not as desirable as
            // `start..^1`.  

            var startFromEnd = false;
            var endFromEnd = false;

            if (semanticModel.GetOperation(invocation, cancellationToken) is IInvocationOperation invocationOp &&
                invocationOp.Instance != null)
            {
                var startOperation = semanticModel.GetOperation(startExpr, cancellationToken);
                var endOperation = semanticModel.GetOperation(endExpr, cancellationToken);
                var checker = new InfoCache(semanticModel.Compilation);

                if (startOperation != null && 
                    endOperation != null &&
                    checker.TryGetMemberInfo(invocationOp.TargetMethod.ContainingType, out var memberInfo))
                {
                    var lengthLikeProperty = memberInfo.LengthLikeProperty;

                    // If our start-op is actually equivalent to `expr.Length - val`, then just change our
                    // start-op to be `val` and record that we should emit it as `^val`.
                    startFromEnd = IsFromEnd(lengthLikeProperty, invocationOp.Instance, ref startOperation);
                    startExpr = (ExpressionSyntax)startOperation.Syntax;

                    // Similarly, if our end-op is actually equivalent to `expr.Length - val`, then just
                    // change our end-op to be `val` and record that we should emit it as `^val`.
                    endFromEnd = IsFromEnd(lengthLikeProperty, invocationOp.Instance, ref endOperation);
                    endExpr = (ExpressionSyntax)endOperation.Syntax;

                    // If the range operation goes to 'expr.Length' then we can just leave off the end part
                    // of the range.  i.e. `start..`
                    if (IsInstanceLengthCheck(lengthLikeProperty, invocationOp.Instance, endOperation))
                    {
                        endExpr = null;
                    }

                    // If we're starting the range operation from 0, then we can just leave off the start of
                    // the range. i.e. `..end`
                    if (startOperation.ConstantValue.HasValue &&
                        startOperation.ConstantValue.Value is 0)
                    {
                        startExpr = null;
                    }
                }
            }

            return RangeExpression(
                startExpr != null && startFromEnd ? IndexExpression(startExpr) : startExpr,
                endExpr != null && endFromEnd ? IndexExpression(endExpr) : endExpr);
        }

        private static ExpressionSyntax GetExpression(ImmutableDictionary<string, string> props, ExpressionSyntax expr, string omitKey, string fromEndKey)
            => props.ContainsKey(omitKey)
                ? null
                : props.ContainsKey(fromEndKey) ? IndexExpression(expr) : expr;

        private static RangeExpressionSyntax CreateConstantRange(
            SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var constant1Syntax = (ExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var constant2Syntax = (ExpressionSyntax)diagnostic.AdditionalLocations[2].FindNode(getInnermostNodeForTie: true, cancellationToken);

            // the form is s.Slice(constant1, s.Length - constant2).  Want to generate
            // s[constant1..(constant2-constant1)]
            var constant1 = GetInt32Value(semanticModel.GetOperation(constant1Syntax));
            var constant2 = GetInt32Value(semanticModel.GetOperation(constant2Syntax));

            var endExpr = (ExpressionSyntax)CSharpSyntaxGenerator.Instance.LiteralExpression(constant2 - constant1);
            return RangeExpression(
                constant1Syntax,
                IndexExpression(endExpr));
        }

        private static int GetInt32Value(IOperation operation)
            => (int)operation.ConstantValue.Value;

        /// <summary>
        /// check if its the form: `expr.Length - value`.  If so, update rangeOperation to then
        /// point to 'value' so that we can generate '^value'.
        /// </summary>
        private static bool IsFromEnd(
            IPropertySymbol lengthLikeProperty, IOperation instance, ref IOperation rangeOperation)
        {
            if (rangeOperation is IBinaryOperation binaryOperation &&
                binaryOperation.OperatorKind == BinaryOperatorKind.Subtract &&
                IsInstanceLengthCheck(lengthLikeProperty, instance, binaryOperation.LeftOperand))
            {
                rangeOperation = binaryOperation.RightOperand;
                return true;
            }

            return false;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Use_range_operator, createChangedDocument, FeaturesResources.Use_range_operator)
            {
            }
        }
    }
}
