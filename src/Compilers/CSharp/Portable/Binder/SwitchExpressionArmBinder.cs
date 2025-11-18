// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Binder for one of the arms of a switch expression. For example, in the one-armed switch expression
    /// "e switch { p when c => v }", this could be the binder for the arm "p when c => v".
    /// </summary>
    internal sealed class SwitchExpressionArmBinder : Binder
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

        internal BoundSwitchExpressionArm BindSwitchExpressionArm(SwitchExpressionArmSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node == _arm);
            TypeSymbol inputType = _switchExpressionBinder.GetInputType();
            return BindSwitchExpressionArm(node, inputType, diagnostics);
        }

        internal override BoundSwitchExpressionArm BindSwitchExpressionArm(SwitchExpressionArmSyntax node, TypeSymbol switchGoverningType, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node == _arm);
            Binder armBinder = this.GetRequiredBinder(node);
            bool hasErrors = switchGoverningType.IsErrorType();
            ImmutableArray<LocalSymbol> locals = _armScopeBinder.Locals;
            BoundPattern pattern = armBinder.BindPattern(node.Pattern, switchGoverningType, permitDesignations: true, hasErrors, diagnostics, out bool hasUnionMatching);
            BoundExpression? whenClause = node.WhenClause != null
                ? armBinder.BindBooleanExpression(node.WhenClause.Condition, diagnostics)
                : null;
            BoundExpression armResult = armBinder.BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            var label = new GeneratedLabelSymbol("arm");
            return new BoundSwitchExpressionArm(node, locals, pattern, hasUnionMatching, whenClause, armResult, label, hasErrors | pattern.HasErrors);
        }
    }
}
