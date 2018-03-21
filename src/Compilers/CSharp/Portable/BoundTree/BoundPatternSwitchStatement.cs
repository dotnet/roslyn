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
            : this(syntax, expression, innerLocals, innerLocalFunctions, switchSections, defaultLabel, breakLabel, hasErrors)
        {

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
