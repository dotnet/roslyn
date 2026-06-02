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
            if (!decisionDag.SuitableForLowering)
            {
                bool negated = this.Pattern.IsNegated(out var innerPattern);
                Debug.Assert(negated == this.IsNegated);
                decisionDag = DecisionDagBuilder.CreateDecisionDagForIsPattern(
                    compilation,
                    this.Syntax,
                    this.Expression,
                    innerPattern,
                    HasUnionMatching,
                    this.WhenTrueLabel,
                    this.WhenFalseLabel,
                    BindingDiagnosticBag.Discarded,
                    forLowering: true);
                Debug.Assert(decisionDag.SuitableForLowering);
            }

            return decisionDag;
        }
    }
}
