// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal sealed class AnalyzedSwitchSection(ImmutableArray<AnalyzedSwitchLabel> labels, IOperation body, SyntaxNode syntaxToRemove)
        {
            public readonly ImmutableArray<AnalyzedSwitchLabel> Labels = labels;
            public readonly IOperation Body = body;
            public readonly SyntaxNode SyntaxToRemove = syntaxToRemove;
        }

        /// <summary>
        /// Represents a switch-label constructed from a series of
        /// if-conditions, possibly combined by logical-and operator
        /// </summary>
        internal sealed class AnalyzedSwitchLabel(AnalyzedPattern pattern, ImmutableArray<TExpressionSyntax> guards)
        {
            public readonly AnalyzedPattern Pattern = pattern;
            public readonly ImmutableArray<TExpressionSyntax> Guards = guards;
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
            internal sealed class Type(TIsExpressionSyntax expression) : AnalyzedPattern
            {
                public readonly TIsExpressionSyntax IsExpressionSyntax = expression;
            }

            /// <summary>
            /// Represents a source-pattern constructed from C# patterns
            /// </summary>
            internal sealed class Source(TPatternSyntax patternSyntax) : AnalyzedPattern
            {
                public readonly TPatternSyntax PatternSyntax = patternSyntax;
            }

            /// <summary>
            /// Represents a constant-pattern constructed from an equality check
            /// </summary>
            internal sealed class Constant(TExpressionSyntax expression) : AnalyzedPattern
            {
                public readonly TExpressionSyntax ExpressionSyntax = expression;
            }

            /// <summary>
            /// Represents a relational-pattern constructed from comparison operators
            /// </summary>
            internal sealed class Relational(BinaryOperatorKind operatorKind, TExpressionSyntax value) : AnalyzedPattern
            {
                public readonly BinaryOperatorKind OperatorKind = operatorKind;
                public readonly TExpressionSyntax Value = value;
            }

            /// <summary>
            /// Represents a range-pattern constructed from a couple of comparison operators
            /// </summary>
            internal sealed class Range(TExpressionSyntax lowerBound, TExpressionSyntax higherBound) : AnalyzedPattern
            {
                public readonly TExpressionSyntax LowerBound = lowerBound;
                public readonly TExpressionSyntax HigherBound = higherBound;
            }

            /// <summary>
            /// Represents an and-pattern, constructed from two other patterns.
            /// </summary>
            internal sealed class And(AnalyzedPattern leftPattern, AnalyzedPattern rightPattern) : AnalyzedPattern
            {
                public readonly AnalyzedPattern LeftPattern = leftPattern;
                public readonly AnalyzedPattern RightPattern = rightPattern;
            }
        }
    }
}
