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

        private BoundExpression _switchGoverningExpression;
        private DiagnosticBag _switchGoverningDiagnostics;

        internal SwitchExpressionBinder(SwitchExpressionSyntax switchExpressionSyntax, Binder next)
            : base(next)
        {
            SwitchExpressionSyntax = switchExpressionSyntax;
        }

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // Bind switch expression and set the switch governing type.
            var boundSwitchGoverningExpression = SwitchGoverningExpression;
            diagnostics.AddRange(SwitchGoverningDiagnostics);
            ImmutableArray<BoundSwitchExpressionArm> switchCases = BindSwitchExpressionCases(node, originalBinder, diagnostics);
            bool hasErrors = false;
            TypeSymbol resultType = InferResultType(switchCases, diagnostics);
            switchCases = AddConversionsToCases(switchCases, resultType, diagnostics);
            // PROTOTYPE(patterns2): check for subsumption, completeness, etc.
            //var hasErrors = CheckSwitchErrors(node, boundSwitchGoverningExpression, switchCases, decisionDag, diagnostics);
            return new BoundSwitchExpression(node, boundSwitchGoverningExpression, switchCases, resultType, hasErrors);
        }

        /// <summary>
        /// Infer the result type of the switch expression by looking for a common type.
        /// </summary>
        private TypeSymbol InferResultType(ImmutableArray<BoundSwitchExpressionArm> switchCases, DiagnosticBag diagnostics)
        {
            var seenTypes = new HashSet<TypeSymbol>();
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

            return commonType;
        }

        /// <summary>
        /// Rewrite the expressions in the switch expression cases to add a conversion to the result (common) type.
        /// </summary>
        private ImmutableArray<BoundSwitchExpressionArm> AddConversionsToCases(ImmutableArray<BoundSwitchExpressionArm> switchCases, TypeSymbol resultType, DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundSwitchExpressionArm>.GetInstance();
            foreach (var oldCase in switchCases)
            {
                var oldValue = oldCase.Value;
                var newValue = GenerateConversionForAssignment(resultType, oldValue, diagnostics);
                var newCase = (oldValue == newValue) ? oldCase :
                    new BoundSwitchExpressionArm(oldCase.Syntax, oldCase.Locals, oldCase.Pattern, oldCase.Guard, newValue, oldCase.HasErrors);
                builder.Add(newCase);
            }

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<BoundSwitchExpressionArm> BindSwitchExpressionCases(SwitchExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            bool hasErrors = SwitchGoverningExpression.HasErrors;
            var builder = ArrayBuilder<BoundSwitchExpressionArm>.GetInstance();
            foreach (var arm in node.Arms)
            {
                var armBinder = originalBinder.GetBinder(arm);
                var boundArm = armBinder.BindSwitchExpressionArm(arm, diagnostics);
                builder.Add(boundArm);
            }

            return builder.ToImmutableAndFree();
        }

        internal BoundExpression SwitchGoverningExpression
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                Debug.Assert(_switchGoverningExpression != null);
                return _switchGoverningExpression;
            }
        }

        internal TypeSymbol SwitchGoverningType => SwitchGoverningExpression.Type;

        protected DiagnosticBag SwitchGoverningDiagnostics
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                Debug.Assert(_switchGoverningDiagnostics != null);
                return _switchGoverningDiagnostics;
            }
        }

        private void EnsureSwitchGoverningExpressionAndDiagnosticsBound()
        {
            if (_switchGoverningExpression == null)
            {
                var switchGoverningDiagnostics = new DiagnosticBag();
                var boundSwitchGoverningExpression = BindSwitchGoverningExpression(switchGoverningDiagnostics);
                _switchGoverningDiagnostics = switchGoverningDiagnostics;
                Interlocked.CompareExchange(ref _switchGoverningExpression, boundSwitchGoverningExpression, null);
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
