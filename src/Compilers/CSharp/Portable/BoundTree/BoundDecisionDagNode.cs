// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDecisionDagNode
    {
        public override bool Equals(object? other)
        {
            if (this == other)
                return true;

            switch (this, other)
            {
                case (BoundEvaluationDecisionDagNode n1, BoundEvaluationDecisionDagNode n2):
                    return n1.Evaluation.Equals(n2.Evaluation) && n1.Next == n2.Next;
                case (BoundTestDecisionDagNode n1, BoundTestDecisionDagNode n2):
                    return n1.Test.Equals(n2.Test) && n1.WhenTrue == n2.WhenTrue && n1.WhenFalse == n2.WhenFalse;
                case (BoundWhenDecisionDagNode n1, BoundWhenDecisionDagNode n2):
                    return n1.WhenExpression == n2.WhenExpression && n1.WhenTrue == n2.WhenTrue && n1.WhenFalse == n2.WhenFalse;
                case (BoundLeafDecisionDagNode n1, BoundLeafDecisionDagNode n2):
                    return n1.Label == n2.Label;
                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            switch (this)
            {
                case BoundEvaluationDecisionDagNode n:
                    return Hash.Combine(n.Evaluation.GetHashCode(), RuntimeHelpers.GetHashCode(n.Next));
                case BoundTestDecisionDagNode n:
                    return Hash.Combine(n.Test.GetHashCode(), Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenFalse), RuntimeHelpers.GetHashCode(n.WhenTrue)));
                case BoundWhenDecisionDagNode n:
                    // See https://github.com/dotnet/runtime/pull/31819 for why ! is temporarily required below.
                    return Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenExpression!), Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenFalse!), RuntimeHelpers.GetHashCode(n.WhenTrue)));
                case BoundLeafDecisionDagNode n:
                    return RuntimeHelpers.GetHashCode(n.Label);
                default:
                    throw ExceptionUtilities.UnexpectedValue(this);
            }
        }
    }
}
