// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSwitchExpression
    {
        public BoundDecisionDag DecisionDag { get; }

        public BoundSwitchExpression(SyntaxNode syntax, BoundExpression expression, ImmutableArray<BoundSwitchExpressionArm> switchArms, BoundDecisionDag decisionDag, LabelSymbol defaultLabel, TypeSymbol type, bool hasErrors = false)
            : base(BoundKind.SwitchExpression, syntax, type, hasErrors || expression.HasErrors() || switchArms.HasErrors() || decisionDag.HasErrors())
        {

            Debug.Assert(expression != null, "Field 'expression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(!switchArms.IsDefault, "Field 'switchArms' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");

            this.Expression = expression;
            this.SwitchArms = switchArms;
            this.DecisionDag = decisionDag;
            this.DefaultLabel = defaultLabel;
        }

        public BoundSwitchExpression Update(BoundExpression expression, ImmutableArray<BoundSwitchExpressionArm> switchArms, BoundDecisionDag decisionDag, LabelSymbol defaultLabel, TypeSymbol type)
        {
            if (expression != this.Expression || switchArms != this.SwitchArms || decisionDag != this.DecisionDag || defaultLabel != this.DefaultLabel || type != this.Type)
            {
                var result = new BoundSwitchExpression(this.Syntax, expression, switchArms, decisionDag, defaultLabel, type, this.HasErrors);
                result.WasCompilerGenerated = this.WasCompilerGenerated;
                return result;
            }
            return this;
        }

        public HashSet<LabelSymbol> ReachableLabels => this.DecisionDag.ReachableLabels;
    }

}
