// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSwitchExpression
    {
        public BoundDecisionDag GetDecisionDagForLowering(CSharpCompilation compilation, out LabelSymbol? defaultLabel)
        {
            defaultLabel = this.DefaultLabel;

            BoundDecisionDag decisionDag = this.ReachabilityDecisionDag;
            if (decisionDag.ContainsAnySynthesizedNodes())
            {
                decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchExpression(
                    compilation,
                    this.Syntax,
                    this.Expression,
                    this.SwitchArms,
                    // there's no default label if the original switch is exhaustive.
                    // we generate a new label here because the new dag might not be.
                    defaultLabel ??= new GeneratedLabelSymbol("default"),
                    BindingDiagnosticBag.Discarded,
                    forLowering: true);
                Debug.Assert(!decisionDag.ContainsAnySynthesizedNodes());
            }

            return decisionDag;
        }
    }
}
