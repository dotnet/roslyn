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
            builder.Append($"[{this.Id}]: ");
            switch (this)
            {
                case BoundTestDecisionDagNode node:
                    builder.Append($"{dumpDagTest(node.Test)} ");
                    builder.Append(node.WhenTrue != null
                        ? $"? [{node.WhenTrue.Id}] "
                        : "? <unreachable> ");

                    builder.Append(node.WhenFalse != null
                        ? $": [{node.WhenFalse.Id}]"
                        : ": <unreachable>");
                    break;
                case BoundEvaluationDecisionDagNode node:
                    builder.Append($"{dumpDagTest(node.Evaluation)}; ");
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
                    throw ExceptionUtilities.UnexpectedValue(this);
            }

            return pooledBuilder.ToStringAndFree();

            static string dumpDagTest(BoundDagTest test)
            {
                switch (test)
                {
                    case BoundDagTypeEvaluation a:
                        return $"{a.GetDebuggerDisplay()} = ({a.Type}){a.Input.GetDebuggerDisplay()}";
                    case BoundDagPropertyEvaluation e:
                        return $"{e.GetDebuggerDisplay()} = {e.Input.GetDebuggerDisplay()}.{e.Property.Name}";
                    case BoundDagFieldEvaluation e:
                        return $"{e.GetDebuggerDisplay()} = {e.Input.GetDebuggerDisplay()}.{e.Field.Name}";
                    case BoundDagDeconstructEvaluation d:
                        var result = "(";
                        var first = true;
                        foreach (var param in d.DeconstructMethod.Parameters)
                        {
                            if (!first)
                            {
                                result += ", ";
                            }
                            first = false;
                            result += $"Item{param.Ordinal + 1}";
                        }
                        result += $") {d.GetDebuggerDisplay()} = {d.Input.GetDebuggerDisplay()}";
                        return result;
                    case BoundDagIndexEvaluation i:
                        return $"{i.GetDebuggerDisplay()} = {i.Input.GetDebuggerDisplay()}[{i.Index}]";
                    case BoundDagEvaluation e:
                        return $"{e.GetDebuggerDisplay()} = {e.Kind}({e.Input.GetDebuggerDisplay()})";
                    case BoundDagTypeTest b:
                        var typeName = b.Type.TypeKind == TypeKind.Error ? "<error type>" : b.Type.ToString();
                        return $"{b.Input.GetDebuggerDisplay()} is {typeName}";
                    case BoundDagValueTest v:
                        return $"{v.Input.GetDebuggerDisplay()} == {v.Value.GetValueToDisplay()}";
                    case BoundDagNonNullTest nn:
                        return $"{nn.Input.GetDebuggerDisplay()} != null";
                    case BoundDagExplicitNullTest n:
                        return $"{n.Input.GetDebuggerDisplay()} == null";
                    case BoundDagRelationalTest r:
                        var operatorName = r.Relation.Operator() switch
                        {
                            BinaryOperatorKind.LessThan => "<",
                            BinaryOperatorKind.LessThanOrEqual => "<=",
                            BinaryOperatorKind.GreaterThan => ">",
                            BinaryOperatorKind.GreaterThanOrEqual => ">=",
                            _ => "??"
                        };
                        return $"{r.Input.GetDebuggerDisplay()} {operatorName} {r.Value.GetValueToDisplay()}";
                    default:
                        return $"{test.Kind}({test.Input.GetDebuggerDisplay()})";
                }
            }
        }
#endif
    }
}
