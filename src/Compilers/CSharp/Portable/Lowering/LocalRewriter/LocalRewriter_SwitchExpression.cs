// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            return SwitchExpressionLocalRewriter.Rewrite(this, node);
        }

        private sealed class SwitchExpressionLocalRewriter : BaseSwitchLocalRewriter
        {
            private SwitchExpressionLocalRewriter(BoundConvertedSwitchExpression node, LocalRewriter localRewriter, bool generateInstrumentation)
                : base(node.Syntax, localRewriter, node.SwitchArms.SelectAsArray(arm => arm.Syntax),
                      generateInstrumentation: generateInstrumentation)
            {
            }

            public static BoundExpression Rewrite(LocalRewriter localRewriter, BoundConvertedSwitchExpression node)
            {
                var generateInstrumentation = !node.WasCompilerGenerated && localRewriter.Instrument;
                // When compiling for Debug (not Release), we produce the most detailed sequence points.
                var produceDetailedSequencePoints = generateInstrumentation && localRewriter._compilation.Options.OptimizationLevel != OptimizationLevel.Release;

                if (produceDetailedSequencePoints)
                {
                    localRewriter._needsSpilling = true;
                    var rewriter = new SwitchExpressionLocalRewriter(node, localRewriter, generateInstrumentation: true);
                    BoundExpression result = rewriter.LowerSwitchExpressionForDebug(node);
                    rewriter.Free();
                    return result;
                }
                else
                {
                    var rewriter = new SwitchExpressionLocalRewriter(node, localRewriter, generateInstrumentation: false);
                    BoundExpression result = rewriter.LowerSwitchExpressionForRelease(node);
                    rewriter.Free();
                    return result;
                }
            }

            private BoundExpression LowerSwitchExpressionForDebug(BoundConvertedSwitchExpression node)
            {
                const bool produceDetailedSequencePoints = true;

                _factory.Syntax = node.Syntax;
                var result = ArrayBuilder<BoundStatement>.GetInstance();
                var outerVariables = ArrayBuilder<LocalSymbol>.GetInstance();
                var loweredSwitchGoverningExpression = _localRewriter.VisitExpression(node.Expression);

                BoundDecisionDag decisionDag = ShareTempsIfPossibleAndEvaluateInput(
                    node.GetDecisionDagForLowering(_factory.Compilation, out LabelSymbol? defaultLabel),
                    loweredSwitchGoverningExpression, expr => result.Add(_factory.ExpressionStatement(expr)), out BoundExpression savedInputExpression);

                Debug.Assert(savedInputExpression != null);

                object restorePointForEnclosingStatement = new object();
                object restorePointForSwitchBody = new object();

                // lower the decision dag.
                (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) =
                    LowerDecisionDag(decisionDag);

                if (_whenNodeIdentifierLocal is not null)
                {
                    outerVariables.Add(_whenNodeIdentifierLocal);
                }

                if (produceDetailedSequencePoints)
                {
                    var syntax = (SwitchExpressionSyntax)node.Syntax;
                    result.Add(new BoundSavePreviousSequencePoint(syntax, restorePointForEnclosingStatement));
                    // While evaluating the state machine, we highlight the `switch {...}` part.
                    var spanStart = syntax.SwitchKeyword.Span.Start;
                    var spanEnd = syntax.Span.End;
                    var spanForSwitchBody = new TextSpan(spanStart, spanEnd - spanStart);
                    result.Add(new BoundStepThroughSequencePoint(node.Syntax, span: spanForSwitchBody));
                    result.Add(new BoundSavePreviousSequencePoint(syntax, restorePointForSwitchBody));
                }

                // add the rest of the lowered dag that references that input
                result.Add(_factory.Block(loweredDag));
                // A branch to the default label when no switch case matches is included in the
                // decision tree, so the code in result is unreachable at this point.

                // Lower each switch expression arm
                LocalSymbol resultTemp = _factory.SynthesizedLocal(node.Type, node.Syntax, kind: SynthesizedLocalKind.LoweringTemp);
                LabelSymbol afterSwitchExpression = _factory.GenerateLabel("afterSwitchExpression");
                foreach (BoundSwitchExpressionArm arm in node.SwitchArms)
                {
                    _factory.Syntax = arm.Syntax;
                    var sectionBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                    sectionBuilder.AddRange(switchSections[arm.Syntax]);
                    sectionBuilder.Add(_factory.Label(arm.Label));
                    var loweredValue = _localRewriter.VisitExpression(arm.Value);
                    if (GenerateInstrumentation)
                        loweredValue = this._localRewriter.Instrumenter.InstrumentSwitchExpressionArmExpression(arm.Value, loweredValue, _factory);

                    sectionBuilder.Add(_factory.Assignment(_factory.Local(resultTemp), loweredValue));
                    sectionBuilder.Add(_factory.Goto(afterSwitchExpression));
                    var statements = sectionBuilder.ToImmutableAndFree();
                    if (arm.Locals.IsEmpty)
                    {
                        result.Add(_factory.StatementList(statements));
                    }
                    else
                    {
                        // Lifetime of these locals is expanded to the entire switch body, as it is possible to
                        // share them as temps in the decision dag.
                        outerVariables.AddRange(arm.Locals);

                        // Note the language scope of the locals, even though they are included for the purposes of
                        // lifetime analysis in the enclosing scope.
                        result.Add(new BoundScope(arm.Syntax, arm.Locals, statements));
                    }
                }

                _factory.Syntax = node.Syntax;
                if (defaultLabel is not null)
                {
                    result.Add(_factory.Label(defaultLabel));
                    if (produceDetailedSequencePoints)
                        result.Add(new BoundRestorePreviousSequencePoint(node.Syntax, restorePointForSwitchBody));
                    BoundExpression throwCall = ConstructThrowHelperCall(savedInputExpression);
                    result.Add(_factory.HiddenSequencePoint(_factory.ExpressionStatement(throwCall)));
                }

                if (GenerateInstrumentation)
                    result.Add(_factory.HiddenSequencePoint());
                result.Add(_factory.Label(afterSwitchExpression));
                if (produceDetailedSequencePoints)
                    result.Add(new BoundRestorePreviousSequencePoint(node.Syntax, restorePointForEnclosingStatement));

                outerVariables.Add(resultTemp);
                outerVariables.AddRange(_tempAllocator.AllTemps());
                return _factory.SpillSequence(outerVariables.ToImmutableAndFree(), result.ToImmutableAndFree(), _factory.Local(resultTemp));
            }

            private BoundExpression ConstructThrowHelperCall(BoundExpression savedInputExpression)
            {
                var objectType = _factory.SpecialType(SpecialType.System_Object);
                if (implicitConversionExists(savedInputExpression, objectType) &&
                    _factory.WellKnownMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject, isOptional: true) is MethodSymbol)
                {
                    return ConstructThrowSwitchExpressionExceptionHelperCall(_factory, _factory.Convert(objectType, savedInputExpression));
                }
                else if (_factory.WellKnownMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor, isOptional: true) is MethodSymbol)
                {
                    return ConstructThrowSwitchExpressionExceptionParameterlessHelperCall(_factory);
                }
                else
                {
                    return ConstructThrowInvalidOperationExceptionHelperCall(_factory);
                }

                bool implicitConversionExists(BoundExpression expression, TypeSymbol type)
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    Conversion c = _localRewriter._compilation.Conversions.ClassifyConversionFromExpression(expression, type, isChecked: false, ref discardedUseSiteInfo);
                    return c.IsImplicit;
                }
            }

            private BoundExpression LowerSwitchExpressionForRelease(BoundConvertedSwitchExpression node)
            {
                var outerVariables = ArrayBuilder<LocalSymbol>.GetInstance();
                var loweredSwitchGoverningExpression = _localRewriter.VisitExpression(node.Expression);
                var sideEffectsBuilder = ArrayBuilder<BoundExpression>.GetInstance();

                BoundDecisionDag decisionDag = ShareTempsIfPossibleAndEvaluateInput(
                    node.GetDecisionDagForLowering(_factory.Compilation, out LabelSymbol? defaultLabel),
                    loweredSwitchGoverningExpression, sideEffectsBuilder.Add, out BoundExpression savedInputExpression);

                Debug.Assert(savedInputExpression != null);

                // lower the decision dag.
                (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) =
                    LowerDecisionDag(decisionDag);

                if (_whenNodeIdentifierLocal is not null)
                {
                    outerVariables.Add(_whenNodeIdentifierLocal);
                }

                // Lower each switch expression arm
                var switchArmsBuilder = ArrayBuilder<BoundLoweredSwitchExpressionArm>.GetInstance(node.SwitchArms.Length + (defaultLabel is null ? 0 : 1));
                foreach (BoundSwitchExpressionArm arm in node.SwitchArms)
                {
                    _factory.Syntax = arm.Syntax;
                    ImmutableArray<BoundStatement> statements = switchSections[arm.Syntax];
                    BoundExpression loweredValue = _localRewriter.VisitExpression(arm.Value);

                    // Lifetime of these locals is expanded to the entire switch body, as it is possible to
                    // share them as temps in the decision dag.
                    outerVariables.AddRange(arm.Locals);
                    switchArmsBuilder.Add(new BoundLoweredSwitchExpressionArm(
                        arm.Syntax, arm.Locals, statements.Add(_factory.Label(arm.Label)), loweredValue));
                }

                _factory.Syntax = node.Syntax;
                if (defaultLabel is not null)
                {
                    BoundExpression throwCall = ConstructThrowHelperCall(savedInputExpression);
                    BoundStatement labelStatement = _factory.Label(defaultLabel);
                    switchArmsBuilder.Add(new BoundLoweredSwitchExpressionArm(
                        syntax: node.Syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        statements: ImmutableArray.Create(labelStatement),
                        value: _factory.Sequence(
                            locals: ImmutableArray<LocalSymbol>.Empty,
                            sideEffects: ImmutableArray.Create(throwCall),
                            result: _factory.Default(node.Type))));
                }

                outerVariables.AddRange(_tempAllocator.AllTemps());
                return _factory.Sequence(
                    outerVariables.ToImmutableAndFree(),
                    sideEffectsBuilder.ToImmutableAndFree(),
                    new BoundLoweredSwitchExpression(
                        syntax: node.Syntax,
                        statements: loweredDag,
                        switchArmsBuilder.ToImmutableAndFree(),
                        type: node.Type));
            }

            private static BoundExpression ConstructThrowSwitchExpressionExceptionHelperCall(SyntheticBoundNodeFactory factory, BoundExpression unmatchedValue)
            {
                Debug.Assert(factory.ModuleBuilderOpt is not null);
                var module = factory.ModuleBuilderOpt;
                var diagnosticSyntax = factory.CurrentFunction.GetNonNullSyntaxNode();
                var diagnostics = factory.Diagnostics.DiagnosticBag;
                Debug.Assert(diagnostics is not null);
                var throwSwitchExpressionExceptionMethod = module.EnsureThrowSwitchExpressionExceptionExists(diagnosticSyntax, factory, diagnostics);
                return factory.Call(
                    receiver: null,
                    throwSwitchExpressionExceptionMethod,
                    arg0: unmatchedValue);
            }

            private static BoundExpression ConstructThrowSwitchExpressionExceptionParameterlessHelperCall(SyntheticBoundNodeFactory factory)
            {
                Debug.Assert(factory.ModuleBuilderOpt is not null);
                var module = factory.ModuleBuilderOpt!;
                var diagnosticSyntax = factory.CurrentFunction.GetNonNullSyntaxNode();
                var diagnostics = factory.Diagnostics.DiagnosticBag;
                Debug.Assert(diagnostics is not null);
                var throwSwitchExpressionExceptionMethod = module.EnsureThrowSwitchExpressionExceptionParameterlessExists(diagnosticSyntax, factory, diagnostics);
                return factory.Call(
                    receiver: null,
                    throwSwitchExpressionExceptionMethod);
            }

            private static BoundExpression ConstructThrowInvalidOperationExceptionHelperCall(SyntheticBoundNodeFactory factory)
            {
                Debug.Assert(factory.ModuleBuilderOpt is not null);
                var module = factory.ModuleBuilderOpt!;
                var diagnosticSyntax = factory.CurrentFunction.GetNonNullSyntaxNode();
                var diagnostics = factory.Diagnostics.DiagnosticBag;
                Debug.Assert(diagnostics is not null);
                var throwMethod = module.EnsureThrowInvalidOperationExceptionExists(diagnosticSyntax, factory, diagnostics);
                return factory.Call(
                    receiver: null,
                    throwMethod);
            }
        }
    }
}
