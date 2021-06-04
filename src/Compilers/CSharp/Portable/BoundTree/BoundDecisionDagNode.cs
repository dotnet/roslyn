// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
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
                    // See https://github.com/dotnet/runtime/pull/31819 for why ! is temporarily required below.
                    return Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenExpression!), Hash.Combine(RuntimeHelpers.GetHashCode(n.WhenFalse!), RuntimeHelpers.GetHashCode(n.WhenTrue)));
                case BoundLeafDecisionDagNode n:
                    return RuntimeHelpers.GetHashCode(n.Label);
                default:
                    throw ExceptionUtilities.UnexpectedValue(this);
            }
        }

#if DEBUG
        private int _id;
        private bool _idWasRead;

        public int Id
        {
            get
            {
                _idWasRead = true;
                return _id;
            }
            internal set
            {
                Debug.Assert(!_idWasRead, "Id was set after reading it");
                _id = value;
            }
        }

        internal new string GetDebuggerDisplay()
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            builder.AppendLine($"State " + this.Id);
            switch (this)
            {
                case BoundTestDecisionDagNode node:
                    builder.AppendLine($"  Test: {dumpDagTest(node.Test)}");
                    if (node.WhenTrue != null)
                    {
                        builder.AppendLine($"  WhenTrue: {node.WhenTrue.Id}");
                    }

                    if (node.WhenFalse != null)
                    {
                        builder.AppendLine($"  WhenFalse: {node.WhenFalse.Id}");
                    }
                    break;
                case BoundEvaluationDecisionDagNode node:
                    builder.AppendLine($"  Test: {dumpDagTest(node.Evaluation)}");
                    if (node.Next != null)
                    {
                        builder.AppendLine($"  Next: {node.Next.Id}");
                    }
                    break;
                case BoundWhenDecisionDagNode node:
                    builder.AppendLine($"  WhenClause: " + node.WhenExpression?.Syntax);
                    if (node.WhenTrue != null)
                    {
                        builder.AppendLine($"  WhenTrue: {node.WhenTrue.Id}");
                    }

                    if (node.WhenFalse != null)
                    {
                        builder.AppendLine($"  WhenFalse: {node.WhenFalse.Id}");
                    }
                    break;
                case BoundLeafDecisionDagNode node:
                    builder.AppendLine($"  Case: {node.Label.Name}" + node.Syntax);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(this);
            }

            return pooledBuilder.ToStringAndFree();

            string dumpDagTest(BoundDagTest d)
            {
                switch (d)
                {
                    case BoundDagTypeEvaluation a:
                        return $"{a.GetDebuggerDisplay()}={a.Kind}({a.GetDebuggerDisplay()} as {a.Type})";
                    case BoundDagEvaluation e:
                        return $"{e.GetDebuggerDisplay()}={e.Kind}({e.GetDebuggerDisplay()})";
                    case BoundDagTypeTest b:
                        return $"?{d.Kind}({d.Input.GetDebuggerDisplay()} is {b.Type})";
                    case BoundDagValueTest v:
                        return $"?{d.Kind}({d.Input.GetDebuggerDisplay()} == {v.Value})";
                    case BoundDagRelationalTest r:
                        var operatorName = r.Relation.Operator() switch
                        {
                            BinaryOperatorKind.LessThan => "<",
                            BinaryOperatorKind.LessThanOrEqual => "<=",
                            BinaryOperatorKind.GreaterThan => ">",
                            BinaryOperatorKind.GreaterThanOrEqual => ">=",
                            _ => "??"
                        };
                        return $"?{d.Kind}({d.Input.GetDebuggerDisplay()} {operatorName} {r.Value})";
                    default:
                        return $"?{d.Kind}({d.Input.GetDebuggerDisplay()})";
                }
            }
        }
#endif
    }
}
