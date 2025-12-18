// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch;

using static BinaryOperatorKind;

internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
    TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax>
{
    // Match the following pattern which can be safely converted to switch statement
    //
    //    <if-statement-sequence>
    //        : if (<section-expr>) { <unreachable-end-point> }, <if-statement-sequence>
    //        : if (<section-expr>) { <unreachable-end-point> }, ( return | throw )
    //        | <if-statement>
    //
    //    <if-statement>
    //        : if (<section-expr>) { _ } else <if-statement>
    //        | if (<section-expr>) { _ } else { <if-statement-sequence> }
    //        | if (<section-expr>) { _ } else { _ }
    //        | if (<section-expr>) { _ }
    //
    //    <section-expr>
    //        : <section-expr> || <pattern-expr>
    //        | <pattern-expr>
    //
    //    <pattern-expr>
    //        : <pattern-expr> && <expr>                         // C#
    //        | <expr0> is <pattern>                             // C#
    //        | <expr0> is <type>                                // C#
    //        | <expr0> == <const-expr>                          // C#, VB
    //        | <expr0> <comparison-op> <const>                  // C#, VB
    //        | ( <expr0> >= <const> | <const> <= <expr0> )
    //           && ( <expr0> <= <const> | <const> >= <expr0> )  // C#, VB
    //        | ( <expr0> <= <const> | <const> >= <expr0> )
    //           && ( <expr0> >= <const> | <const> <= <expr0> )  // C#, VB
    //
    internal abstract class Analyzer(ISyntaxFacts syntaxFacts, AbstractConvertIfToSwitchCodeRefactoringProvider<TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax>.Feature features)
    {
        public abstract bool CanConvert(IConditionalOperation operation);
        public abstract bool HasUnreachableEndPoint(IOperation operation);
        public abstract bool CanImplicitlyConvert(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol targetType);

        /// <summary>
        /// Holds the expression determined to be used as the target expression of the switch
        /// </summary>
        /// <remarks>
        /// Note that this is initially unset until we find a non-constant expression.
        /// </remarks>
        private TExpressionSyntax _switchTargetExpression = null!;

        /// <summary>
        /// Holds the type of the <see cref="_switchTargetExpression"/>
        /// </summary>
        private ITypeSymbol? _switchTargetType = null;
        private readonly ISyntaxFacts _syntaxFacts = syntaxFacts;

        public Feature Features { get; } = features;

        public bool Supports(Feature feature)
            => (Features & feature) != 0;

        public (ImmutableArray<AnalyzedSwitchSection>, TExpressionSyntax TargetExpression) AnalyzeIfStatementSequence(ReadOnlySpan<IOperation> operations)
        {
            using var _ = ArrayBuilder<AnalyzedSwitchSection>.GetInstance(out var sections);
            if (!ParseIfStatementSequence(operations, sections, topLevel: true, out var defaultBodyOpt))
            {
                return default;
            }

            if (defaultBodyOpt is object)
            {
                sections.Add(new AnalyzedSwitchSection(labels: default, defaultBodyOpt, defaultBodyOpt.Syntax));
            }

            RoslynDebug.Assert(_switchTargetExpression is object);
            return (sections.ToImmutable(), _switchTargetExpression);
        }

        // Tree to parse:
        //
        //    <if-statement-sequence>
        //        : if (<section-expr>) { <unreachable-end-point> }, <if-statement-sequence>
        //        : if (<section-expr>) { <unreachable-end-point> }, ( return | throw )
        //        | <if-statement>
        //
        private bool ParseIfStatementSequence(
            ReadOnlySpan<IOperation> operations,
            ArrayBuilder<AnalyzedSwitchSection> sections,
            bool topLevel,
            out IOperation? defaultBodyOpt)
        {
            var current = 0;
            while (current < operations.Length &&
                operations[current] is IConditionalOperation { WhenFalse: null } op &&
                HasUnreachableEndPoint(op.WhenTrue) &&
                ParseIfStatement(op, sections, out _))
            {
                current++;
            }

            defaultBodyOpt = null;
            if (current == 0)
            {
                // didn't consume a sequence of if-statements with unreachable ends.  Check for the last case.
                if (operations.Length == 0)
                    return false;

                // If we're in the initial state, it's fine for there to be many operations that follow.  We're just
                // trying to check if the first one completes our analysis (and we'll not touch the ones that
                // follow). However, if we're actually in one of the recursive calls, it's *not* ok to ignore the 
                // following ops as those may impact if the higher call into us is ok.  So in that case, we do not
                // allow the parsing to succeed if we have more than one operation left.
                return topLevel
                    ? operations is [var op1, ..] && ParseIfStatement(op1, sections, out defaultBodyOpt)
                    : operations is [var op2] && ParseIfStatement(op2, sections, out defaultBodyOpt);
            }
            else
            {
                if (current < operations.Length)
                {
                    // consumed a sequence of if-statements with unreachable-ends.  If we end with a normal
                    // if-statement, we're done.  Otherwise, we end with whatever last return/throw we see.
                    var nextStatement = operations[current];
                    if (!ParseIfStatement(nextStatement, sections, out defaultBodyOpt) &&
                        nextStatement is IReturnOperation { ReturnedValue: not null } or IThrowOperation { Exception: not null })
                    {
                        defaultBodyOpt = nextStatement;
                    }
                }

                return true;
            }
        }

        // Tree to parse:
        //
        //    <if-statement>
        //        : if (<section-expr>) { _ } else <if-statement>
        //        | if (<section-expr>) { _ } else { <if-statement-sequence> }
        //        | if (<section-expr>) { _ } else { _ }
        //        | if (<section-expr>) { _ }
        //
        private bool ParseIfStatement(IOperation operation, ArrayBuilder<AnalyzedSwitchSection> sections, out IOperation? defaultBodyOpt)
        {
            switch (operation)
            {
                case IConditionalOperation op when CanConvert(op):
                    var section = ParseSwitchSection(op);
                    if (section is null)
                    {
                        break;
                    }

                    sections.Add(section);

                    if (op.WhenFalse is null)
                    {
                        defaultBodyOpt = null;
                    }
                    else if (!ParseIfStatementOrBlock(op.WhenFalse, sections, out defaultBodyOpt))
                    {
                        defaultBodyOpt = op.WhenFalse;
                    }

                    return true;
            }

            defaultBodyOpt = null;
            return false;
        }

        private bool ParseIfStatementOrBlock(IOperation op, ArrayBuilder<AnalyzedSwitchSection> sections, out IOperation? defaultBodyOpt)
        {
            return op is IBlockOperation block
                ? ParseIfStatementSequence(block.Operations.AsSpan(), sections, topLevel: false, out defaultBodyOpt)
                : ParseIfStatement(op, sections, out defaultBodyOpt);
        }

        private AnalyzedSwitchSection? ParseSwitchSection(IConditionalOperation operation)
        {
            using var _ = ArrayBuilder<AnalyzedSwitchLabel>.GetInstance(out var labels);
            if (!ParseSwitchLabels(operation.Condition, labels))
            {
                return null;
            }

            return new AnalyzedSwitchSection(labels.ToImmutable(), operation.WhenTrue, operation.Syntax);
        }

        // Tree to parse:
        //
        //    <section-expr>
        //        : <section-expr> || <pattern-expr>
        //        | <pattern-expr>
        //
        private bool ParseSwitchLabels(IOperation operation, ArrayBuilder<AnalyzedSwitchLabel> labels)
        {
            if (operation is IBinaryOperation { OperatorKind: ConditionalOr } op)
            {
                if (!ParseSwitchLabels(op.LeftOperand, labels))
                {
                    return false;
                }

                operation = op.RightOperand;
            }

            var label = ParseSwitchLabel(operation);
            if (label is null)
            {
                return false;
            }

            labels.Add(label);
            return true;
        }

        private AnalyzedSwitchLabel? ParseSwitchLabel(IOperation operation)
        {
            using var _ = ArrayBuilder<TExpressionSyntax>.GetInstance(out var guards);
            var pattern = ParsePattern(operation, guards);
            if (pattern is null)
                return null;

            return new AnalyzedSwitchLabel(pattern, guards.ToImmutable());
        }

        private enum ConstantResult
        {
            /// <summary>
            /// None of operands were constant.
            /// </summary>
            None,
            /// <summary>
            /// Signifies that the left operand is the constant.
            /// </summary>
            Left,
            /// <summary>
            /// Signifies that the right operand is the constant.
            /// </summary>
            Right,
        }

        private ConstantResult DetermineConstant(IBinaryOperation op)
        {
            return (op.LeftOperand, op.RightOperand) switch
            {
                var (e, v) when IsConstant(v) && CheckTargetExpression(e, out var switchTargetType) && CheckConstantType(v, switchTargetType) => ConstantResult.Right,
                var (v, e) when IsConstant(v) && CheckTargetExpression(e, out var switchTargetType) && CheckConstantType(v, switchTargetType) => ConstantResult.Left,
                _ => ConstantResult.None,
            };
        }

        // Tree to parse:
        //
        //    <pattern-expr>
        //        : <pattern-expr> && <expr>                         // C#
        //        | <expr0> is <pattern>                             // C#
        //        | <expr0> is <type>                                // C#
        //        | <expr0> == <const-expr>                          // C#, VB
        //        | <expr0> <comparison-op> <const>                  //     VB
        //        | ( <expr0> >= <const> | <const> <= <expr0> )
        //           && ( <expr0> <= <const> | <const> >= <expr0> )  //     VB
        //        | ( <expr0> <= <const> | <const> >= <expr0> )
        //           && ( <expr0> >= <const> | <const> <= <expr0> )  //     VB
        //
        private AnalyzedPattern? ParsePattern(IOperation operation, ArrayBuilder<TExpressionSyntax> guards)
        {
            switch (operation)
            {
                case IBinaryOperation { OperatorKind: ConditionalAnd } op
                    when Supports(Feature.RangePattern) && GetRangeBounds(op) is (TExpressionSyntax lower, TExpressionSyntax higher):
                    return new AnalyzedPattern.Range(lower, higher);

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals } op:
                    return DetermineConstant(op) switch
                    {
                        ConstantResult.Left when op.LeftOperand.Syntax is TExpressionSyntax left
                            => new AnalyzedPattern.Constant(left),
                        ConstantResult.Right when op.RightOperand.Syntax is TExpressionSyntax right
                            => new AnalyzedPattern.Constant(right),
                        _ => null
                    };

                case IBinaryOperation { OperatorKind: NotEquals } op
                    when Supports(Feature.InequalityPattern):
                    return ParseRelationalPattern(op);

                case IBinaryOperation op
                    when Supports(Feature.RelationalPattern) && IsRelationalOperator(op.OperatorKind):
                    return ParseRelationalPattern(op);

                // Check this below the cases that produce Relational/Ranges.  We would prefer to use those if
                // available before utilizing a CaseGuard.
                case IBinaryOperation { OperatorKind: ConditionalAnd } op
                    when Supports(Feature.AndPattern | Feature.CaseGuard):
                    {
                        var leftPattern = ParsePattern(op.LeftOperand, guards);
                        if (leftPattern == null)
                            return null;

                        if (Supports(Feature.AndPattern))
                        {
                            var guardCount = guards.Count;
                            var rightPattern = ParsePattern(op.RightOperand, guards);
                            if (rightPattern != null)
                                return new AnalyzedPattern.And(leftPattern, rightPattern);

                            // Making a pattern out of the RHS didn't work.  Reset the guards back to where we started.
                            guards.Count = guardCount;
                        }

                        if (Supports(Feature.CaseGuard) && op.RightOperand.Syntax is TExpressionSyntax node)
                        {
                            guards.Add(node);
                            return leftPattern;
                        }

                        return null;
                    }

                case IIsTypeOperation op
                    when Supports(Feature.IsTypePattern) && CheckTargetExpression(op.ValueOperand, out _) && op.Syntax is TIsExpressionSyntax node:
                    return new AnalyzedPattern.Type(node);

                case IIsPatternOperation op
                    when Supports(Feature.SourcePattern) && CheckTargetExpression(op.Value, out _) && op.Pattern.Syntax is TPatternSyntax pattern:
                    return new AnalyzedPattern.Source(pattern);

                case IParenthesizedOperation op:
                    return ParsePattern(op.Operand, guards);
            }

            return null;
        }

        private AnalyzedPattern? ParseRelationalPattern(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                ConstantResult.Left when op.LeftOperand.Syntax is TExpressionSyntax left
                    => new AnalyzedPattern.Relational(Flip(op.OperatorKind), left),
                ConstantResult.Right when op.RightOperand.Syntax is TExpressionSyntax right
                    => new AnalyzedPattern.Relational(op.OperatorKind, right),
                _ => null
            };
        }

        private enum BoundKind
        {
            /// <summary>
            /// Not a range bound.
            /// </summary>
            None,
            /// <summary>
            /// Signifies that the lower-bound of a range pattern
            /// </summary>
            Lower,
            /// <summary>
            /// Signifies that the higher-bound of a range pattern
            /// </summary>
            Higher,
        }

        private (SyntaxNode Lower, SyntaxNode Higher) GetRangeBounds(IBinaryOperation op)
        {
            if (op is not
                { LeftOperand: IBinaryOperation left, RightOperand: IBinaryOperation right })
            {
                return default;
            }

            return (GetRangeBound(left), GetRangeBound(right)) switch
            {
                ({ Kind: BoundKind.Lower } low, { Kind: BoundKind.Higher } high)
                    when CheckTargetExpression(low.Expression, high.Expression) => (low.Value.Syntax, high.Value.Syntax),
                ({ Kind: BoundKind.Higher } high, { Kind: BoundKind.Lower } low)
                    when CheckTargetExpression(low.Expression, high.Expression) => (low.Value.Syntax, high.Value.Syntax),
                _ => default
            };

            bool CheckTargetExpression(IOperation left, IOperation right)
                => _syntaxFacts.AreEquivalent(left.Syntax, right.Syntax) && this.CheckTargetExpression(left, out _);
        }

        private static (BoundKind Kind, IOperation Expression, IOperation Value) GetRangeBound(IBinaryOperation op)
        {
            return op.OperatorKind switch
            {
                // 5 <= i
                LessThanOrEqual when IsConstant(op.LeftOperand) => (BoundKind.Lower, op.RightOperand, op.LeftOperand),
                // i <= 5
                LessThanOrEqual when IsConstant(op.RightOperand) => (BoundKind.Higher, op.LeftOperand, op.RightOperand),
                // 5 >= i
                GreaterThanOrEqual when IsConstant(op.LeftOperand) => (BoundKind.Higher, op.RightOperand, op.LeftOperand),
                // i >= 5
                GreaterThanOrEqual when IsConstant(op.RightOperand) => (BoundKind.Lower, op.LeftOperand, op.RightOperand),
                _ => default
            };
        }

        /// <summary>
        /// Changes the direction the operator is pointing
        /// </summary>
        private static BinaryOperatorKind Flip(BinaryOperatorKind operatorKind)
        {
            return operatorKind switch
            {
                LessThan => GreaterThan,
                LessThanOrEqual => GreaterThanOrEqual,
                GreaterThanOrEqual => LessThanOrEqual,
                GreaterThan => LessThan,
                NotEquals => NotEquals,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };
        }

        private static bool IsRelationalOperator(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case LessThan:
                case LessThanOrEqual:
                case GreaterThanOrEqual:
                case GreaterThan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsConstant(IOperation operation)
        {
            // Constants do not propagate to conversions
            return operation is IConversionOperation { Conversion.IsUserDefined: false } op
                ? IsConstant(op.Operand)
                : operation.ConstantValue.HasValue;
        }

        private bool CheckTargetExpression(IOperation operation, [NotNullWhen(true)] out ITypeSymbol? switchTargetType)
        {
            operation = operation.WalkDownConversion();

            if (operation.Syntax is not TExpressionSyntax expression)
            {
                switchTargetType = null;
                return false;
            }

            // If we have not figured the switch expression yet, we will assume that the first expression is the one.
            if (_switchTargetExpression is null)
            {
                RoslynDebug.Assert(_switchTargetType is null);

                _switchTargetExpression = expression;
                _switchTargetType = operation.Type;
            }

            switchTargetType = _switchTargetType;
            return switchTargetType != null && _syntaxFacts.AreEquivalent(expression, _switchTargetExpression);
        }

        private bool CheckConstantType(IOperation operation, ITypeSymbol switchTargetType)
        {
            RoslynDebug.AssertNotNull(operation.SemanticModel);

            return CanImplicitlyConvert(operation.SemanticModel, operation.Syntax, switchTargetType);
        }
    }

    [Flags]
    internal enum Feature
    {
        None = 0,
        // VB/C# 9.0 features
        RelationalPattern = 1,
        // VB features
        InequalityPattern = 1 << 1,
        RangePattern = 1 << 2,
        // C# 7.0 features
        SourcePattern = 1 << 3,
        IsTypePattern = 1 << 4,
        CaseGuard = 1 << 5,
        // C# 8.0 features
        SwitchExpression = 1 << 6,
        // C# 9.0 features
        OrPattern = 1 << 7,
        AndPattern = 1 << 8,
        TypePattern = 1 << 9,
    }
}
