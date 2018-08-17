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
    /// <summary>
    /// Binder for one of the arms of a switch expression. For example, in the one-armed switch expression
    /// "e switch { p when c => v }", this could be the binder for the arm "p when c => v".
    /// </summary>
    internal class SwitchExpressionArmBinder : Binder
    {
        private readonly SwitchExpressionArmSyntax _arm;
        private readonly ExpressionVariableBinder _armScopeBinder;
        private readonly SwitchExpressionBinder _switchExpressionBinder;

        public SwitchExpressionArmBinder(SwitchExpressionArmSyntax arm, ExpressionVariableBinder armScopeBinder, SwitchExpressionBinder switchExpressionBinder) : base(armScopeBinder)
        {
            this._arm = arm;
            this._armScopeBinder = armScopeBinder;
            this._switchExpressionBinder = switchExpressionBinder;
        }

        internal override BoundSwitchExpressionArm BindSwitchExpressionArm(SwitchExpressionArmSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node == _arm);
            Binder armBinder = this.GetBinder(node);
            bool hasErrors = _switchExpressionBinder.SwitchGoverningType.IsErrorType();
            ImmutableArray<LocalSymbol> locals = _armScopeBinder.Locals;
            BoundPattern pattern = armBinder.BindPattern(node.Pattern, _switchExpressionBinder.SwitchGoverningType, _switchExpressionBinder.SwitchGoverningValEscape, hasErrors, diagnostics);
            BoundExpression whenClause = node.WhenClause != null
                ? armBinder.BindBooleanExpression(node.WhenClause.Condition, diagnostics)
                : null;
            BoundExpression armResult = armBinder.BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            var label = new GeneratedLabelSymbol("arm");
            return new BoundSwitchExpressionArm(node, locals, pattern, whenClause, armResult, label, hasErrors | pattern.HasErrors);
        }
    }
}
