// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        // Represents a switch-section constructed from a series of
        // if-conditions, possibly combined with logical-or operator
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

        // Represents a switch-label constructed from a series of
        // if-conditions, possibly combined by logical-and operator
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

        // Represents a case clause (pattern) constructed from various checks - see below
        internal abstract class AnalyzedPattern
        {
            private AnalyzedPattern()
            {
            }

            // Represents a type-pattern, constructed from is-expression
            internal sealed class Type : AnalyzedPattern
            {
                public readonly TIsExpressionSyntax IsExpressionSyntax;

                public Type(TIsExpressionSyntax expression)
                {
                    IsExpressionSyntax = expression;
                }
            }

            // Represents a source-pattern constructed from C# patterns
            internal sealed class Source : AnalyzedPattern
            {
                public readonly TPatternSyntax PatternSyntax;

                public Source(TPatternSyntax patternSyntax)
                {
                    PatternSyntax = patternSyntax;
                }
            }

            // Represents a constant-pattern constructed from an equality check
            internal sealed class Constant : AnalyzedPattern
            {
                public readonly TExpressionSyntax ExpressionSyntax;

                public Constant(TExpressionSyntax expression)
                {
                    ExpressionSyntax = expression;
                }
            }

            // Represents a relational-pattern constructed from comparison operators
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

            // Represents a range-pattern constructed from a couple of comparison operators
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
        }
    }
}
