// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReverseForStatement
{
    using static IntegerUtilities;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ReverseForStatement), Shared]
    internal class CSharpReverseForStatementCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpReverseForStatementCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var forStatement = await context.TryGetRelevantNodeAsync<ForStatementSyntax>().ConfigureAwait(false);
            if (forStatement == null)
                return;

            // We support the following cases
            // 
            //  for (var x = start; x < end ; x++)
            //  for (...          ; ...     ; ++x)
            //  for (...          ; x <= end; ...)
            //  for (...          ; ...     ; x += 1)
            //
            //  for (var x = end    ; x >= start; x--)
            //  for (...            ; ...       ; --x)
            //  for (...            ; ...       ; x -= 1)

            var declaration = forStatement.Declaration;
            if (declaration == null ||
                declaration.Variables.Count != 1 ||
                forStatement.Incrementors.Count != 1)
            {
                return;
            }

            var variable = declaration.Variables[0];
            var after = forStatement.Incrementors[0];

            if (forStatement.Condition is not BinaryExpressionSyntax condition)
                return;

            var (document, _, cancellationToken) = context;
            if (MatchesIncrementPattern(variable, condition, after, out var start, out var equals, out var end) ||
                MatchesDecrementPattern(variable, condition, after, out end, out start))
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (IsUnsignedBoundary(semanticModel, variable, start, end, cancellationToken))
                {
                    // Don't allow reversing when you have unsigned types and are on the start/end
                    // of the legal values for that type.  i.e. `for (byte i = 0; i < 10; i++)` it's
                    // not trivial to reverse this.
                    return;
                }

                context.RegisterRefactoring(
                    CodeAction.Create(
                        CSharpFeaturesResources.Reverse_for_statement,
                        c => ReverseForStatementAsync(document, forStatement, c),
                        nameof(CSharpFeaturesResources.Reverse_for_statement)));
            }
        }

        private static bool IsUnsignedBoundary(
            SemanticModel semanticModel, VariableDeclaratorSyntax variable,
            ExpressionSyntax start, ExpressionSyntax end, CancellationToken cancellationToken)
        {
            var local = semanticModel.GetDeclaredSymbol(variable, cancellationToken) as ILocalSymbol;
            var startValue = semanticModel.GetConstantValue(start, cancellationToken);
            var endValue = semanticModel.GetConstantValue(end, cancellationToken);

            return local?.Type.SpecialType switch
            {
                SpecialType.System_Byte => IsUnsignedBoundary(startValue, endValue, byte.MaxValue),
                SpecialType.System_UInt16 => IsUnsignedBoundary(startValue, endValue, ushort.MaxValue),
                SpecialType.System_UInt32 => IsUnsignedBoundary(startValue, endValue, uint.MaxValue),
                SpecialType.System_UInt64 => IsUnsignedBoundary(startValue, endValue, ulong.MaxValue),
                _ => false,
            };
        }

        private static bool IsUnsignedBoundary(Optional<object?> startValue, Optional<object?> endValue, ulong maxValue)
            => ValueEquals(startValue, 0) || ValueEquals(endValue, maxValue);

        private static bool ValueEquals(Optional<object?> valueOpt, ulong value)
            => valueOpt.HasValue && IsIntegral(valueOpt.Value) && ToUInt64(valueOpt.Value) == value;

        private static bool MatchesIncrementPattern(
            VariableDeclaratorSyntax variable, BinaryExpressionSyntax condition, ExpressionSyntax after,
            [NotNullWhen(true)] out ExpressionSyntax? start, out bool equals, [NotNullWhen(true)] out ExpressionSyntax? end)
        {
            equals = default;
            end = null;
            return IsIncrementInitializer(variable, out start) &&
                   IsIncrementCondition(variable, condition, out equals, out end) &&
                   IsIncrementAfter(variable, after);
        }

        private static bool MatchesDecrementPattern(
            VariableDeclaratorSyntax variable, BinaryExpressionSyntax condition, ExpressionSyntax after,
            [NotNullWhen(true)] out ExpressionSyntax? end, [NotNullWhen(true)] out ExpressionSyntax? start)
        {
            start = null;
            return IsDecrementInitializer(variable, out end) &&
                   IsDecrementCondition(variable, condition, out start) &&
                   IsDecrementAfter(variable, after);
        }

        private static bool IsIncrementInitializer(VariableDeclaratorSyntax variable, [NotNullWhen(true)] out ExpressionSyntax? start)
        {
            start = variable.Initializer?.Value;
            return start != null;
        }

        private static bool IsIncrementCondition(
            VariableDeclaratorSyntax variable, BinaryExpressionSyntax condition,
            out bool equals, [NotNullWhen(true)] out ExpressionSyntax? end)
        {
            // i < ...   i <= ...
            if (condition.Kind() is SyntaxKind.LessThanExpression or
                SyntaxKind.LessThanOrEqualExpression)
            {
                end = condition.Right;
                equals = condition.Kind() == SyntaxKind.LessThanOrEqualExpression;
                return IsVariableReference(variable, condition.Left);
            }

            // ... > i   ... >= i
            if (condition.Kind() is SyntaxKind.GreaterThanExpression or
                SyntaxKind.GreaterThanOrEqualExpression)
            {
                end = condition.Left;
                equals = condition.Kind() == SyntaxKind.GreaterThanOrEqualExpression;
                return IsVariableReference(variable, condition.Right);
            }

            end = null;
            equals = default;
            return false;
        }

        private static bool IsIncrementAfter(
            VariableDeclaratorSyntax variable, ExpressionSyntax after)
        {
            // i++
            // ++i
            // i += 1
            if (after is PostfixUnaryExpressionSyntax postfixUnary &&
                postfixUnary.Kind() == SyntaxKind.PostIncrementExpression &&
                IsVariableReference(variable, postfixUnary.Operand))
            {
                return true;
            }

            if (after is PrefixUnaryExpressionSyntax prefixUnary &&
                prefixUnary.Kind() == SyntaxKind.PreIncrementExpression &&
                IsVariableReference(variable, prefixUnary.Operand))
            {
                return true;
            }

            if (after is AssignmentExpressionSyntax assignment &&
                assignment.Kind() == SyntaxKind.AddAssignmentExpression &&
                IsVariableReference(variable, assignment.Left) &&
                IsLiteralOne(assignment.Right))
            {
                return true;
            }

            return false;
        }

        private static bool IsLiteralOne(ExpressionSyntax expression)
            => expression.WalkDownParentheses() is LiteralExpressionSyntax literal && literal.Token.Value is 1;

        private static bool IsDecrementInitializer(
            VariableDeclaratorSyntax variable, [NotNullWhen(true)] out ExpressionSyntax? end)
        {
            end = variable.Initializer?.Value;
            return end != null;
        }

        private static bool IsDecrementCondition(
            VariableDeclaratorSyntax variable, BinaryExpressionSyntax condition,
            [NotNullWhen(true)] out ExpressionSyntax? start)
        {
            // i >= ...
            if (condition.Kind() == SyntaxKind.GreaterThanOrEqualExpression)
            {
                start = condition.Right;
                return IsVariableReference(variable, condition.Left);
            }

            // ... <= i
            if (condition.Kind() == SyntaxKind.LessThanOrEqualExpression)
            {
                start = condition.Left;
                return IsVariableReference(variable, condition.Right);
            }

            start = null;
            return false;
        }

        private static bool IsDecrementAfter(
            VariableDeclaratorSyntax variable, ExpressionSyntax after)
        {
            // i--
            // --i
            // i -= 1
            if (after is PostfixUnaryExpressionSyntax postfixUnary &&
                postfixUnary.Kind() == SyntaxKind.PostDecrementExpression &&
                IsVariableReference(variable, postfixUnary.Operand))
            {
                return true;
            }

            if (after is PrefixUnaryExpressionSyntax prefixUnary &&
                prefixUnary.Kind() == SyntaxKind.PreDecrementExpression &&
                IsVariableReference(variable, prefixUnary.Operand))
            {
                return true;
            }

            if (after is AssignmentExpressionSyntax assignment &&
                assignment.Kind() == SyntaxKind.SubtractAssignmentExpression &&
                IsVariableReference(variable, assignment.Left) &&
                IsLiteralOne(assignment.Right))
            {
                return true;
            }

            return false;
        }

        private static bool IsVariableReference(VariableDeclaratorSyntax variable, ExpressionSyntax expr)
            => expr.WalkDownParentheses() is IdentifierNameSyntax identifier &&
               identifier.Identifier.ValueText == variable.Identifier.ValueText;

        private static async Task<Document> ReverseForStatementAsync(
            Document document, ForStatementSyntax forStatement, CancellationToken cancellationToken)
        {
            var variable = forStatement.Declaration!.Variables[0];
            var condition = (BinaryExpressionSyntax)forStatement.Condition!;
            var after = forStatement.Incrementors[0];

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            var generator = editor.Generator;
            if (MatchesIncrementPattern(
                    variable, condition, after,
                    out var start, out var equals, out var end))
            {
                //  for (var x = start  ; x < end   ; ...) =>
                //  for (var x = end - 1; x >= start; ...)
                //
                //  for (var x = start; x <= end  ; ...) =>
                //  for (var x = end  ; x >= start; ...) =>

                var newStart = equals
                    ? end
                    : (ExpressionSyntax)generator.SubtractExpression(end, generator.LiteralExpression(1));

                editor.ReplaceNode(variable.Initializer!.Value, Reduce(newStart));
                editor.ReplaceNode(condition, Reduce(Invert(variable, condition, start)));
            }
            else if (MatchesDecrementPattern(variable, condition, after, out end, out start))
            {
                //  for (var x = end; x >= start; x--) =>
                //  for (var x = start; x <= end; x--)
                editor.ReplaceNode(variable.Initializer!.Value, Reduce(start));
                editor.ReplaceNode(condition, Reduce(Invert(variable, condition, end)));
            }
            else
            {
                throw new InvalidOperationException();
            }

            editor.ReplaceNode(after, InvertAfter(after));
            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private static ExpressionSyntax Reduce(ExpressionSyntax expr)
        {
            expr = expr.WalkDownParentheses();

            if (expr is BinaryExpressionSyntax outerBinary)
            {
                var reducedLeft = Reduce(outerBinary.Left);
                var reducedRight = Reduce(outerBinary.Right);

                // (... + 1) - 1  =>  ...
                // (... - 1) + 1  =>  ...
                {
                    if (reducedLeft is BinaryExpressionSyntax innerLeft &&
                        IsLiteralOne(innerLeft.Right) &&
                        IsLiteralOne(reducedRight))
                    {
                        if ((outerBinary.Kind() == SyntaxKind.SubtractExpression && innerLeft.Kind() == SyntaxKind.AddExpression) ||
                            (outerBinary.Kind() == SyntaxKind.AddExpression && innerLeft.Kind() == SyntaxKind.SubtractExpression))
                        {
                            return Reduce(innerLeft.Left);
                        }
                    }
                }

                // v <= x - 1   =>   v < x
                // x - 1 >= v   =>   x > v
                {
                    if (outerBinary.Kind() == SyntaxKind.LessThanOrEqualExpression &&
                        reducedRight is BinaryExpressionSyntax innerRight &&
                        innerRight.Kind() == SyntaxKind.SubtractExpression &&
                        IsLiteralOne(innerRight.Right))
                    {
                        var newOperator = SyntaxFactory.Token(SyntaxKind.LessThanToken).WithTriviaFrom(outerBinary.OperatorToken);
                        return Reduce(outerBinary.WithRight(innerRight.Left)
                                                 .WithOperatorToken(newOperator));
                    }

                    if (outerBinary.Kind() == SyntaxKind.GreaterThanOrEqualExpression &&
                        reducedLeft is BinaryExpressionSyntax innerLeft &&
                        innerLeft.Kind() == SyntaxKind.SubtractExpression &&
                        IsLiteralOne(innerLeft.Right))
                    {
                        var newOperator = SyntaxFactory.Token(SyntaxKind.GreaterThanToken).WithTriviaFrom(outerBinary.OperatorToken);
                        return Reduce(outerBinary.WithRight(innerLeft.Left)
                                                 .WithOperatorToken(newOperator));
                    }
                }
            }

            return expr.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static BinaryExpressionSyntax Invert(
            VariableDeclaratorSyntax variable, BinaryExpressionSyntax condition, ExpressionSyntax operand)
        {
            var (left, right) = IsVariableReference(variable, condition.Left)
                ? (condition.Left, operand)
                : (operand, condition.Right);

            var newOperatorKind = condition.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression
                ? SyntaxKind.GreaterThanEqualsToken
                : SyntaxKind.LessThanEqualsToken;

            var newExpressionKind = newOperatorKind == SyntaxKind.GreaterThanEqualsToken
                ? SyntaxKind.GreaterThanOrEqualExpression
                : SyntaxKind.LessThanOrEqualExpression;

            var newOperator = SyntaxFactory.Token(newOperatorKind).WithTriviaFrom(condition.OperatorToken);
            return SyntaxFactory.BinaryExpression(newExpressionKind, left, newOperator, right);
        }

        private static ExpressionSyntax InvertAfter(ExpressionSyntax after)
        {
            var opToken = after switch
            {
                PostfixUnaryExpressionSyntax postfixUnary => postfixUnary.OperatorToken,
                PrefixUnaryExpressionSyntax prefixUnary => prefixUnary.OperatorToken,
                AssignmentExpressionSyntax assignment => assignment.OperatorToken,
                _ => throw ExceptionUtilities.UnexpectedValue(after.Kind())
            };

            var newKind = opToken.Kind() switch
            {
                SyntaxKind.MinusMinusToken => SyntaxKind.PlusPlusToken,
                SyntaxKind.PlusPlusToken => SyntaxKind.MinusMinusToken,
                SyntaxKind.PlusEqualsToken => SyntaxKind.MinusEqualsToken,
                SyntaxKind.MinusEqualsToken => SyntaxKind.PlusEqualsToken,
                _ => throw ExceptionUtilities.UnexpectedValue(opToken.Kind())
            };

            var newOpToken = SyntaxFactory.Token(newKind).WithTriviaFrom(opToken);
            return after.ReplaceToken(opToken, newOpToken);
        }
    }
}
