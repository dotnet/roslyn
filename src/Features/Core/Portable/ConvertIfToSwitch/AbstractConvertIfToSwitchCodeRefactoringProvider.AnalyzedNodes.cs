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

        internal abstract class AnalyzedPattern
        {
            private AnalyzedPattern()
            {
            }

            internal sealed class Type : AnalyzedPattern
            {
                public readonly TIsExpressionSyntax IsExpressionSyntax;

                public Type(TIsExpressionSyntax expression)
                {
                    IsExpressionSyntax = expression;
                }
            }

            internal sealed class Source : AnalyzedPattern
            {
                public readonly TPatternSyntax PatternSyntax;

                public Source(TPatternSyntax patternSyntax)
                {
                    PatternSyntax = patternSyntax;
                }
            }

            internal sealed class Constant : AnalyzedPattern
            {
                public readonly TExpressionSyntax ExpressionSyntax;

                public Constant(TExpressionSyntax expression)
                {
                    ExpressionSyntax = expression;
                }
            }

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
