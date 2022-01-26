// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundIsPatternExpression
    {
        public BoundDecisionDag GetDecisionDagForLowering(CSharpCompilation compilation)
        {
            BoundDecisionDag decisionDag = this.ReachabilityDecisionDag;
            if (decisionDag.ContainsAnySynthesizedNodes())
            {
                decisionDag = DecisionDagBuilder.CreateDecisionDagForIsPattern(
                    compilation,
                    this.Syntax,
                    this.Expression,
                    this.Pattern,
                    this.WhenTrueLabel,
                    this.WhenFalseLabel,
                    BindingDiagnosticBag.Discarded,
                    forLowering: true);
                Debug.Assert(!decisionDag.ContainsAnySynthesizedNodes());
            }

            return decisionDag;
        }
    }
}
