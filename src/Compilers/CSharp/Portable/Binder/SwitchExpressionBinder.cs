﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SwitchExpressionBinder : Binder
    {
        private readonly SwitchExpressionSyntax SwitchExpressionSyntax;

        private BoundExpression _inputExpression;
        private BindingDiagnosticBag _inputExpressionDiagnostics;

        internal SwitchExpressionBinder(SwitchExpressionSyntax switchExpressionSyntax, Binder next)
            : base(next)
        {
            SwitchExpressionSyntax = switchExpressionSyntax;
        }

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node == SwitchExpressionSyntax);

            // Bind switch expression and set the switch governing type.
            var boundInputExpression = InputExpression;
            diagnostics.AddRange(InputExpressionDiagnostics, allowMismatchInDependencyAccumulation: true);
            ImmutableArray<BoundSwitchExpressionArm> switchArms = BindSwitchExpressionArms(node, originalBinder, diagnostics);
            TypeSymbol naturalType = InferResultType(switchArms, diagnostics);
            bool reportedNotExhaustive = CheckSwitchExpressionExhaustive(node, boundInputExpression, switchArms, out BoundDecisionDag decisionDag, out LabelSymbol defaultLabel, diagnostics);

            // When the input is constant, we use that to reshape the decision dag that is returned
            // so that flow analysis will see that some of the cases may be unreachable.
            decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(boundInputExpression);

            return new BoundUnconvertedSwitchExpression(
                node, boundInputExpression, switchArms, decisionDag,
                defaultLabel: defaultLabel, reportedNotExhaustive: reportedNotExhaustive, type: naturalType);
        }

        /// <summary>
        /// Build the decision dag, giving an error if some cases are subsumed and a warning if the switch expression is not exhaustive.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="boundInputExpression"></param>
        /// <param name="switchArms"></param>
        /// <param name="decisionDag"></param>
        /// <param name="diagnostics"></param>
        /// <returns>true if there was a non-exhaustive warning reported</returns>
        private bool CheckSwitchExpressionExhaustive(
            SwitchExpressionSyntax node,
            BoundExpression boundInputExpression,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            out BoundDecisionDag decisionDag,
            out LabelSymbol defaultLabel,
            BindingDiagnosticBag diagnostics)
        {
            defaultLabel = new GeneratedLabelSymbol("default");
            decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchExpression(this.Compilation, node, boundInputExpression, switchArms, defaultLabel, diagnostics);
            var reachableLabels = decisionDag.ReachableLabels;
            bool hasErrors = false;
            foreach (BoundSwitchExpressionArm arm in switchArms)
            {
                hasErrors |= arm.HasErrors;
                if (!hasErrors && !reachableLabels.Contains(arm.Label))
                {
                    diagnostics.Add(ErrorCode.ERR_SwitchArmSubsumed, arm.Pattern.Syntax.Location);
                }
            }

            if (!reachableLabels.Contains(defaultLabel))
            {
                // switch expression is exhaustive; no default label needed.
                defaultLabel = null;
                return false;
            }

            if (hasErrors)
                return true;

            // We only report exhaustive warnings when the default label is reachable through some series of
            // tests that do not include a test in which the value is known to be null.  Handling paths with
            // nulls is the job of the nullable walker.
            bool wasAcyclic = TopologicalSort.TryIterativeSort<BoundDecisionDagNode>(new[] { decisionDag.RootNode }, nonNullSuccessors, out var nodes);
            // Since decisionDag.RootNode is acyclic by construction, its subset of nodes sorted here cannot be cyclic
            Debug.Assert(wasAcyclic);
            foreach (var n in nodes)
            {
                if (n is BoundLeafDecisionDagNode leaf && leaf.Label == defaultLabel)
                {
                    var samplePattern = PatternExplainer.SamplePatternForPathToDagNode(
                        BoundDagTemp.ForOriginalInput(boundInputExpression), nodes, n, nullPaths: false, out bool requiresFalseWhenClause, out bool unnamedEnumValue);
                    ErrorCode warningCode =
                        requiresFalseWhenClause ? ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen :
                        unnamedEnumValue ? ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue :
                        ErrorCode.WRN_SwitchExpressionNotExhaustive;
                    diagnostics.Add(
                        warningCode,
                        node.SwitchKeyword.GetLocation(),
                        samplePattern);
                    return true;
                }
            }

            return false;

            ImmutableArray<BoundDecisionDagNode> nonNullSuccessors(BoundDecisionDagNode n)
            {
                switch (n)
                {
                    case BoundTestDecisionDagNode p:
                        switch (p.Test)
                        {
                            case BoundDagNonNullTest t: // checks that the input is not null
                                return ImmutableArray.Create(p.WhenTrue);
                            case BoundDagExplicitNullTest t: // checks that the input is null
                                return ImmutableArray.Create(p.WhenFalse);
                            default:
                                return BoundDecisionDag.Successors(n);
                        }
                    default:
                        return BoundDecisionDag.Successors(n);
                }
            }
        }

        /// <summary>
        /// Infer the result type of the switch expression by looking for a common type
        /// to which every arm's expression can be converted.
        /// </summary>
        private TypeSymbol InferResultType(ImmutableArray<BoundSwitchExpressionArm> switchCases, BindingDiagnosticBag diagnostics)
        {
            var seenTypes = Symbols.SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<TypeSymbol>();
            var typesInOrder = ArrayBuilder<TypeSymbol>.GetInstance();
            foreach (var @case in switchCases)
            {
                var type = @case.Value.Type;
                if (type is object && seenTypes.Add(type))
                {
                    typesInOrder.Add(type);
                }
            }

            seenTypes.Free();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var commonType = BestTypeInferrer.GetBestType(typesInOrder, Conversions, ref useSiteInfo);
            typesInOrder.Free();

            // We've found a candidate common type among those arms that have a type.  Also check that every arm's
            // expression (even those without a type) can be converted to that type.
            if (commonType is object)
            {
                foreach (var @case in switchCases)
                {
                    if (!this.Conversions.ClassifyImplicitConversionFromExpression(@case.Value, commonType, ref useSiteInfo).Exists)
                    {
                        commonType = null;
                        break;
                    }
                }
            }

            diagnostics.Add(SwitchExpressionSyntax, useSiteInfo);
            return commonType;
        }

        private ImmutableArray<BoundSwitchExpressionArm> BindSwitchExpressionArms(SwitchExpressionSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = InputExpression.HasErrors;
            var builder = ArrayBuilder<BoundSwitchExpressionArm>.GetInstance();
            foreach (var arm in node.Arms)
            {
                var armBinder = originalBinder.GetBinder(arm);
                var boundArm = armBinder.BindSwitchExpressionArm(arm, diagnostics);
                builder.Add(boundArm);
            }

            return builder.ToImmutableAndFree();
        }

        internal BoundExpression InputExpression
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                Debug.Assert(_inputExpression != null);
                return _inputExpression;
            }
        }

        internal TypeSymbol SwitchGoverningType => InputExpression.Type;

        internal uint SwitchGoverningValEscape => GetValEscape(InputExpression, LocalScopeDepth);

        protected BindingDiagnosticBag InputExpressionDiagnostics
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                Debug.Assert(_inputExpressionDiagnostics != null);
                return _inputExpressionDiagnostics;
            }
        }

        private void EnsureSwitchGoverningExpressionAndDiagnosticsBound()
        {
            if (_inputExpression == null)
            {
                var switchGoverningDiagnostics = new BindingDiagnosticBag();
                var boundSwitchGoverningExpression = BindSwitchGoverningExpression(switchGoverningDiagnostics);
                _inputExpressionDiagnostics = switchGoverningDiagnostics;
                Interlocked.CompareExchange(ref _inputExpression, boundSwitchGoverningExpression, null);
            }
        }

        private BoundExpression BindSwitchGoverningExpression(BindingDiagnosticBag diagnostics)
        {
            var switchGoverningExpression = BindRValueWithoutTargetType(SwitchExpressionSyntax.GoverningExpression, diagnostics);
            if (switchGoverningExpression.Type == (object)null || switchGoverningExpression.Type.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_BadPatternExpression, SwitchExpressionSyntax.GoverningExpression.Location, switchGoverningExpression.Display);
                switchGoverningExpression = this.GenerateConversionForAssignment(CreateErrorType(), switchGoverningExpression, diagnostics);
            }

            return switchGoverningExpression;
        }
    }
}
