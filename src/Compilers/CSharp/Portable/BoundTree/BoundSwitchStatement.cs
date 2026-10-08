// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSwitchStatement
    {
        public BoundDecisionDag GetDecisionDagForLowering(CSharpCompilation compilation, out LabelSymbol? unreachableDefaultLabel)
        {
            unreachableDefaultLabel = null;

            BoundDecisionDag decisionDag = this.ReachabilityDecisionDag;
            if (!decisionDag.SuitableForLowering)
            {
                LabelSymbol? defaultLabel = null;

                if (this.DefaultLabel is { } defaultSwitchLabel)
                {
                    // We get here when there is an explicit 'default:' label
                    // By definition, the switch is exhaustive, but it is possible, that according to
                    // the reachability Dag, the label is not reachable through the Dag (it might still
                    // be reachable through an explicit goto).

                    if (decisionDag.ReachableLabels.Contains(defaultSwitchLabel.Label))
                    {
                        // The label was reachable. We don't need to worry about lowering Dag saying the same,
                        // because it must. It is safe to jump to BreakLabel for any unmatched input,
                        // otherwise it would be unsafe with reachability Dag as well, and an error about that
                        // (not all code paths return a value, etc.) would be reported elsewhere.
                        defaultLabel = defaultSwitchLabel.Label;
                    }
                    else
                    {
                        // If the code becomes reachable according to the lowering Dag,
                        // we need to throw "unreachable" for code paths that reach the label
                        // directly from the lowering Dag.
                        // An explicit goto, however, should still go the the declared default label without throwing.
                    }
                }
                else if (decisionDag.ReachableLabels.Contains(this.BreakLabel))
                {
                    // We get here when the switch statement is not exhaustive according to the
                    // reachability Dag. We don't need to worry about lowering Dag saying the same,
                    // because it must. It is safe to jump to BreakLabel for an unmatched input,
                    // otherwise it would be unsafe with reachability Dag as well, and an error about that
                    // (not all code paths return a value, etc.) would be reported elsewhere.
                    defaultLabel = this.BreakLabel;
                }

                if (defaultLabel is null)
                {
                    // The switch statement is exhaustive according to the reachability Dag. If it is
                    // not exhaustive according to lowering Dag (when this label is reachable),
                    // we need to emit throw "unreachable" at this label.
                    defaultLabel = unreachableDefaultLabel = new GeneratedLabelSymbol("unreachableDefault");
                }

                decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchStatement(
                    compilation,
                    this.Syntax,
                    this.Expression,
                    this.SwitchSections,
                    defaultLabel,
                    BindingDiagnosticBag.Discarded,
                    forLowering: true);
                Debug.Assert(decisionDag.SuitableForLowering);

                if (unreachableDefaultLabel is not null)
                {
                    Debug.Assert(this.DefaultLabel?.Label != unreachableDefaultLabel);
                    Debug.Assert(this.BreakLabel != unreachableDefaultLabel);
                    Debug.Assert(defaultLabel == unreachableDefaultLabel);
                    Debug.Assert(!this.ReachabilityDecisionDag.ReachableLabels.Contains(unreachableDefaultLabel));

                    if (!decisionDag.ReachableLabels.Contains(unreachableDefaultLabel))
                    {
                        unreachableDefaultLabel = null;
                    }
                }
            }

            return decisionDag;
        }
    }
}
