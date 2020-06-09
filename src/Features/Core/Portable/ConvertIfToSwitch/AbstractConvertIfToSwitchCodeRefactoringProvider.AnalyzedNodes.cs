// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TIfStatementSyntax,
        TExpressionSyntax,
        TIsExpressionSyntax,
        TPatternSyntax>
        where TIfStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TIsExpressionSyntax : SyntaxNode
        where TPatternSyntax : SyntaxNode
    {
        /// <summary>
        /// Represents a switch-section constructed from a series of
        /// if-conditions, possibly combined with logical-or operator
        /// </summary>
        internal sealed class AnalyzedSwitchSection
        {
            public readonly ImmutableArray<AnalyzedSwitchLabel> Labels;
            public readonly IOperation Body;
            public readonly SyntaxNode SyntaxToRemove;

            public AnalyzedSwitchSection(ImmutableArray<AnalyzedSwitchLabel> labels, IOperation body, SyntaxNode syntaxToRemove)
            {
                Labels = labels;
                Body = body;
                SyntaxToRemove = syntaxToRemove;
            }
        }

        /// <summary>
        /// Represents a switch-label constructed from a series of
        /// if-conditions, possibly combined by logical-and operator
        /// </summary>
        internal sealed class AnalyzedSwitchLabel
        {
            public readonly AnalyzedPattern Pattern;
            public readonly ImmutableArray<TExpressionSyntax> Guards;

            public AnalyzedSwitchLabel(AnalyzedPattern pattern, ImmutableArray<TExpressionSyntax> guards)
            {
                Pattern = pattern;
                Guards = guards;
            }
        }

        /// <summary>
        /// Base class to represents a case clause (pattern) constructed from various checks
        /// </summary>
        internal abstract class AnalyzedPattern
        {
            private AnalyzedPattern()
            {
            }

            /// <summary>
            /// Represents a type-pattern, constructed from is-expression
            /// </summary>
            internal sealed class Type : AnalyzedPattern
            {
                public readonly TIsExpressionSyntax IsExpressionSyntax;

                public Type(TIsExpressionSyntax expression)
                    => IsExpressionSyntax = expression;
            }

            /// <summary>
            /// Represents a source-pattern constructed from C# patterns
            /// </summary>
            internal sealed class Source : AnalyzedPattern
            {
                public readonly TPatternSyntax PatternSyntax;

                public Source(TPatternSyntax patternSyntax)
                    => PatternSyntax = patternSyntax;
            }

            /// <summary>
            /// Represents a constant-pattern constructed from an equality check
            /// </summary>
            internal sealed class Constant : AnalyzedPattern
            {
                public readonly TExpressionSyntax ExpressionSyntax;

                public Constant(TExpressionSyntax expression)
                    => ExpressionSyntax = expression;
            }

            /// <summary>
            /// Represents a relational-pattern constructed from comparison operators
            /// </summary>
            internal sealed class Relational : AnalyzedPattern
            {
                public readonly BinaryOperatorKind OperatorKind;
                public readonly TExpressionSyntax Value;

                public Relational(BinaryOperatorKind operatorKind, TExpressionSyntax value)
                {
                    OperatorKind = operatorKind;
                    Value = value;
                }
            }

            /// <summary>
            /// Represents a range-pattern constructed from a couple of comparison operators
            /// </summary>
            internal sealed class Range : AnalyzedPattern
            {
                public readonly TExpressionSyntax LowerBound;
                public readonly TExpressionSyntax HigherBound;

                public Range(TExpressionSyntax lowerBound, TExpressionSyntax higherBound)
                {
                    LowerBound = lowerBound;
                    HigherBound = higherBound;
                }
            }

            /// <summary>
            /// Represents an and-pattern, constructed from two other patterns.
            /// </summary>
            internal sealed class And : AnalyzedPattern
            {
                public readonly AnalyzedPattern LeftPattern;
                public readonly AnalyzedPattern RightPattern;

                public And(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern)
                {
                    LeftPattern = leftPattern;
                    RightPattern = rightPattern;
                }
            }
        }
    }
}
