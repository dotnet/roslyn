// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<TIfStatementSyntax>
    {
        public abstract class Pattern
        {
        }

        public sealed class SwitchLabel
        {
            public readonly Pattern Pattern;
            public readonly ImmutableArray<SyntaxNode> Guards;

            public SwitchLabel(Pattern pattern, ImmutableArray<SyntaxNode> guards)
            {
                Pattern = pattern;
                Guards = guards;
            }
        }

        public sealed class SwitchSection
        {
            public readonly ImmutableArray<SwitchLabel> Labels;
            public readonly IOperation Body;
            public readonly SyntaxNode IfStatementSyntax;

            public SwitchSection(ImmutableArray<SwitchLabel> labels, IOperation body, SyntaxNode syntax)
            {
                Labels = labels;
                Body = body;
                IfStatementSyntax = syntax;
            }
        }

        public sealed class TypePattern : Pattern
        {
            public readonly SyntaxNode IsExpressionSyntax;

            public TypePattern(IIsTypeOperation operation)
            {
                IsExpressionSyntax = operation.Syntax;
            }
        }

        public sealed class SourcePattern : Pattern
        {
            public readonly SyntaxNode PatternSyntax;

            public SourcePattern(IPatternOperation operation)
            {
                PatternSyntax = operation.Syntax;
            }
        }

        public sealed class ConstantPattern : Pattern
        {
            public readonly SyntaxNode ExpressionSyntax;

            public ConstantPattern(IOperation operation)
            {
                ExpressionSyntax = operation.Syntax;
            }
        }

        public sealed class RelationalPattern : Pattern
        {
            public readonly BinaryOperatorKind OperatorKind;
            public readonly SyntaxNode Value;

            public RelationalPattern(BinaryOperatorKind operatorKind, IOperation value)
            {
                OperatorKind = operatorKind;
                Value = value.Syntax;
            }
        }

        public sealed class RangePattern : Pattern
        {
            public readonly SyntaxNode LowerBound;
            public readonly SyntaxNode HigherBound;

            public RangePattern(IOperation lowerBound, IOperation higherBound)
            {
                LowerBound = lowerBound.Syntax;
                HigherBound = higherBound.Syntax;
            }
        }
    }
}
