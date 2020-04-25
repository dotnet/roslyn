// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    /// <summary>
    /// Base class to represent a pattern constructed from various checks
    /// </summary>
    internal abstract class AnalyzedPattern
    {
        public readonly ExpressionSyntax TargetExpression;

        private AnalyzedPattern(ExpressionSyntax target)
            => TargetExpression = target;

        /// <summary>
        /// Represents a type-pattern, constructed from is-expression
        /// </summary>
        internal sealed class Type : AnalyzedPattern
        {
            public readonly TypeSyntax TypeSyntax;

            public Type(TypeSyntax expression, ExpressionSyntax target) : base(target)
                => TypeSyntax = expression;
        }

        /// <summary>
        /// Represents a source-pattern, constructed from C# patterns
        /// </summary>
        internal sealed class Source : AnalyzedPattern
        {
            public readonly PatternSyntax PatternSyntax;

            public Source(PatternSyntax patternSyntax, ExpressionSyntax target) : base(target)
                => PatternSyntax = patternSyntax;
        }

        /// <summary>
        /// Represents a constant-pattern, constructed from an equality check
        /// </summary>
        internal sealed class Constant : AnalyzedPattern
        {
            public readonly ExpressionSyntax ExpressionSyntax;

            public Constant(ExpressionSyntax expression, ExpressionSyntax target) : base(target)
                => ExpressionSyntax = expression;
        }

        /// <summary>
        /// Represents a relational-pattern, constructed from relational operators
        /// </summary>
        internal sealed class Relational : AnalyzedPattern
        {
            public readonly BinaryOperatorKind OperatorKind;
            public readonly ExpressionSyntax Value;

            public Relational(BinaryOperatorKind operatorKind, ExpressionSyntax value, ExpressionSyntax target) : base(target)
            {
                OperatorKind = operatorKind;
                Value = value;
            }
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

            private Binary(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern, bool isDisjunctive, SyntaxToken token, ExpressionSyntax target) : base(target)
            {
                Left = leftPattern;
                Right = rightPattern;
                IsDisjunctive = isDisjunctive;
                Token = token;
            }

            public static AnalyzedPattern? Create(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern, bool isDisjunctive, SyntaxToken token)
            {
                var target = leftPattern.TargetExpression;
                if (!SyntaxFactory.AreEquivalent(target, rightPattern.TargetExpression))
                    return null;

                // We factor out not-patterns in a conjunction.
                // For instance: `not 1 and not 2` is simplified as `not (1 or 2)`.

                // Note that we don't do the same for disjunction, because the result would not be the same.
                // For instance: `not 1 or not 2` cannot be rewritten as `not (1 and 2)`.
                // The latter could be always true while that is not the case in the original form.

                return !isDisjunctive && (leftPattern, rightPattern) is (Not left, Not right)
                    ? Not.Create(new Binary(left.Pattern, right.Pattern, isDisjunctive: true, token, target))
                    : new Binary(leftPattern, rightPattern, isDisjunctive, token, target);
            }
        }

        /// <summary>
        /// Represents a not-pattern, constructed from inequality check or a logical-not expression.
        /// </summary>
        internal sealed class Not : AnalyzedPattern
        {
            public readonly AnalyzedPattern Pattern;

            private Not(AnalyzedPattern pattern, ExpressionSyntax target) : base(target)
                => Pattern = pattern;

            private static BinaryOperatorKind Negate(BinaryOperatorKind kind)
                => kind switch
                {
                    BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
                    BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
                    BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
                    BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
                    var v => throw ExceptionUtilities.UnexpectedValue(v)
                };

            public static AnalyzedPattern? Create(AnalyzedPattern? pattern)
                => pattern switch
                {
                    null => null,
                    Not p => p.Pattern, // double negative
                    Relational p => new Relational(Negate(p.OperatorKind), p.Value, p.TargetExpression),
                    _ => new Not(pattern, pattern.TargetExpression)
                };
        }
    }
}
