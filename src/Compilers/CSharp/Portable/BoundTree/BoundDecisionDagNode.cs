// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if DEBUG
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
#endif
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
                    return Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenExpression), Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenFalse), RuntimeHelpers.GetHashCode(n.WhenTrue)));
                case BoundLeafDecisionDagNode n:
                    return RuntimeHelpers.GetHashCode(n.Label);
                default:
                    throw ExceptionUtilities.UnexpectedValue(this);
            }
        }

#if DEBUG
        private int _id = -1;

        public int Id
        {
            get
            {
                return _id;
            }
            internal set
            {
                Debug.Assert(value >= 0, "Id must be non-negative but was set to " + value);
                Debug.Assert(_id == -1, $"Id was set to {_id} and set again to {value}");
                _id = value;
            }
        }

        internal new string GetDebuggerDisplay()
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            builder.Append($"[{this.Id}]: ");
            switch (this)
            {
                case BoundTestDecisionDagNode node:
                    builder.Append($"{node.Test.GetDebuggerDisplay()} ");
                    builder.Append(node.WhenTrue != null
                        ? $"? [{node.WhenTrue.Id}] "
                        : "? <unreachable> ");

                    builder.Append(node.WhenFalse != null
                        ? $": [{node.WhenFalse.Id}]"
                        : ": <unreachable>");
                    break;
                case BoundEvaluationDecisionDagNode node:
                    builder.Append($"{node.Evaluation.GetDebuggerDisplay()}; ");
                    builder.Append(node.Next != null
                        ? $"[{node.Next.Id}]"
                        : "<unreachable>");
                    break;
                case BoundWhenDecisionDagNode node:
                    builder.Append("when ");
                    builder.Append(node.WhenExpression is { } when
                        ? $"({when.Syntax}) "
                        : "<true> ");

                    builder.Append(node.WhenTrue != null
                        ? $"? [{node.WhenTrue.Id}] "
                        : "? <unreachable> ");

                    builder.Append(node.WhenFalse != null
                        ? $": [{node.WhenFalse.Id}]"
                        : ": <unreachable>");

                    break;
                case BoundLeafDecisionDagNode node:
                    builder.Append(node.Label is GeneratedLabelSymbol generated
                        ? $"leaf {generated.NameNoSequence} `{node.Syntax}`"
                        : $"leaf `{node.Label.Name}`");
                    break;
                default:
                    builder.Append(base.GetDebuggerDisplay());
                    break;
            }

            return pooledBuilder.ToStringAndFree();
        }
#endif
    }
}
