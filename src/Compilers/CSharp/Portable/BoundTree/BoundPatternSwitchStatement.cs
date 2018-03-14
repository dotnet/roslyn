// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundPatternSwitchStatement
    {
        public BoundDecisionDag DecisionDag { get; }

        public BoundPatternSwitchStatement(SyntaxNode syntax, BoundExpression expression, ImmutableArray<LocalSymbol> innerLocals, ImmutableArray<LocalFunctionSymbol> innerLocalFunctions, ImmutableArray<BoundPatternSwitchSection> switchSections, BoundPatternSwitchLabel defaultLabel, GeneratedLabelSymbol breakLabel, BoundDecisionDag decisionDag, bool hasErrors = false)
            : base(BoundKind.PatternSwitchStatement, syntax, hasErrors || expression.HasErrors() || switchSections.HasErrors() || defaultLabel.HasErrors() || decisionDag.HasErrors())
        {

            Debug.Assert(expression != null, "Field 'expression' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(!innerLocals.IsDefault, "Field 'innerLocals' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(!innerLocalFunctions.IsDefault, "Field 'innerLocalFunctions' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(!switchSections.IsDefault, "Field 'switchSections' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");
            Debug.Assert(breakLabel != null, "Field 'breakLabel' cannot be null (use Null=\"allow\" in BoundNodes.xml to remove this check)");

            this.Expression = expression;
            this.InnerLocals = innerLocals;
            this.InnerLocalFunctions = innerLocalFunctions;
            this.SwitchSections = switchSections;
            this.DefaultLabel = defaultLabel;
            this.BreakLabel = breakLabel;
            this.DecisionDag = decisionDag;
        }

        public BoundPatternSwitchStatement Update(BoundExpression expression, ImmutableArray<LocalSymbol> innerLocals, ImmutableArray<LocalFunctionSymbol> innerLocalFunctions, ImmutableArray<BoundPatternSwitchSection> switchSections, BoundPatternSwitchLabel defaultLabel, GeneratedLabelSymbol breakLabel, BoundDecisionDag decisionDag)
        {
            if (expression != this.Expression || innerLocals != this.InnerLocals || innerLocalFunctions != this.InnerLocalFunctions || switchSections != this.SwitchSections || defaultLabel != this.DefaultLabel || breakLabel != this.BreakLabel || decisionDag != this.DecisionDag)
            {
                var result = new BoundPatternSwitchStatement(this.Syntax, expression, innerLocals, innerLocalFunctions, switchSections, defaultLabel, breakLabel, decisionDag, this.HasErrors);
                result.WasCompilerGenerated = this.WasCompilerGenerated;
                return result;
            }
            return this;
        }

        public HashSet<LabelSymbol> ReachableLabels => this.DecisionDag.ReachableLabels;
    }

}
