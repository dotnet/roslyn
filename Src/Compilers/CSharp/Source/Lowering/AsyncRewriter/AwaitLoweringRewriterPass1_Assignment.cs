
using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AwaitLoweringRewriterPass1
    {
        /// <summary>
        /// Rewrite an assignment operator.
        /// 
        /// Possible cases:
        ///   (1) Neither the lvalue nor rvalue contain an await expression.
        ///   (2) Only the lvalue contains an await expression.
        ///   (3) The rvalue contains an await expression, and the lvalue is one of:
        ///     (a) an await-containing expression,
        ///     (b) an array access,
        ///     (c) a field access, or
        ///     (d) a local
        ///
        /// </summary>
        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            BoundExpression leftNode = (BoundExpression)this.Visit(node.Left);
            BoundExpression rightNode = (BoundExpression)this.Visit(node.Right);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(leftNode, rightNode))
            {
                // Case (1) - no await expression:
                return node.Update(leftNode, rightNode, node.RefKind, type);
            }

            if (!RequiresSpill(rightNode))
            {
                // Case (2) - only lvalue contains await
                //     Spill(l_sideEffects, lvalue) = rvalue
                //   is rewritten as:
                //     Spill(l_sideEffects, lvalue = rvalue)

                var spill = (BoundSpillSequence) leftNode;
                var newAssignment = node.Update(spill.Value, rightNode, node.RefKind, type);
                return RewriteSpillSequence(spill, newAssignment);
            }

            // Case (3) -
            return SpillAssignmentOperator(node, leftNode, (BoundSpillSequence) rightNode);
        }

        private BoundNode SpillAssignmentOperator(BoundAssignmentOperator node, BoundExpression left, BoundSpillSequence right)
        {
            var spillBuilder = new SpillBuilder();
            var spilledLeftNode = SpillLValue(left, spillBuilder);
            var innerSpill = node.Update(spilledLeftNode, right.Value, node.RefKind, node.Type);
            spillBuilder.AddSpill(right);
            return spillBuilder.BuildSequenceAndFree(F, innerSpill);
        }

        private BoundExpression SpillLValue(BoundExpression left, SpillBuilder spillBuilder)
        {
            switch (left.Kind)
            {
                case BoundKind.Sequence:
                    {
                        var sequence = (BoundSequence)left;
                        spillBuilder.AddSequence(F, sequence);
                        return SpillLValue(sequence.Value, spillBuilder);
                    }

                case BoundKind.SpillSequence:
                    {
                        var spill = (BoundSpillSequence)left;
                        spillBuilder.AddSpill(spill);
                        return SpillLValue(spill.Value, spillBuilder);
                    }

                case BoundKind.ArrayAccess:
                    {
                        // Case 3.b -

                        var array = (BoundArrayAccess)left;

                        var spillReceiver = F.SpillTemp(array.Expression.Type, array.Expression);
                        spillBuilder.Statements.Add(GenerateSpillInit(spillReceiver));
                        spillBuilder.Temps.Add(spillReceiver);

                        var spilledIndices = ArrayBuilder<BoundExpression>.GetInstance();
                        foreach (var index in array.Indices)
                        {
                            var indexTemp = F.SpillTemp(index.Type, index);
                            spillBuilder.Statements.Add(GenerateSpillInit(indexTemp));
                            spillBuilder.Temps.Add(indexTemp);

                            spilledIndices.Add(indexTemp);
                        }

                        return array.Update(spillReceiver, spilledIndices.ToReadOnlyAndFree(), array.Type);
                    }

                case BoundKind.FieldAccess:
                    {
                        // Case 3.c -

                        var field = (BoundFieldAccess)left;
                        if (Unspillable(field.ReceiverOpt))
                        {
                            return field;
                        }

                        BoundExpression newReceiver;
                        if (field.ReceiverOpt.Type.IsReferenceType)
                        {
                            var receiverTemp = F.SpillTemp(field.ReceiverOpt.Type, field.ReceiverOpt);
                            spillBuilder.Statements.Add(GenerateSpillInit(receiverTemp));
                            spillBuilder.Temps.Add(receiverTemp);

                            newReceiver = receiverTemp;
                        }
                        else
                        {
                            Debug.Assert(field.ReceiverOpt.Type.IsValueType, "Don't spill unconstrained type parameters.");
                            newReceiver = SpillLValue(field.ReceiverOpt, spillBuilder);
                        }

                        return field.Update(
                            newReceiver,
                            field.FieldSymbol,
                            field.ConstantValueOpt,
                            field.ResultKind,
                            field.Type);
                    }

                case BoundKind.Local:
                    {
                        // Case 3.d -
                        return left;
                    }

                default:
                    throw new NotImplementedException("stack spilling for lvalue: " + left.Kind.ToString());
            }
        }

        private static bool Unspillable(BoundExpression node)
        {
            return node == null ||
                node.Kind == BoundKind.Literal ||
                node.Kind == BoundKind.ThisReference ||
                node.Kind == BoundKind.TypeExpression ||
                node.Type.IsStatic;
        }
    }
}