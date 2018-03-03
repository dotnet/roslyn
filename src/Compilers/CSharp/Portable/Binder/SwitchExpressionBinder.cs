// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected readonly SwitchExpressionSyntax SwitchExpressionSyntax;

        private BoundExpression _inputExpression;
        private DiagnosticBag _inputExpressionDiagnostics;

        internal SwitchExpressionBinder(SwitchExpressionSyntax switchExpressionSyntax, Binder next)
            : base(next)
        {
            SwitchExpressionSyntax = switchExpressionSyntax;
        }

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // Bind switch expression and set the switch governing type.
            var boundInputExpression = InputExpression;
            diagnostics.AddRange(InputExpressionDiagnostics);
            ImmutableArray<BoundSwitchExpressionArm> switchArms = BindSwitchExpressionArms(node, originalBinder, diagnostics);
            TypeSymbol resultType = InferResultType(switchArms, diagnostics);
            switchArms = AddConversionsToArms(switchArms, resultType, diagnostics);
            bool hasErrors = CheckSwitchExpressionExhaustive(node, boundInputExpression, switchArms, out BoundDecisionDag decisionDag, out LabelSymbol defaultLabel, diagnostics);
            return new BoundSwitchExpression(node, boundInputExpression, switchArms, decisionDag, defaultLabel, resultType, hasErrors);
        }

        /// <summary>
        /// Build the decision dag, warning if the switch expression is not exhaustive. Returns true if there were errors.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="boundInputExpression"></param>
        /// <param name="switchArms"></param>
        /// <param name="decisionDag"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private bool CheckSwitchExpressionExhaustive(
            SwitchExpressionSyntax node,
            BoundExpression boundInputExpression,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            out BoundDecisionDag decisionDag,
            out LabelSymbol defaultLabel,
            DiagnosticBag diagnostics)
        {
            defaultLabel = new GeneratedLabelSymbol("default");
            decisionDag = DecisionDagBuilder.CreateDecisionDag(this.Compilation, node, boundInputExpression, switchArms, defaultLabel, diagnostics);
            HashSet<LabelSymbol> reachableLabels = decisionDag.ReachableLabels;
            foreach (BoundSwitchExpressionArm arm in switchArms)
            {
                if (!reachableLabels.Contains(arm.Label))
                {
                    diagnostics.Add(ErrorCode.ERR_SwitchArmSubsumed, arm.Pattern.Syntax.Location);
                }
            }

            if (reachableLabels.Contains(defaultLabel))
            {
                // warning: switch expression is not exhaustive
                diagnostics.Add(ErrorCode.WRN_SwitchExpressionNotExhaustive, node.SwitchKeyword.GetLocation());
            }
            else
            {
                // switch expression is exhaustive; no default label needed.
                defaultLabel = null;
            }

            return decisionDag.HasErrors;
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
                if (type != null && seenTypes.Add(type))
                {
                    typesInOrder.Add(type);
                }
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var commonType = BestTypeInferrer.InferBestType(typesInOrder.ToImmutableAndFree(), Conversions, ref useSiteDiagnostics);
            diagnostics.Add(SwitchExpressionSyntax, useSiteDiagnostics);
            if (commonType == null)
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
                    new BoundSwitchExpressionArm(oldCase.Syntax, oldCase.Locals, oldCase.Pattern, oldCase.Guard, newValue, oldCase.Label, oldCase.HasErrors);
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
