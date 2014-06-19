using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal sealed class OperatorRewriter : BoundTreeRewriter
    {
        private MethodSymbol containingSymbol;
        private readonly Compilation compilation;

        private OperatorRewriter(MethodSymbol containingSymbol, Compilation compilation)
        {
            this.compilation = compilation;
            this.containingSymbol = containingSymbol;
        }

        public static BoundStatement Rewrite(BoundStatement node, MethodSymbol containingSymbol, Compilation compilation)
        {
            Debug.Assert(node != null);
            Debug.Assert(compilation != null);

            var rewriter = new OperatorRewriter(containingSymbol, compilation);
            var result = (BoundStatement)rewriter.Visit(node);
            return result;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldContainingSymbol = this.containingSymbol;
            try
            {
                this.containingSymbol = node.Symbol;
                return base.VisitLambda(node);
            }
            finally
            {
                this.containingSymbol = oldContainingSymbol;
            }
        }

        private BoundNode RewriteStringConcatenation(BoundBinaryOperator node)
        {
            // UNDONE: We need to make this more sophisticated. For example, we should
            // UNDONE: be rewriting (M() + "A") + ("B" + N()) as 
            // UNDONE: String.Concat(M(), "AB", N()).
            // UNDONE: We have many overloads of String.Concat to choose from: that
            // UNDONE: take one, two, three, four strings, that take params arrays
            // UNDONE: in strings and objects, and so on. See the native compiler
            // UNDONE: string rewriter for details.
            // UNDONE: For now, just to get this going let's do it the easy way;
            // UNDONE: we'll just generate calls to String.Concat(string, string)
            // UNDONE: or String.Concat(object, object) as appropriate.

            Debug.Assert(node != null);
            Debug.Assert(node.ConstantValueOpt == null);

            SpecialMember member = (node.OperatorKind == BinaryOperatorKind.StringConcatenation) ?
                SpecialMember.System_String__ConcatStringString :
                SpecialMember.System_String__ConcatObjectObject;

            var method = (MethodSymbol)this.compilation.Assembly.GetSpecialTypeMember(member);

            // UNDONE: Handle the bizarre error case where we don't have the expected string concat methods.
            Debug.Assert(method != null);

            return Visit(BoundCall.SynthesizedCall(null, method, node.Left, node.Right));
        }

        private BoundNode RewriteStringEquality(BoundBinaryOperator node, SpecialMember member)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.ConstantValueOpt == null);

            if (node.Left.ConstantValue == ConstantValue.Null || node.Right.ConstantValue == ConstantValue.Null)
            {
                return base.VisitBinaryOperator(node);
            }

            var method = (MethodSymbol)this.compilation.Assembly.GetSpecialTypeMember(member);
            Debug.Assert(method != null);

            return Visit(BoundCall.SynthesizedCall(null, method, node.Left, node.Right));
        }

        private BoundNode RewriteDelegateOperation(BoundBinaryOperator node, SpecialMember member)
        {
            Debug.Assert(node != null);
            var method = (MethodSymbol)this.compilation.Assembly.GetSpecialTypeMember(member);
            // UNDONE: Handle the bizarre error case where we don't have the expected methods.
            Debug.Assert(method != null);
            BoundExpression call = BoundCall.SynthesizedCall(null, method, node.Left, node.Right);
            BoundExpression result = method.ReturnType.SpecialType == SpecialType.System_Delegate ?
                BoundConversion.SynthesizedConversion(call, ConversionKind.ExplicitReference, node.Type) :
                call;
            return Visit(result);
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            if (node.ConstantValueOpt != null)
            {
                return node;
            }

            switch (node.OperatorKind)
            {
                case BinaryOperatorKind.ObjectAndStringConcatenation:
                case BinaryOperatorKind.StringAndObjectConcatenation:
                case BinaryOperatorKind.StringConcatenation:
                    return RewriteStringConcatenation(node);
                case BinaryOperatorKind.StringEqual:
                    return RewriteStringEquality(node, SpecialMember.System_String__op_Equality);
                case BinaryOperatorKind.StringNotEqual:
                    return RewriteStringEquality(node, SpecialMember.System_String__op_Inequality);
                case BinaryOperatorKind.DelegateCombination:
                    return RewriteDelegateOperation(node, SpecialMember.System_Delegate__Combine);
                case BinaryOperatorKind.DelegateRemoval:
                    return RewriteDelegateOperation(node, SpecialMember.System_Delegate__Remove);
                case BinaryOperatorKind.DelegateEqual:
                    return RewriteDelegateOperation(node, SpecialMember.System_Delegate__op_Equality);
                case BinaryOperatorKind.DelegateNotEqual:
                    return RewriteDelegateOperation(node, SpecialMember.System_Delegate__op_Inequality);
            }
            return base.VisitBinaryOperator(node);
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            // TestExpression ?? ElseExpression
            if (node.ConstantValue == null && !node.HasErrors)
            {
                var testExpression = node.TestExpression;
                ConstantValue testExpressionConstantValue = testExpression.ConstantValue;
                if (testExpressionConstantValue != null)
                {
                    // testExpression is a known compile time constant
                    if (testExpressionConstantValue == ConstantValue.Null)
                    {
                        // testExpression is always null
                        return Visit(node.ElseExpression);
                    }
                    // testExpression is always non null constant
                    return Visit(node.LeftConversion ?? node.TestExpression);
                }
                else
                {
                    TypeSymbol exprType = node.Type;
                    BoundExpression elseExpression = node.ElseExpression;
                    BoundExpression leftConversion = node.LeftConversion; //it's a BoundConversion, but we're passing by ref

                    // There are two ways that a conversion of the test expression can be represented:
                    //   1) If it was cast in source, then node.TestExpression could be a BoundConversion, or
                    //   2) If the compiler generated a conversion to reconcile the types, then node.LeftConversion could be non-null.
                    if (leftConversion == null)
                    {
                        if (IsUpdateRequiredForExplicitConversion(exprType, ref testExpression, ref elseExpression))
                        {
                            return node.Update(
                                testExpression,
                                elseExpression,
                                leftConversion: null,
                                constantValueOpt: null,
                                type: exprType);
                        }
                    }
                    else if (IsUpdateRequiredForExplicitConversion(exprType, ref leftConversion, ref elseExpression))
                    {
                        return node.Update(
                            (BoundExpression)Visit(testExpression),
                            elseExpression,
                            (BoundConversion)leftConversion,
                            constantValueOpt: null,
                            type: exprType);
                    }
                }
            }
            return base.VisitNullCoalescingOperator(node);
        }

        /// <summary>
        /// If the condition has a constant value, then just use the selected branch.
        /// e.g. "true ? x : y" becomes "x".
        /// 
        /// In some special cases, it is also necessary to make implicit reference conversions
        /// explicit to satisfy CLR verification rules.  See IsUpdateRequiredForExplicitConversion.
        /// </summary>
        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            if (node.ConstantValue == null && !node.HasErrors)
            {
                ConstantValue conditionConstantValue = node.Condition.ConstantValue;
                if (conditionConstantValue == ConstantValue.True)
                {
                    return Visit(node.Consequence);
                }
                else if (conditionConstantValue == ConstantValue.False)
                {
                    return Visit(node.Alternative);
                }
                else if (conditionConstantValue == null || !conditionConstantValue.IsBad)
                {
                    TypeSymbol exprType = node.Type;
                    BoundExpression consequence = node.Consequence;
                    BoundExpression alternative = node.Alternative;

                    if (IsUpdateRequiredForExplicitConversion(exprType, ref consequence, ref alternative))
                    {
                        return node.Update(
                            node.Condition,
                            consequence,
                            alternative,
                            node.ConstantValueOpt,
                            exprType);
                    }
                }
            }
            return base.VisitConditionalOperator(node);
        }

        /// <summary>
        /// Determines whether it is necessary to perform an explicit conversion so that the
        /// types of two conditional branches will verifiably reconcile.
        /// </summary>
        /// <param name="destinationType">The expected type of both branches.</param>
        /// <param name="expr1">The first branch.  Ready to be used (i.e. visited, converted, etc) if the return value is true.</param>
        /// <param name="expr2">The second branch.  Ready to be used (i.e. visited, converted, etc) if the return value is true.</param>
        /// <returns>True if the the BoundNode containing the conditional branches should be updated.</returns>
        /// <remarks>
        /// From ILGENREC::GenQMark
        /// See VSWhideby Bugs #49619 and 108643. If the destination type is an interface we need
        /// to force a static cast to be generated for any cast result expressions. The static cast
        /// should be done before the unifying jump so the code is verifiable and to allow the JIT to
        /// optimize it away. NOTE: Since there is no staticcast instruction, we implement static cast
        /// with a stloc / ldloc to a temporary.
        /// http://bugcheck/bugs/VSWhidbey/49619
        /// http://bugcheck/bugs/VSWhidbey/108643
        /// http://bugcheck/bugs/DevDivBugs/42645
        /// </remarks>
        private bool IsUpdateRequiredForExplicitConversion(TypeSymbol destinationType, ref BoundExpression expr1, ref BoundExpression expr2)
        {
            Debug.Assert(destinationType == expr1.Type && destinationType == expr2.Type);

            if (destinationType.TypeKind == TypeKind.Interface && expr1.Kind == BoundKind.Conversion && expr2.Kind == BoundKind.Conversion)
            {
                BoundConversion conv1 = (BoundConversion)expr1;
                BoundConversion conv2 = (BoundConversion)expr2;

                if (conv1.ConversionKind == ConversionKind.ImplicitReference &&
                    conv2.ConversionKind == ConversionKind.ImplicitReference)
                {
                    // NOTE: Dev10 says we only need to change one, and changes the second
                    // EXPLANATION: See ECMA 335, Partition III, Section 1.8.1.3.  Basically,
                    // the algorithm is:
                    //   1) If the left type works for the right expression, use that; else
                    //   2) If the right type works for the left expression, use that; else
                    //   3) If there is a "closest common supertype", use that; else
                    //   4) Error.
                    // The issue is that, if the types don't match exactly but share an interface,
                    // there's no guarantee that the runtime will be able to find the interface.
                    // However, if you convert one of the types to the interface type, then it will
                    // work for both expression, and either (1) or (2) will succeed.
                    // This explains both (a) why this only applies to interfaces and (b) why it's
                    // okay to only explicitly convert one branch.

                    expr1 = (BoundExpression)Visit(conv1);
                    expr2 = MakeImplicitReferenceConversionExplicit(conv2);
                    return true;
                }
            }

            return false;
        }

        private BoundConversion MakeImplicitReferenceConversionExplicit(BoundConversion implicitConv)
        {
            Debug.Assert(implicitConv.ConversionKind == ConversionKind.ImplicitReference);
            Debug.Assert(implicitConv.Type.TypeKind == TypeKind.Interface);
            return implicitConv.Update(
                operand: (BoundExpression)Visit(implicitConv.Operand), //NB: visit
                conversionKind: ConversionKind.ExplicitReference,
                symbolOpt: null,
                @checked: false,
                explicitCastInCode: false,
                constantValueOpt: implicitConv.ConstantValueOpt,
                type: implicitConv.Type);
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            if (node.HasErrors)
            {
                return node;
            }

            // There are five possible cases.
            //
            // Case 1: receiver.Prop += value is transformed into
            // temp = receiver
            // temp.Prop = temp.Prop + value
            // and a later rewriting will turn that into calls to getters and setters.
            //
            // Case 2: collection[i1, i2, i3] += value is transformed into
            // tc = collection
            // t1 = i1
            // t2 = i2
            // t3 = i3
            // tc[t1, t2, t3] = tc[t1, t2, t3] + value
            // and again, a later rewriting will turn that into getters and setters of the indexer.
            //
            // Case 3: local += value (and param += value) needs no temporaries; it simply
            // becomes local = local + value.
            //
            // Case 4: staticField += value needs no temporaries either. However, classInst.field += value becomes
            // temp = classInst
            // temp.field = temp.field + value 
            //
            // Case 5: otherwise, it must be structVariable.field += value or array[index] += value. Either way
            // we have a variable on the left. Transform it into:
            // ref temp = ref variable
            // temp = temp + value

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();

            // This will be filled in with the LHS that uses temporaries to prevent
            // double-evaluation of side effects.

            BoundExpression transformedLHS = null;

            if (node.Left.Kind == BoundKind.PropertyAccess)
            {
                // We need to stash away the receiver so that it does not get evaluated twice.
                // If the receiver is classified as a value of reference type then we can simply say
                //
                // R temp = receiver
                // temp.prop = temp.prop + rhs
                //
                // But if the receiver is classified as a variable of struct type then we
                // cannot make a copy of the value; we need to make sure that we mutate
                // the original receiver, not the copy.  We have to generate
                //
                // ref R temp = ref receiver
                // temp.prop = temp.prop + rhs
                //
                // The rules of C# (in section 7.17.1) require that if you have receiver.prop 
                // as the target of an assignment such that receiver is a value type, it must
                // be classified as a variable. If we've gotten this far in the rewriting,
                // assume that was the case.

                var prop = (BoundPropertyAccess)node.Left;

                // If the property is static then we can just generate prop = prop + value
                if (prop.ReceiverOpt == null)
                {
                    transformedLHS = prop;
                }
                else
                {
                    // Can we ever avoid storing the receiver in a temp? If the receiver is a variable then it 
                    // might be modified by the computation of the getter, the value, or the operation. 
                    // The receiver cannot be a null constant or constant of value type. It could be a 
                    // constant of string type, but there are no mutable properties of a string.
                    // Similarly, there are no mutable properties of a Type object, so the receiver
                    // cannot be a typeof(T) expression. The only situation I can think of where we could
                    // optimize away the temp is if the receiver is a readonly field of reference type,
                    // we are not in a constructor, and the receiver of the *field*, if any, is also idempotent.
                    // It doesn't seem worthwhile to pursue an optimization for this exceedingly rare case.

                    var rewrittenReceiver = (BoundExpression)Visit(prop.ReceiverOpt);
                    var receiverTemp = TempHelpers.StoreToTemp(rewrittenReceiver, rewrittenReceiver.Type.IsValueType ? RefKind.Ref : RefKind.None, containingSymbol);
                    stores.Add(receiverTemp.Item1);
                    temps.Add(receiverTemp.Item2.LocalSymbol);
                    transformedLHS = new BoundPropertyAccess(prop.Syntax, prop.SyntaxTree, receiverTemp.Item2, prop.PropertySymbol, prop.Type);
                }
            }
            else if (node.Left.Kind == BoundKind.IndexerAccess)
            {
                var indexer = (BoundIndexerAccess)node.Left;
                BoundExpression transformedReceiver = null;
                if (indexer.ReceiverOpt != null)
                {
                    var rewrittenReceiver = (BoundExpression)Visit(indexer.ReceiverOpt);
                    var receiverTemp = TempHelpers.StoreToTemp(rewrittenReceiver, rewrittenReceiver.Type.IsValueType ? RefKind.Ref : RefKind.None, containingSymbol);
                    transformedReceiver = receiverTemp.Item2;
                    stores.Add(receiverTemp.Item1);
                    temps.Add(receiverTemp.Item2.LocalSymbol);
                }

                // UNDONE: Dealing with the arguments is a bit tricky because they can be named out-of-order arguments;
                // UNDONE: we have to preserve both the source-code order of the side effects and the side effects
                // UNDONE: only being executed once.
                // UNDONE:
                // UNDONE: This is a subtly different problem than the problem faced by the conventional call
                // UNDONE: rewriter; with the conventional call rewriter we already know that the side effects
                // UNDONE: will only be executed once because the arguments are only being pushed on the stack once. 
                // UNDONE: In a compound equality operator on an indexer the indices are placed on the stack twice. 
                // UNDONE: That is to say, if you have:
                // UNDONE:
                // UNDONE: C().M(z : Z(), x : X(), y : Y())
                // UNDONE:
                // UNDONE: then we can rewrite that into
                // UNDONE:
                // UNDONE: tempc = C()
                // UNDONE: tempz = Z()
                // UNDONE: tempc.M(X(), Y(), tempz)
                // UNDONE:
                // UNDONE: See, we can optimize away two of the temporaries, for x and y. But we cannot optimize away any of the
                // UNDONE: temporaries in
                // UNDONE:
                // UNDONE: C().Collection[z : Z(), x : X(), y : Y()] += 1;
                // UNDONE:
                // UNDONE: because we have to ensure not just that Z() happens first, but in additioan that X() and Y() are only 
                // UNDONE: called once.  We have to generate this as
                // UNDONE:
                // UNDONE: tempc = C().Collection
                // UNDONE: tempz = Z()
                // UNDONE: tempx = X()
                // UNDONE: tempy = Y()
                // UNDONE: tempc[tempx, tempy, tempz] = tempc[tempx, tempy, tempz] + 1;
                // UNDONE:
                // UNDONE: Fortunately arguments to indexers are never ref or out, so we don't need to worry about that.
                // UNDONE: However, we can still do the optimization where constants are not stored in
                // UNDONE: temporaries; if we have
                // UNDONE: 
                // UNDONE: C().Collection[z : 123, y : Y(), x : X()] += 1;
                // UNDONE:
                // UNDONE: Then we can generate that as
                // UNDONE:
                // UNDONE: tempc = C().Collection
                // UNDONE: tempx = X()
                // UNDONE: tempy = Y()
                // UNDONE: tempc[tempx, tempy, 123] = tempc[tempx, tempy, 123] + 1;
                // UNDONE:
                // UNDONE: For now, we'll punt on both problems, as indexers are not implemented yet anyway.
                // UNDONE: We'll just generate one temporary for each argument. This will work, but in the
                // UNDONE: subsequent rewritings will generate more unnecessary temporaries. 

                var transformedArguments = ArrayBuilder<BoundExpression>.GetInstance();
                foreach (var argument in indexer.Arguments)
                {
                    var rewrittenArgument = (BoundExpression)Visit(argument);
                    var argumentTemp = TempHelpers.StoreToTemp(rewrittenArgument, RefKind.None, containingSymbol);
                    transformedArguments.Add(argumentTemp.Item2);
                    stores.Add(argumentTemp.Item1);
                    temps.Add(argumentTemp.Item2.LocalSymbol);
                }

                transformedLHS = new BoundIndexerAccess(indexer.Syntax, indexer.SyntaxTree, transformedArguments.ToReadOnlyAndFree(), transformedReceiver,
                    indexer.IndexerSymbol, indexer.Type);
            }
            else if (node.Left.Kind == BoundKind.Local || node.Left.Kind == BoundKind.Parameter)
            {
                // No temporaries are needed. Just generate local = local + value
                transformedLHS = node.Left;
            }
            else if (node.Left.Kind == BoundKind.FieldAccess)
            {
                // * If the field is static then no temporaries are needed. 
                // * If the field is not static and the receiver is of reference type then generate t = r; t.f = t.f + value
                // * If the field is not static and the receiver is a variable of value type then we'll fall into the
                //   general variable case below.

                var fieldAccess = (BoundFieldAccess)node.Left;
                if (fieldAccess.ReceiverOpt == null)
                {
                    transformedLHS = fieldAccess;
                }
                else if (!fieldAccess.ReceiverOpt.Type.IsValueType)
                {
                    var rewrittenReceiver = (BoundExpression)Visit(fieldAccess.ReceiverOpt);
                    var receiverTemp = TempHelpers.StoreToTemp(rewrittenReceiver, RefKind.None, containingSymbol);
                    stores.Add(receiverTemp.Item1);
                    temps.Add(receiverTemp.Item2.LocalSymbol);
                    transformedLHS = new BoundFieldAccess(fieldAccess.Syntax, fieldAccess.SyntaxTree, receiverTemp.Item2, fieldAccess.FieldSymbol, null);
                }
            }

            if (transformedLHS == null)
            {
                // We made no transformation above. Either we have array[index] += value or 
                // structVariable.field += value; either way we have a potentially complicated variable-
                // producing expression on the left. Generate
                // ref temp = ref variable; temp = temp + value
                var rewrittenVariable = (BoundExpression)Visit(node.Left);
                var variableTemp = TempHelpers.StoreToTemp(rewrittenVariable, RefKind.Ref, containingSymbol);
                stores.Add(variableTemp.Item1);
                temps.Add(variableTemp.Item2.LocalSymbol);
                transformedLHS = variableTemp.Item2;
            }

            // OK, we now have the temporary declarations, the temporary stores, and the transformed left hand side.
            // We need to generate 
            //
            // xlhs = (FINAL)((LEFT)xlhs op rhs)
            //
            // And then wrap it up with the generated temporaries.
            //
            // (The right hand side has already been converted to the type expected by the operator.)

            BoundExpression opLHS = BoundConversion.SynthesizedConversion(transformedLHS, node.LeftConversion, node.Operator.LeftType);
            Debug.Assert(node.Right.Type == node.Operator.RightType);
            BoundExpression op = new BoundBinaryOperator(null, null, node.Operator.Kind, opLHS, node.Right, null, node.Operator.ReturnType);
            BoundExpression opFinal = BoundConversion.SynthesizedConversion(op, node.FinalConversion, node.Left.Type);
            BoundExpression assignment = new BoundAssignmentOperator(null, null, transformedLHS, opFinal, node.Left.Type);

            // OK, at this point we have:
            //
            // * temps evaluating and storing portions of the LHS that must be evaluated only once.
            // * the "transformed" left hand side, rebuilt to use temps where necessary
            // * the assignment "xlhs = (FINAL)((LEFT)xlhs op (RIGHT)rhs)"
            // 
            // Notice that we have recursively rewritten the bound nodes that are things stored in
            // the temps, but we might have more rewriting to do on the assignment. There are three
            // conversions in there that might be lowered to method calls, an operator that might
            // be lowered to delegate combine, string concat, and so on, and don't forget, we 
            // haven't lowered the right hand side at all! Let's rewrite all these things at once.

            BoundExpression rewrittenAssignment = (BoundExpression)Visit(assignment);

            BoundExpression result = (temps.Count == 0) ?
                rewrittenAssignment :
                new BoundSequence(null,
                    null,
                    temps.ToReadOnly(),
                    stores.ToReadOnly(),
                    rewrittenAssignment,
                    rewrittenAssignment.Type);

            temps.Free();
            stores.Free();
            return result;
        }
    }
}