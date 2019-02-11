﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private DiagnosticBag _inputExpressionDiagnostics;

        internal SwitchExpressionBinder(SwitchExpressionSyntax switchExpressionSyntax, Binder next)
            : base(next)
        {
            SwitchExpressionSyntax = switchExpressionSyntax;
        }

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            Debug.Assert(node == SwitchExpressionSyntax);

            // Bind switch expression and set the switch governing type.
            var boundInputExpression = InputExpression;
            diagnostics.AddRange(InputExpressionDiagnostics);
            ImmutableArray<BoundSwitchExpressionArm> switchArms = BindSwitchExpressionArms(node, originalBinder, diagnostics);
            TypeSymbol resultType = InferResultType(switchArms, diagnostics);
            switchArms = AddConversionsToArms(switchArms, resultType, diagnostics);
            bool reportedNonexhaustive = CheckSwitchExpressionExhaustive(node, boundInputExpression, switchArms, out BoundDecisionDag decisionDag, out LabelSymbol defaultLabel, diagnostics);

            // When the input is constant, we use that to reshape the decision dag that is returned
            // so that flow analysis will see that some of the cases may be unreachable.
            decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(boundInputExpression);
            return new BoundSwitchExpression(node, boundInputExpression, switchArms, decisionDag, defaultLabel, reportedNonexhaustive, resultType);
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
            DiagnosticBag diagnostics)
        {
            defaultLabel = new GeneratedLabelSymbol("default");
            decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchExpression(this.Compilation, node, boundInputExpression, switchArms, defaultLabel, diagnostics);
            var reachableLabels = decisionDag.ReachableLabels;
            foreach (BoundSwitchExpressionArm arm in switchArms)
            {
                if (!reachableLabels.Contains(arm.Label))
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

            // We only report exhaustive warnings when the default label is reachable through some series of
            // tests that do not include a test in which the value is know to be null.  Handling paths with
            // nulls is the job of the nullable walker.
            foreach (var n in TopologicalSort.IterativeSort<BoundDecisionDagNode>(new[] { decisionDag.RootNode }, nonNullSuccessors))
            {
                if (n is BoundLeafDecisionDagNode leaf && leaf.Label == defaultLabel)
                {
                    diagnostics.Add(ErrorCode.WRN_SwitchExpressionNotExhaustive, node.SwitchKeyword.GetLocation());
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
                            case BoundDagNullTest t: // checks that the input is null
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
        /// Infer the result type of the switch expression by looking for a common type.
        /// </summary>
        private TypeSymbol InferResultType(ImmutableArray<BoundSwitchExpressionArm> switchCases, DiagnosticBag diagnostics)
        {
            var seenTypes = PooledHashSet<TypeSymbol>.GetInstance();
            var typesInOrder = ArrayBuilder<TypeSymbol>.GetInstance();
            foreach (var @case in switchCases)
            {
                var type = @case.Value.Type;
                if (!(type is null) && seenTypes.Add(type))
                {
                    typesInOrder.Add(type);
                }
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var commonType = BestTypeInferrer.GetBestType(typesInOrder, Conversions, ref useSiteDiagnostics);
            diagnostics.Add(SwitchExpressionSyntax, useSiteDiagnostics);
            if (commonType is null)
            {
                diagnostics.Add(ErrorCode.ERR_SwitchExpressionNoBestType, SwitchExpressionSyntax.Location);
                commonType = CreateErrorType();
            }

            seenTypes.Free();
            return commonType;
        }

        /// <summary>
        /// Rewrite the expressions in the switch expression cases to add a conversion to the result (common) type.
        /// </summary>
        private ImmutableArray<BoundSwitchExpressionArm> AddConversionsToArms(ImmutableArray<BoundSwitchExpressionArm> switchCases, TypeSymbol resultType, DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundSwitchExpressionArm>.GetInstance();
            foreach (var oldCase in switchCases)
            {
                var oldValue = oldCase.Value;
                var newValue = GenerateConversionForAssignment(resultType, oldValue, diagnostics);
                var newCase = (oldValue == newValue) ? oldCase :
                    new BoundSwitchExpressionArm(oldCase.Syntax, oldCase.Locals, oldCase.Pattern, oldCase.WhenClause, newValue, oldCase.Label, oldCase.HasErrors);
                builder.Add(newCase);
            }

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<BoundSwitchExpressionArm> BindSwitchExpressionArms(SwitchExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
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

        protected DiagnosticBag InputExpressionDiagnostics
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
                var switchGoverningDiagnostics = new DiagnosticBag();
                var boundSwitchGoverningExpression = BindSwitchGoverningExpression(switchGoverningDiagnostics);
                _inputExpressionDiagnostics = switchGoverningDiagnostics;
                Interlocked.CompareExchange(ref _inputExpression, boundSwitchGoverningExpression, null);
            }
        }

        private BoundExpression BindSwitchGoverningExpression(DiagnosticBag diagnostics)
        {
            var switchGoverningExpression = BindValue(SwitchExpressionSyntax.GoverningExpression, diagnostics, BindValueKind.RValue);
            if (switchGoverningExpression.Type == (object)null || switchGoverningExpression.Type.SpecialType == SpecialType.System_Void)
            {
                diagnostics.Add(ErrorCode.ERR_BadPatternExpression, SwitchExpressionSyntax.GoverningExpression.Location, switchGoverningExpression.Display);
                switchGoverningExpression = this.GenerateConversionForAssignment(CreateErrorType(), switchGoverningExpression, diagnostics);
            }

            return switchGoverningExpression;
        }
    }
}
