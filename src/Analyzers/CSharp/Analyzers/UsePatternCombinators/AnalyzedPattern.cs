// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;

using static SyntaxFactory;

/// <summary>
/// Base class to represent a pattern constructed from various checks
/// </summary>
internal abstract class AnalyzedPattern
{
    public readonly IOperation Target;

    private AnalyzedPattern(IOperation target)
        => Target = target;

    /// <summary>
    /// Represents a type-pattern, constructed from an is-expression
    /// </summary>
    internal sealed class Type : AnalyzedPattern
    {
        private static readonly SyntaxAnnotation s_annotation = new();

        public static Type? TryCreate(BinaryExpressionSyntax binaryExpression, IIsTypeOperation operation)
        {
            Contract.ThrowIfNull(operation.SemanticModel);
            if (binaryExpression.Right is not TypeSyntax typeSyntax)
            {
                return null;
            }

            // We are coming from a type pattern, which likes to bind to types, but converting to
            // patters which like to bind to expressions. For example, given:
            //
            // if (T is C.X || T is Y) { }
            //
            // we would want to convert to:
            //
            // if (T is C.X or Y)
            //
            // In the first case the compiler will bind to types named C or Y that are in scope
            // but in the second it will also bind to a fields, methods etc. which for 'Y' changes
            // semantics, and for 'C.X' could be a compile error.
            //
            // So lets create a pattern syntax and make sure the result is the same
            var dummyStatement = ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName("_"),
                IsPatternExpression(
                    binaryExpression.Left,
                    ConstantPattern(ParenthesizedExpression(binaryExpression.Right.WithAdditionalAnnotations(s_annotation)))
                )
            ));

            if (operation.SemanticModel.TryGetSpeculativeSemanticModel(typeSyntax.SpanStart, dummyStatement, out var speculativeModel))
            {
                var originalInfo = operation.SemanticModel.GetTypeInfo(binaryExpression.Right);
                var newInfo = speculativeModel.GetTypeInfo(dummyStatement.GetAnnotatedNodes(s_annotation).Single());
                if (!originalInfo.Equals(newInfo))
                {
                    return null;
                }
            }

            return new Type(typeSyntax, operation.ValueOperand);
        }

        public readonly TypeSyntax TypeSyntax;

        private Type(TypeSyntax type, IOperation target) : base(target)
            => TypeSyntax = type;
    }

    /// <summary>
    /// Represents a source-pattern, constructed from C# patterns
    /// </summary>
    internal sealed class Source(PatternSyntax patternSyntax, IOperation target) : AnalyzedPattern(target)
    {
        public readonly PatternSyntax PatternSyntax = patternSyntax;
    }

    /// <summary>
    /// Represents a constant-pattern, constructed from an equality check
    /// </summary>
    internal sealed class Constant(ExpressionSyntax expression, IOperation target) : AnalyzedPattern(target)
    {
        public readonly ExpressionSyntax ExpressionSyntax = expression;
    }

    /// <summary>
    /// Represents a relational-pattern, constructed from relational operators
    /// </summary>
    internal sealed class Relational(BinaryOperatorKind operatorKind, ExpressionSyntax value, IOperation target) : AnalyzedPattern(target)
    {
        public readonly BinaryOperatorKind OperatorKind = operatorKind;
        public readonly ExpressionSyntax Value = value;
    }

    /// <summary>
    /// Represents an and/or pattern, constructed from a logical and/or expression.
    /// </summary>
    internal sealed class Binary : AnalyzedPattern
    {
        public readonly AnalyzedPattern Left;
        public readonly AnalyzedPattern Right;
        public readonly bool IsDisjunctive;
        public readonly SyntaxToken Token;

        private Binary(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern, bool isDisjunctive, SyntaxToken token, IOperation target) : base(target)
        {
            Left = leftPattern;
            Right = rightPattern;
            IsDisjunctive = isDisjunctive;
            Token = token;
        }

        public static AnalyzedPattern? TryCreate(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern, bool isDisjunctive, SyntaxToken token)
        {
            var leftTarget = leftPattern.Target;
            var rightTarget = rightPattern.Target;

            var leftConv = (leftTarget as IConversionOperation)?.Conversion;
            var rightConv = (rightTarget as IConversionOperation)?.Conversion;

            var target = (leftConv, rightConv) switch
            {
                ({ IsUserDefined: true }, _) or
                (_, { IsUserDefined: true }) => null,

                // If the original targets are implicitly converted due to usage of operators,
                // both targets must have been converted to the same type, otherwise we bail.
                ({ IsImplicit: true }, { IsImplicit: true }) when !Equals(leftTarget.Type, rightTarget.Type) => null,

                // If either of targets are implicitly converted but not both,
                // we take the conversion node so that we can generate a cast off of it.
                (null, { IsImplicit: true }) => rightTarget,
                ({ IsImplicit: true }, null) => leftTarget,

                // If no implicit conversion is present, we just pick either side and continue.
                _ => leftTarget,
            };

            if (target is null)
                return null;

            var compareTarget = target == leftTarget ? rightTarget : leftTarget;
            if (!AreEquivalent(target.Syntax, compareTarget.Syntax))
                return null;

            return new Binary(leftPattern, rightPattern, isDisjunctive, token, target);
        }
    }

    /// <summary>
    /// Represents a not-pattern, constructed from inequality check or a logical-not expression.
    /// </summary>
    internal sealed class Not : AnalyzedPattern
    {
        public readonly AnalyzedPattern Pattern;

        private Not(AnalyzedPattern pattern, IOperation target) : base(target)
            => Pattern = pattern;

        private static BinaryOperatorKind Negate(BinaryOperatorKind kind)
        {
            return kind switch
            {
                BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
                BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
                BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
                BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };
        }

        public static AnalyzedPattern? TryCreate(AnalyzedPattern? pattern)
        {
            return pattern switch
            {
                null => null,
                Not p => p.Pattern, // Avoid double negative
                Relational p => new Relational(Negate(p.OperatorKind), p.Value, p.Target),
                Binary { Left: Not left, Right: Not right } p // Apply demorgans's law
                    => Binary.TryCreate(left.Pattern, right.Pattern, !p.IsDisjunctive, p.Token),
                _ => new Not(pattern, pattern.Target)
            };
        }
    }
}
