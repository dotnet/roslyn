// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    using static BinaryOperatorKind;

    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider
    {
        // Match the following pattern which can be safely converted to switch statement
        //
        //    <if-statement-sequence>
        //        : if (<section-expr>) { <unreachable-end-point> }, <if-statement-sequence>
        //        : if (<section-expr>) { <unreachable-end-point> }, ( return | throw )
        //        | <if-statement>

        //    <if-statement>
        //        : if (<section-expr>) { _ } else <if-statement>
        //        | if (<section-expr>) { _ } else { _ }
        //        | if (<section-expr>) { _ }
        //        | { <if-statement-sequence> }

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
        //    <section-expr>
        //        : <section-expr> || <pattern-expr>
        //        | <pattern-expr>
        //
        internal abstract class Analyzer<TIfStatementSyntax> : IAnalyzer where TIfStatementSyntax : SyntaxNode
        {
            private SyntaxNode? _switchTargetExpression;
            private readonly ISyntaxFactsService _syntaxFacts;

            protected Analyzer(ISyntaxFactsService syntaxFacts)
            {
                _syntaxFacts = syntaxFacts;
            }

            public (ImmutableArray<SwitchSection>, SyntaxNode) AnalyzeIfStatementSequence(ReadOnlySpan<IOperation> operations)
            {
                var sections = ArrayBuilder<SwitchSection>.GetInstance();
                if (!ParseIfStatementSequence(operations, sections, out var defaultBodyOpt))
                {
                    sections.Free();
                    return default;
                }

                if (defaultBodyOpt is object)
                {
                    sections.Add(new SwitchSection(labels: default, defaultBodyOpt, defaultBodyOpt.Syntax));
                }

                Debug.Assert(_switchTargetExpression is object);
                return (sections.ToImmutableAndFree(), _switchTargetExpression);
            }

            private bool ParseIfStatementSequence(ReadOnlySpan<IOperation> operations, ArrayBuilder<SwitchSection> sections, out IOperation? defaultBodyOpt)
            {
                if (operations.Length > 1 &&
                    operations[0] is IConditionalOperation { WhenFalse: null } op &&
                    HasUnreachableEndPoint(op.WhenTrue))
                {
                    if (!ParseIfStatement(op, sections, out defaultBodyOpt))
                    {
                        return false;
                    }

                    if (!ParseIfStatementSequence(operations.Slice(1), sections, out defaultBodyOpt))
                    {
                        var nextStatement = operations[1];
                        if (nextStatement is IReturnOperation { ReturnedValue: { } } ||
                            nextStatement is IThrowOperation { Exception: { } })
                        {
                            defaultBodyOpt = nextStatement;
                        }
                    }

                    return true;
                }

                if (operations.Length > 0)
                {
                    return ParseIfStatement(operations[0], sections, out defaultBodyOpt);
                }

                defaultBodyOpt = null;
                return false;
            }

            private bool ParseIfStatement(IOperation operation, ArrayBuilder<SwitchSection> sections, out IOperation? defaultBodyOpt)
            {
                switch (operation)
                {
                    case IBlockOperation op:
                        return ParseIfStatementSequence(op.Operations.AsSpan(), sections, out defaultBodyOpt);

                    case IConditionalOperation op when CanConvert(op):
                        var section = ParseSwitchSection(op);
                        if (section is null)
                        {
                            defaultBodyOpt = null;
                            return false;
                        }

                        sections.Add(section);

                        if (op.WhenFalse is null)
                        {
                            defaultBodyOpt = null;
                        }
                        else if (!ParseIfStatement(op.WhenFalse, sections, out defaultBodyOpt))
                        {
                            defaultBodyOpt = op.WhenFalse;
                        }

                        return true;
                }

                defaultBodyOpt = null;
                return false;
            }

            private SwitchSection? ParseSwitchSection(IConditionalOperation operation)
            {
                var labels = ArrayBuilder<SwitchLabel>.GetInstance();
                if (!ParseSwitchLabels(operation.Condition, labels))
                {
                    labels.Free();
                    return null;
                }

                return new SwitchSection(labels.ToImmutableAndFree(), operation.WhenTrue, operation.Syntax);
            }

            private bool ParseSwitchLabels(IOperation operation, ArrayBuilder<SwitchLabel> labels)
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

            private SwitchLabel? ParseSwitchLabel(IOperation operation)
            {
                var guards = ArrayBuilder<SyntaxNode>.GetInstance();
                var pattern = ParsePattern(operation, guards);
                if (pattern is null)
                {
                    guards.Free();
                    return null;
                }

                return new SwitchLabel(pattern, guards.ToImmutableAndFree());
            }

            private enum ConstantResult
            {
                None,
                Left,
                Right,
            }

            private ConstantResult DetermineConstant(IBinaryOperation op)
            {
                return (op.LeftOperand, op.RightOperand) switch
                {
                    var (e, v) when IsConstant(v) && CheckTargetExpression(e) => ConstantResult.Right,
                    var (v, e) when IsConstant(v) && CheckTargetExpression(e) => ConstantResult.Left,
                    _ => ConstantResult.None,
                };
            }

            private Pattern? ParsePattern(IOperation operation, ArrayBuilder<SyntaxNode> guards)
            {
                switch (operation)
                {
                    case IBinaryOperation { OperatorKind: ConditionalAnd } op when SupportsCaseGuard:
                        guards.Add(op.RightOperand.Syntax);
                        return ParsePattern(op.LeftOperand, guards);

                    case IBinaryOperation { OperatorKind: ConditionalAnd } op when SupportsRangePattern && GetRangeBounds(op) is var (lower, higher):
                        return new RangePattern(lower, higher);

                    case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals } op:
                        return DetermineConstant(op) switch
                        {
                            ConstantResult.Left => new ConstantPattern(op.LeftOperand),
                            ConstantResult.Right => new ConstantPattern(op.RightOperand),
                            _ => null
                        };

                    case IBinaryOperation op when SupportsRelationalPattern && IsComparisonOperator(op.OperatorKind):
                        return DetermineConstant(op) switch
                        {
                            ConstantResult.Left => new RelationalPattern(Negate(op.OperatorKind), op.LeftOperand),
                            ConstantResult.Right => new RelationalPattern(op.OperatorKind, op.RightOperand),
                            _ => null
                        };

                    case IIsTypeOperation op when SupportsTypePattern && CheckTargetExpression(op.ValueOperand):
                        return new TypePattern(op);

                    case IIsPatternOperation op when SupportsSourcePattern && CheckTargetExpression(op.Value):
                        return new SourcePattern(op.Pattern);

                    case IParenthesizedOperation op:
                        return ParsePattern(op.Operand, guards);
                }

                return null;
            }

            private enum BoundKind
            {
                None,
                Lower,
                Higher,
            }

            private (IOperation Lower, IOperation Higher)? GetRangeBounds(IBinaryOperation op)
            {
                if (!(op is { LeftOperand: IBinaryOperation left, RightOperand: IBinaryOperation right }))
                {
                    return null;
                }

                return (GetRangeBound(left), GetRangeBound(right)) switch
                {
                    ({ Kind: BoundKind.Lower } low, { Kind: BoundKind.Higher } high)
                        when CheckTargetExpression(low.Expression, high.Expression) => (low.Value, high.Value),
                    ({ Kind: BoundKind.Higher } high, { Kind: BoundKind.Lower } low)
                        when CheckTargetExpression(low.Expression, high.Expression) => (low.Value, high.Value),
                    _ => ((IOperation, IOperation)?)null
                };

                bool CheckTargetExpression(IOperation left, IOperation right)
                    => _syntaxFacts.AreEquivalent(left.Syntax, right.Syntax) && this.CheckTargetExpression(left);
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

            private static BinaryOperatorKind Negate(BinaryOperatorKind operatorKind)
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

            private static bool IsComparisonOperator(BinaryOperatorKind operatorKind)
            {
                switch (operatorKind)
                {
                    case LessThan:
                    case LessThanOrEqual:
                    case GreaterThanOrEqual:
                    case GreaterThan:
                    case NotEquals:
                        return true;
                    default:
                        return false;
                }
            }

            private static bool IsConstant(IOperation operation)
            {
                // Use syntax to skip possible conversion operation
                return operation.SemanticModel.GetConstantValue(operation.Syntax).HasValue;
            }

            private bool CheckTargetExpression(IOperation operation)
            {
                if (operation is IConversionOperation { IsImplicit: false } op)
                {
                    // Unwrap explicit casts because switch will emit those anyways
                    operation = op.Operand;
                }

                var expression = operation.Syntax;
                // If we have not figured the switch expression yet,
                // we will assume that the first expression is the one.
                if (_switchTargetExpression is null)
                {
                    _switchTargetExpression = expression;
                    return true;
                }

                return _syntaxFacts.AreEquivalent(expression, _switchTargetExpression);
            }

            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var cancellationToken = context.CancellationToken;

                if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                {
                    return;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var ifStatement = await context.TryGetRelevantNodeAsync<TIfStatementSyntax>().ConfigureAwait(false);
                if (ifStatement == null || ifStatement.ContainsDiagnostics)
                {
                    return;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var ifOperation = semanticModel.GetOperation(ifStatement);
                if (!(ifOperation is IConditionalOperation { Parent: IBlockOperation { Operations: var operations } }))
                {
                    return;
                }

                var index = operations.IndexOf(ifOperation);
                if (index == -1)
                {
                    return;
                }

                var (sections, target) = AnalyzeIfStatementSequence(operations.AsSpan().Slice(index));
                if (sections.IsDefaultOrEmpty)
                {
                    return;
                }

                // To prevent noisiness we don't offer this unless we're going to generate at least
                // two switch labels.  It can be quite annoying to basically have this offered
                // on pretty much any simple 'if' like "if (a == 0)" or "if (x == null)".  In these
                // cases, the converted code just looks and feels worse, and it ends up causing the
                // lightbulb to appear too much.
                //
                // This does mean that if someone has a simple if, and is about to add a lot more
                // cases, and says to themselves "let me convert this to a switch first!", then they'll
                // be out of luck.  However, I believe the core value here is in taking existing large
                // if-chains/checks and easily converting them over to a switch.  So not offering the
                // feature on simple if-statements seems like an acceptable compromise to take to ensure
                // the overall user experience isn't degraded.
                var labelCount = sections.Sum(section => section.Labels.IsDefault ? 1 : section.Labels.Length);
                if (labelCount < 2)
                {
                    return;
                }

                context.RegisterRefactoring(
                    new MyCodeAction(Title,
                        _ => UpdateDocumentAsync(root, document, target, ifStatement, sections, convertToSwitchExpression: false)),
                    ifStatement.Span);

                if (SupportsSwitchExpression &&
                    CanConvertToSwitchExpression(sections))
                {
                    context.RegisterRefactoring(
                        new MyCodeAction("TODO",
                            _ => UpdateDocumentAsync(root, document, target, ifStatement, sections, convertToSwitchExpression: true)),
                        ifStatement.Span);
                }
            }

            private static bool CanConvertToSwitchExpression(ImmutableArray<SwitchSection> sections)
            {
                return
                    sections.Any(section => section.Labels.IsDefault) &&
                    sections.All(section => section.Labels.IsDefault || section.Labels.Length == 1) &&
                    sections.Any(section => section.Body.Kind == OperationKind.Return) &&
                    sections.All(section => CanConvertToSwitchArm(section.Body));

                static bool CanConvertToSwitchArm(IOperation op)
                {
                    switch (op)
                    {
                        case IReturnOperation { ReturnedValue: { } }:
                        case IThrowOperation { Exception: { } }:
                        case IBlockOperation { Operations: { Length: 1 } statements } when CanConvertToSwitchArm(statements[0]):
                            return true;
                    }

                    return false;
                }
            }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                SyntaxNode target,
                SyntaxNode ifStatement,
                ImmutableArray<SwitchSection> sections,
                bool convertToSwitchExpression)
            {
                var generator = SyntaxGenerator.GetGenerator(document);

                var ifSpan = ifStatement.Span;
                var lastNode = sections.LastOrDefault()?.SyntaxToRemove ?? ifStatement;

                SyntaxNode @switch;
                if (convertToSwitchExpression)
                {
                    @switch = CreateSwitchExpressionStatement(target, sections);
                }
                else
                {
                    @switch = CreateSwitchStatement(ifStatement, target, sections.Select(AsSwitchSectionSyntax))
                        .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(lastNode.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                }

                var nodesToRemove = sections.Skip(1).Select(s => s.SyntaxToRemove).Where(s => s.Parent == ifStatement.Parent);
                root = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                root = root.ReplaceNode(root.FindNode(ifSpan), @switch);
                return Task.FromResult(document.WithSyntaxRoot(root));

                SyntaxNode AsSwitchSectionSyntax(SwitchSection section)
                {
                    var statements = AsSwitchSectionStatements(section.Body);
                    return section.Labels.IsDefault
                        ? generator.DefaultSwitchSection(statements)
                        : generator.SwitchSectionFromLabels(section.Labels.Select(AsSwitchLabelSyntax), statements);
                }
            }

            public abstract SyntaxNode CreateSwitchExpressionStatement(SyntaxNode target, ImmutableArray<SwitchSection> sections);
            public abstract SyntaxNode CreateSwitchStatement(SyntaxNode ifStatement, SyntaxNode target, IEnumerable<SyntaxNode> sectionList);
            public abstract IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation);
            public abstract SyntaxNode AsSwitchLabelSyntax(SwitchLabel label);

            public abstract bool CanConvert(IConditionalOperation operation);
            public abstract bool HasUnreachableEndPoint(IOperation operation);

            public abstract string Title { get; }

            public abstract bool SupportsSwitchExpression { get; }
            public abstract bool SupportsRelationalPattern { get; }
            public abstract bool SupportsSourcePattern { get; }
            public abstract bool SupportsRangePattern { get; }
            public abstract bool SupportsTypePattern { get; }
            public abstract bool SupportsCaseGuard { get; }
        }
    }
}
