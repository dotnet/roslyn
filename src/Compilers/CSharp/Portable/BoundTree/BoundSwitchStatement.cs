// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSwitchStatement
    {
        public BoundDecisionDag GetDecisionDagForLowering(CSharpCompilation compilation)
        {
            BoundDecisionDag decisionDag = this.ReachabilityDecisionDag;
            if (!decisionDag.SuitableForLowering)
            {
                decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchStatement(
                    compilation,
                    this.Syntax,
                    this.Expression,
                    this.SwitchSections,
                    this.DefaultLabel?.Label ?? this.BreakLabel,
                    BindingDiagnosticBag.Discarded,
                    forLowering: true);
                Debug.Assert(decisionDag.SuitableForLowering);
            }

            return decisionDag;
        }
    }
}
