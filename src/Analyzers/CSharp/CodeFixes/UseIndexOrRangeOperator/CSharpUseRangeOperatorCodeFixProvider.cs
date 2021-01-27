﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static CodeFixHelpers;
    using static CSharpUseRangeOperatorDiagnosticAnalyzer;
    using static Helpers;
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseRangeOperatorCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseRangeOperatorCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseRangeOperatorDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

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
            var invocationNodes = diagnostics.Select(d => GetInvocationExpression(d, cancellationToken))
                                             .OrderByDescending(i => i.SpanStart)
                                             .ToImmutableArray();
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, invocationNodes,
                canReplace: (_1, _2) => true,
                (semanticModel, currentRoot, currentInvocation) =>
                    UpdateInvocation(semanticModel, currentRoot, currentInvocation, syntaxGenerator, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        private static SyntaxNode UpdateInvocation(
            SemanticModel semanticModel, SyntaxNode currentRoot,
            InvocationExpressionSyntax currentInvocation,
            SyntaxGenerator generator,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetOperation(currentInvocation, cancellationToken) is IInvocationOperation invocation)
            {
                var infoCache = new InfoCache(semanticModel.Compilation);
                var resultOpt = AnalyzeInvocation(
                    invocation, infoCache, analyzerOptionsOpt: null, cancellationToken);

                if (resultOpt != null)
                {
                    var result = resultOpt.Value;
                    var updatedNode = FixOne(result, generator);
                    if (updatedNode != null)
                    {
                        return currentRoot.ReplaceNode(result.Invocation, updatedNode);
                    }
                }
            }

            return currentRoot;
        }

        private static InvocationExpressionSyntax GetInvocationExpression(Diagnostic d, CancellationToken cancellationToken)
            => (InvocationExpressionSyntax)d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

        private static ExpressionSyntax FixOne(Result result, SyntaxGenerator generator)
        {
            var invocation = result.Invocation;

            var rangeExpression = CreateRangeExpression(result, generator);
            var argument = Argument(rangeExpression).WithAdditionalAnnotations(Formatter.Annotation);
            var arguments = SingletonSeparatedList(argument);

            if (result.MemberInfo.OverloadedMethodOpt == null)
            {
                var argList = invocation.ArgumentList;
                var argumentList = BracketedArgumentList(
                        Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(argList.OpenParenToken),
                        arguments,
                        Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(argList.CloseParenToken));
                if (invocation.Expression is MemberBindingExpressionSyntax)
                {
                    // x?.Substring(...) -> x?[...]
                    return ElementBindingExpression(argumentList);
                }

                if (invocation.Expression is IdentifierNameSyntax)
                {
                    // Substring(...) -> this[...]
                    return ElementAccessExpression(ThisExpression(), argumentList);
                }

                var expression = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    ? memberAccess.Expression // x.Substring(...) -> x[...]
                    : invocation.Expression;
                return ElementAccessExpression(expression, argumentList);
            }
            else
            {
                return invocation.ReplaceNode(
                    invocation.ArgumentList,
                    invocation.ArgumentList.WithArguments(arguments));
            }
        }

        private static RangeExpressionSyntax CreateRangeExpression(Result result, SyntaxGenerator generator)
            => result.Kind switch
            {
                ResultKind.Computed => CreateComputedRange(result),
                ResultKind.Constant => CreateConstantRange(result, generator),
                _ => throw ExceptionUtilities.Unreachable,
            };

        private static RangeExpressionSyntax CreateComputedRange(Result result)
        {
            // We have enough information now to generate `start..end`.  However, this will often
            // not be what the user wants.  For example, generating `start..expr.Length` is not as
            // desirable as `start..`.  Similarly, `start..(expr.Length - 1)` is not as desirable as
            // `start..^1`.  

            var startOperation = result.Op1;
            var endOperation = result.Op2;

            var lengthLikeProperty = result.MemberInfo.LengthLikeProperty;
            var instance = result.InvocationOperation.Instance;

            // If our start-op is actually equivalent to `expr.Length - val`, then just change our
            // start-op to be `val` and record that we should emit it as `^val`.
            var startFromEnd = IsFromEnd(lengthLikeProperty, instance, ref startOperation);
            var startExpr = (ExpressionSyntax)startOperation.Syntax;

            var endFromEnd = false;
            ExpressionSyntax endExpr = null;

            if (!(endOperation is null))
            {
                // We need to do the same for the second argument, since it's present.
                // Similarly, if our end-op is actually equivalent to `expr.Length - val`, then just
                // change our end-op to be `val` and record that we should emit it as `^val`.
                endFromEnd = IsFromEnd(lengthLikeProperty, instance, ref endOperation);

                // Check if the range goes to 'expr.Length'; if it does, we leave off
                // the end part of the range, i.e. `start..`.
                if (!IsInstanceLengthCheck(lengthLikeProperty, instance, endOperation))
                {
                    endExpr = (ExpressionSyntax)endOperation.Syntax;
                }
            }

            // If we're starting the range operation from 0, then we can just leave off the start of
            // the range. i.e. `..end`
            if (startOperation.ConstantValue.HasValue &&
                startOperation.ConstantValue.Value is 0)
            {
                startExpr = null;
            }

            return RangeExpression(
                startExpr != null && startFromEnd ? IndexExpression(startExpr) : startExpr?.Parenthesize(),
                endExpr != null && endFromEnd ? IndexExpression(endExpr) : endExpr?.Parenthesize());
        }

        private static RangeExpressionSyntax CreateConstantRange(Result result, SyntaxGenerator generator)
        {
            var constant1Syntax = (ExpressionSyntax)result.Op1.Syntax;

            // the form is s.Slice(constant1, s.Length - constant2).  Want to generate
            // s[constant1..(constant2-constant1)]
            var constant1 = GetInt32Value(result.Op1);
            var constant2 = GetInt32Value(result.Op2);

            var endExpr = (ExpressionSyntax)generator.LiteralExpression(constant2 - constant1);
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
            if (IsSubtraction(rangeOperation, out var subtraction) &&
                IsInstanceLengthCheck(lengthLikeProperty, instance, subtraction.LeftOperand))
            {
                rangeOperation = subtraction.RightOperand;
                return true;
            }

            return false;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_range_operator, createChangedDocument, CSharpAnalyzersResources.Use_range_operator)
            {
            }
        }
    }
}
