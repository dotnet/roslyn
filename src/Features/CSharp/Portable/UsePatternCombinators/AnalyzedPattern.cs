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
        private AnalyzedPattern()
        {
        }

        /// <summary>
        /// Base class for patterns that target a specific expression, i.e. non-combinators
        /// </summary>
        internal abstract class Test : AnalyzedPattern
        {
            public readonly IOperation TargetOperation;

            protected Test(IOperation targetOperation)
                => TargetOperation = targetOperation;
        }

        /// <summary>
        /// Represents a type-pattern, constructed from is-expression
        /// </summary>
        internal sealed class Type : Test
        {
            public readonly TypeSyntax TypeSyntax;

            public Type(TypeSyntax expression, IOperation target) : base(target)
                => TypeSyntax = expression;
        }

        /// <summary>
        /// Represents a source-pattern, constructed from C# patterns
        /// </summary>
        internal sealed class Source : Test
        {
            public readonly PatternSyntax PatternSyntax;

            public Source(PatternSyntax patternSyntax, IOperation target) : base(target)
                => PatternSyntax = patternSyntax;
        }

        /// <summary>
        /// Represents a constant-pattern, constructed from an equality check
        /// </summary>
        internal sealed class Constant : Test
        {
            public readonly ExpressionSyntax ExpressionSyntax;

            public Constant(ExpressionSyntax expression, IOperation target) : base(target)
                => ExpressionSyntax = expression;
        }

        /// <summary>
        /// Represents a relational-pattern, constructed from relational operators
        /// </summary>
        internal sealed class Relational : Test
        {
            public readonly BinaryOperatorKind OperatorKind;
            public readonly ExpressionSyntax Value;

            public Relational(BinaryOperatorKind operatorKind, ExpressionSyntax value, IOperation target) : base(target)
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

            private Binary(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern, bool isDisjunctive, SyntaxToken token)
            {
                Left = leftPattern;
                Right = rightPattern;
                IsDisjunctive = isDisjunctive;
                Token = token;
            }

            public static AnalyzedPattern Create(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern, bool isDisjunctive, SyntaxToken token)
            {
                return !isDisjunctive && (leftPattern, rightPattern) is (Not left, Not right)
                    ? Not.Create(new Binary(left.Pattern, right.Pattern, isDisjunctive: true, token))
                    : new Binary(leftPattern, rightPattern, isDisjunctive, token);
            }
        }

        /// <summary>
        /// Represents a not-pattern, constructed from inequality check or a logical-not expression.
        /// </summary>
        internal sealed class Not : AnalyzedPattern
        {
            public readonly AnalyzedPattern Pattern;

            private Not(AnalyzedPattern pattern)
                => Pattern = pattern;

            private static BinaryOperatorKind Negate(BinaryOperatorKind kind) => kind switch
            {
                BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
                BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
                BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
                BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };

            public static AnalyzedPattern Create(AnalyzedPattern pattern) => pattern switch
            {
                Not p => p.Pattern,
                Relational p => new Relational(Negate(p.OperatorKind), p.Value, p.TargetOperation),
                _ => new Not(pattern)
            };
        }
    }
}
