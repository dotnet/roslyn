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
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider
    {
        // Match the following pattern which can be safely converted to switch statement
        //
        //    <if-statement-sequence>
        //        : if (<condition-expr>) { <unreachable-end-point> } <if-statement-sequence>
        //        | <if-statement>

        //    <if-statement>
        //        : if (<condition-expr>) { _ } else <if-statement>
        //        | if (<condition-expr>) { _ } else { _ }
        //        | if (<condition-expr>) { _ }
        //        | { <if-statement-sequence> }

        //    <pattern-expr>
        //        : <pattern-expr> && <expr>                         // C#
        //        | <expr0> is <pattern>                             // C#
        //        | <expr0> is <type>                                // C#
        //        | <expr0> == <const-expr>                          // C#, VB
        //        | <expr0> <comparison-op> <const>                  //     VB
        //        | ( <expr0> >= <const> | <const> <= <expr0> )
        //           && ( <expr0> <= <const> | <const> >= <expr0> )  //     VB
        //        | ...
        //
        //    <condition-expr>
        //        : <condition-expr> || <pattern-expr>
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

            public (ImmutableArray<SwitchSection>, SyntaxNode) AnalyzeIfStatementSequence(ReadOnlySpan<IOperation> operations, out IOperation? defaultBodyOpt)
            {
                var sections = ArrayBuilder<SwitchSection>.GetInstance();
                if (!ParseIfStatementSequence(operations, sections, out defaultBodyOpt))
                {
                    sections.Free();
                    return default;
                }

                Debug.Assert(_switchTargetExpression is object);
                return (sections.ToImmutableAndFree(), _switchTargetExpression);
            }

            private bool ParseIfStatementSequence(ReadOnlySpan<IOperation> operations, ArrayBuilder<SwitchSection> sections, out IOperation? defaultBodyOpt)
            {
                if (operations.Length > 1 &&
                    operations[0] is IConditionalOperation {WhenFalse: null} op &&
                    HasUnreachableEndPoint(op.WhenTrue))
                {
                    if (!ParseIfStatement(op, sections, out defaultBodyOpt))
                    {
                        return false;
                    }

                    _ = ParseIfStatementSequence(operations.Slice(1), sections, out defaultBodyOpt);
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
                if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } op)
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
                return (IsConstant(op.LeftOperand), IsConstant(op.RightOperand)) switch
                {
                    (true, false) when SetInitialOrIsEquivalentToTargetExpression(op.RightOperand) => ConstantResult.Left,
                    (false, true) when SetInitialOrIsEquivalentToTargetExpression(op.LeftOperand) => ConstantResult.Right,
                    _ => ConstantResult.None,
                };
            }

            private Pattern? ParsePattern(IOperation operation, ArrayBuilder<SyntaxNode> guards)
            {
                switch (operation)
                {
                    case IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } op when SupportsRangePattern && AnalyzeRangeCheck(op) is var (lower, higher, e):
                        if (SetInitialOrIsEquivalentToTargetExpression(e))
                        {
                            return new RangePattern(lower, higher);
                        }
                        break;

                    case IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } op when SupportsCaseGuard:
                        guards.Add(op.RightOperand.Syntax);
                        return ParsePattern(op.LeftOperand, guards);

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

                    case IIsTypeOperation op when SupportsTypePattern && SetInitialOrIsEquivalentToTargetExpression(op.ValueOperand):
                        return new TypePattern(op);

                    case IIsPatternOperation op when SupportsSourcePattern && SetInitialOrIsEquivalentToTargetExpression(op.Value):
                        return new SourcePattern(op.Pattern);

                    case IParenthesizedOperation op:
                        return ParsePattern(op.Operand, guards);
                }

                return null;
            }

            private enum RangeBoundKind
            {
                None,
                Lower,
                Higher,
            }

            private (IOperation Lower, IOperation Higher, IOperation Expression)? AnalyzeRangeCheck(IBinaryOperation op)
            {
                if (!(op is {LeftOperand: IBinaryOperation leftOperand, RightOperand: IBinaryOperation rightOperand}))
                {
                    return null;
                }

                
                var left = AnalyzeRangeCheckOperand(leftOperand);
                var right = AnalyzeRangeCheckOperand(rightOperand);
                if (!AreEquivalent(left.Expression, right.Expression))
                {
                    return null;
                }

                return (left.Kind, right.Kind) switch
                {
                    (RangeBoundKind.Lower, RangeBoundKind.Higher) => (left.Constant, right.Constant, left.Expression),
                    (RangeBoundKind.Higher, RangeBoundKind.Lower) => (right.Constant, left.Constant, left.Expression),
                    _ => ((IOperation, IOperation, IOperation)?)null
                };

                bool AreEquivalent(IOperation left, IOperation right)
                {
                    return left is object && right is object && _syntaxFacts.AreEquivalent(left.Syntax, right.Syntax);
                }
            }

            private static (RangeBoundKind Kind, IOperation Expression, IOperation Constant) AnalyzeRangeCheckOperand(IBinaryOperation op)
            {
                return op.OperatorKind switch
                {
                    BinaryOperatorKind.LessThanOrEqual when IsConstant(op.LeftOperand) => (RangeBoundKind.Lower, op.RightOperand, op.LeftOperand),      // 5 <= i
                    BinaryOperatorKind.LessThanOrEqual when IsConstant(op.RightOperand) => (RangeBoundKind.Higher, op.LeftOperand, op.RightOperand),    // i <= 5
                    BinaryOperatorKind.GreaterThanOrEqual when IsConstant(op.LeftOperand) => (RangeBoundKind.Higher, op.RightOperand, op.LeftOperand),  // 5 >= i
                    BinaryOperatorKind.GreaterThanOrEqual when IsConstant(op.RightOperand) => (RangeBoundKind.Lower, op.LeftOperand, op.RightOperand),  // i >= 5
                    _ => default
                };
            }

            private static BinaryOperatorKind Negate(BinaryOperatorKind operatorKind)
            {
                return operatorKind switch
                {
                    BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan,
                    BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual,
                    BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual,
                    BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan,
                    BinaryOperatorKind.NotEquals => BinaryOperatorKind.NotEquals,
                    var v => throw ExceptionUtilities.UnexpectedValue(v)
                };
            }

            private static bool IsComparisonOperator(BinaryOperatorKind operatorKind)
            {
                switch (operatorKind)
                {
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.NotEquals:
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

            public virtual bool HasUnreachableEndPoint(IOperation operation)
            {
                return !operation.SemanticModel.AnalyzeControlFlow(operation.Syntax).EndPointIsReachable;
            }

            private bool SetInitialOrIsEquivalentToTargetExpression(IOperation operation)
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

                var ifStatement = await context.TryGetRelevantNodeAsync<TIfStatementSyntax>();
                if (ifStatement == null || ifStatement.ContainsDiagnostics)
                {
                    return;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var ifOperation = semanticModel.GetOperation(ifStatement);
                if (!(ifOperation is IConditionalOperation {Parent: IBlockOperation {Operations: var operations}}))
                {
                    return;
                }

                var index = operations.IndexOf(ifOperation);
                if (index == -1)
                {
                    return;
                }

                var (sections, target) = AnalyzeIfStatementSequence(operations.AsSpan().Slice(index), out var defaultBodyOpt);
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
                var labelCount = sections.Sum(section => section.Labels.Length) + (defaultBodyOpt is object ? 1 : 0);
                if (labelCount < 2)
                {
                    return;
                }

                context.RegisterRefactoring(new MyCodeAction(Title,
                    _ => UpdateDocumentAsync(root, document, target, ifStatement, sections, defaultBodyOpt)));
            }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                SyntaxNode target,
                SyntaxNode ifStatement,
                ImmutableArray<SwitchSection> sections,
                IOperation? defaultBodyOpt)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var sectionList = sections
                    .Select(section => generator.SwitchSectionFromLabels(
                        labels: section.Labels.Select(AsSwitchLabelSyntax),
                        statements: AsSwitchSectionStatements(section.Body)))
                    .ToList();

                if (defaultBodyOpt is object)
                {
                    sectionList.Add(generator.DefaultSwitchSection(AsSwitchSectionStatements(defaultBodyOpt)));
                }

                var ifSpan = ifStatement.Span;
                var @switch = CreateSwitchStatement(ifStatement, target, sectionList);

                foreach (var section in sections.AsSpan().Slice(1))
                {
                    if (section.IfStatementSyntax.Parent != ifStatement.Parent)
                    {
                        break;
                    }

                    root = root.RemoveNode(section.IfStatementSyntax, SyntaxRemoveOptions.KeepNoTrivia);
                }

                var lastNode = sections.LastOrDefault()?.IfStatementSyntax ?? ifStatement;
                @switch = @switch.WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(lastNode.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);

                root = root.ReplaceNode(root.FindNode(ifSpan), @switch);
                return Task.FromResult(document.WithSyntaxRoot(root));
            }

            public abstract SyntaxNode CreateSwitchStatement(SyntaxNode ifStatement, SyntaxNode expression, IEnumerable<SyntaxNode> sectionList);
            public abstract IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation);
            public abstract SyntaxNode AsSwitchLabelSyntax(SwitchLabel label);

            public abstract bool CanConvert(IConditionalOperation operation);

            public abstract string Title { get; }

            public abstract bool SupportsRelationalPattern { get; }
            public abstract bool SupportsSourcePattern { get; }
            public abstract bool SupportsRangePattern { get; }
            public abstract bool SupportsTypePattern { get; }
            public abstract bool SupportsCaseGuard { get; }
        }
    }
}
