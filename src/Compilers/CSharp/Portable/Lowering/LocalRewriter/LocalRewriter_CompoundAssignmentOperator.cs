// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            return VisitCompoundAssignmentOperator(node, true);
        }

        private BoundExpression VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node, bool used)
        {
            Debug.Assert(node.Right.Type == node.Operator.RightType);
            BoundExpression loweredRight = VisitExpression(node.Right);

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();

            var kind = node.Operator.Kind;
            bool isChecked = kind.IsChecked();
            bool isDynamic = kind.IsDynamic();
            var binaryOperator = kind.Operator();

            // if LHS is a member access and the operation is += or -=, we need to check at runtime if the LHS is an event.
            // We rewrite dyn.Member op= RHS to the following:
            //
            //   IsEvent("Member", dyn) ? InvokeMember("{add|remove}_Member", dyn, RHS) : SetMember(BinaryOperation("op=", GetMember("Member", dyn)), RHS)
            //
            bool isPossibleEventHandlerOperation = node.Left.Kind == BoundKind.DynamicMemberAccess &&
                (binaryOperator == BinaryOperatorKind.Addition || binaryOperator == BinaryOperatorKind.Subtraction);

            // save RHS to a temp, we need to use it twice:
            if (isPossibleEventHandlerOperation && CanChangeValueBetweenReads(loweredRight))
            {
                BoundAssignmentOperator assignmentToTemp;
                var temp = _factory.StoreToTemp(loweredRight, out assignmentToTemp);
                loweredRight = temp;
                stores.Add(assignmentToTemp);
                temps.Add(temp.LocalSymbol);
            }

            // This will be filled in with the LHS that uses temporaries to prevent
            // double-evaluation of side effects.
            BoundExpression transformedLHS = TransformCompoundAssignmentLHS(node.Left, stores, temps, isDynamic);

            CSharpSyntaxNode syntax = node.Syntax;

            // OK, we now have the temporary declarations, the temporary stores, and the transformed left hand side.
            // We need to generate 
            //
            // xlhs = (FINAL)((LEFT)xlhs op rhs)
            //
            // And then wrap it up with the generated temporaries.
            //
            // (The right hand side has already been converted to the type expected by the operator.)

            var lhsRead = MakeRValue(transformedLHS);

            BoundExpression opLHS = isDynamic ? lhsRead : MakeConversion(
                syntax: syntax,
                rewrittenOperand: lhsRead,
                conversion: node.LeftConversion,
                rewrittenType: node.Operator.LeftType,
                @checked: isChecked);

            BoundExpression operand = MakeBinaryOperator(syntax, node.Operator.Kind, opLHS, loweredRight, node.Operator.ReturnType, node.Operator.Method, isCompoundAssignment: true);

            BoundExpression opFinal = MakeConversion(
                syntax: syntax,
                rewrittenOperand: operand,
                conversion: node.FinalConversion,
                rewrittenType: node.Left.Type,
                explicitCastInCode: isDynamic,
                @checked: isChecked);

            BoundExpression rewrittenAssignment = MakeAssignmentOperator(syntax, transformedLHS, opFinal, node.Left.Type, used: used, isChecked: isChecked, isCompoundAssignment: true);

            // OK, at this point we have:
            //
            // * temps evaluating and storing portions of the LHS that must be evaluated only once.
            // * the "transformed" left hand side, rebuilt to use temps where necessary
            // * the assignment "xlhs = (FINAL)((LEFT)xlhs op (RIGHT)rhs)"
            // 
            // Notice that we have recursively rewritten the bound nodes that are things stored in
            // the temps, and by calling the "Make" methods we have rewritten the conversions and
            // assignments too, if necessary.

            if (isPossibleEventHandlerOperation)
            {
                // IsEvent("Foo", dyn) ? InvokeMember("{add|remove}_Foo", dyn, RHS) : rewrittenAssignment
                var memberAccess = (BoundDynamicMemberAccess)transformedLHS;

                var isEventCondition = _dynamicFactory.MakeDynamicIsEventTest(memberAccess.Name, memberAccess.Receiver);

                var invokeEventAccessor = _dynamicFactory.MakeDynamicEventAccessorInvocation(
                    (binaryOperator == BinaryOperatorKind.Addition ? "add_" : "remove_") + memberAccess.Name,
                    memberAccess.Receiver,
                    loweredRight);

                rewrittenAssignment = _factory.Conditional(isEventCondition.ToExpression(), invokeEventAccessor.ToExpression(), rewrittenAssignment, rewrittenAssignment.Type);
            }

            BoundExpression result = (temps.Count == 0 && stores.Count == 0) ?
                rewrittenAssignment :
                new BoundSequence(
                    syntax,
                    temps.ToImmutable(),
                    stores.ToImmutable(),
                    rewrittenAssignment,
                    rewrittenAssignment.Type);

            temps.Free();
            stores.Free();
            return result;
        }

        private BoundPropertyAccess TransformPropertyAccess(BoundPropertyAccess prop, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
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

            // If the property is static or if the receiver is of kind "Base" or "this", then we can just generate prop = prop + value
            if (prop.ReceiverOpt == null || prop.PropertySymbol.IsStatic || !CanChangeValueBetweenReads(prop.ReceiverOpt))
            {
                return prop;
            }

            Debug.Assert(prop.ReceiverOpt.Kind != BoundKind.TypeExpression);

            BoundExpression rewrittenReceiver = VisitExpression(prop.ReceiverOpt);

            BoundAssignmentOperator assignmentToTemp;

            // SPEC VIOLATION: It is not very clear when receiver of constrained callvirt is dereferenced - when pushed (in lexical order),
            // SPEC VIOLATION: or when actual call is executed. The actual behavior seems to be implementation specific in different JITs.
            // SPEC VIOLATION: To not depend on that, the right thing to do here is to store the value of the variable 
            // SPEC VIOLATION: when variable has reference type (regular temp), and store variable's location when it has a value type. (ref temp)
            // SPEC VIOLATION: in a case of unconstrained generic type parameter a runtime test (default(T) == null) would be needed
            // SPEC VIOLATION: However, for compatibility with Dev12 we will continue treating all generic type parameters, constrained or not,
            // SPEC VIOLATION: as value types.
            var variableRepresentsLocation = rewrittenReceiver.Type.IsValueType || rewrittenReceiver.Type.Kind == SymbolKind.TypeParameter;

            var receiverTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, refKind: variableRepresentsLocation ? RefKind.Ref : RefKind.None);
            stores.Add(assignmentToTemp);
            temps.Add(receiverTemp.LocalSymbol);

            // CONSIDER: this is a temporary object that will be rewritten away before this lowering completes.
            // Mitigation: this will only produce short-lived garbage for compound assignments and increments/decrements of properties.
            return new BoundPropertyAccess(prop.Syntax, receiverTemp, prop.PropertySymbol, prop.ResultKind, prop.Type);
        }

        private BoundDynamicMemberAccess TransformDynamicMemberAccess(BoundDynamicMemberAccess memberAccess, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            if (!CanChangeValueBetweenReads(memberAccess.Receiver))
            {
                return memberAccess;
            }

            // store receiver to temp:
            var rewrittenReceiver = VisitExpression(memberAccess.Receiver);
            BoundAssignmentOperator assignmentToTemp;
            var receiverTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp);
            stores.Add(assignmentToTemp);
            temps.Add(receiverTemp.LocalSymbol);

            return new BoundDynamicMemberAccess(memberAccess.Syntax, receiverTemp, memberAccess.TypeArgumentsOpt, memberAccess.Name, memberAccess.Invoked, memberAccess.Indexed, memberAccess.Type);
        }

        private BoundIndexerAccess TransformIndexerAccess(BoundIndexerAccess indexerAccess, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            var receiverOpt = indexerAccess.ReceiverOpt;
            Debug.Assert(receiverOpt != null);

            BoundExpression transformedReceiver;
            if (CanChangeValueBetweenReads(receiverOpt))
            {
                BoundExpression rewrittenReceiver = VisitExpression(receiverOpt);

                BoundAssignmentOperator assignmentToTemp;

                // SPEC VIOLATION: It is not very clear when receiver of constrained callvirt is dereferenced - when pushed (in lexical order),
                // SPEC VIOLATION: or when actual call is executed. The actual behavior seems to be implementation specific in different JITs.
                // SPEC VIOLATION: To not depend on that, the right thing to do here is to store the value of the variable 
                // SPEC VIOLATION: when variable has reference type (regular temp), and store variable's location when it has a value type. (ref temp)
                // SPEC VIOLATION: in a case of unconstrained generic type parameter a runtime test (default(T) == null) would be needed
                // SPEC VIOLATION: However, for compatibility with Dev12 we will continue treating all generic type parameters, constrained or not,
                // SPEC VIOLATION: as value types.
                var variableRepresentsLocation = rewrittenReceiver.Type.IsValueType || rewrittenReceiver.Type.Kind == SymbolKind.TypeParameter;

                var receiverTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, refKind: variableRepresentsLocation ? RefKind.Ref : RefKind.None);
                transformedReceiver = receiverTemp;
                stores.Add(assignmentToTemp);
                temps.Add(receiverTemp.LocalSymbol);
            }
            else
            {
                transformedReceiver = VisitExpression(receiverOpt);
            }

            // Dealing with the arguments is a bit tricky because they can be named out-of-order arguments;
            // we have to preserve both the source-code order of the side effects and the side effects
            // only being executed once.
            // 
            // This is a subtly different problem than the problem faced by the conventional call
            // rewriter; with the conventional call rewriter we already know that the side effects
            // will only be executed once because the arguments are only being pushed on the stack once. 
            // In a compound equality operator on an indexer the indices are placed on the stack twice. 
            // That is to say, if you have:
            // 
            // C().M(z : Z(), x : X(), y : Y())
            // 
            // then we can rewrite that into
            // 
            // tempc = C()
            // tempz = Z()
            // tempc.M(X(), Y(), tempz)
            // 
            // See, we can optimize away two of the temporaries, for x and y. But we cannot optimize away any of the
            // temporaries in
            // 
            // C().Collection[z : Z(), x : X(), y : Y()] += 1;
            // 
            // because we have to ensure not just that Z() happens first, but in addition that X() and Y() are only 
            // called once.  We have to generate this as
            // 
            // tempc = C().Collection
            // tempz = Z()
            // tempx = X()
            // tempy = Y()
            // tempc[tempx, tempy, tempz] = tempc[tempx, tempy, tempz] + 1;
            // 
            // Fortunately arguments to indexers are never ref or out, so we don't need to worry about that.
            // However, we can still do the optimization where constants are not stored in
            // temporaries; if we have
            // 
            // C().Collection[z : 123, y : Y(), x : X()] += 1;
            // 
            // Then we can generate that as
            // 
            // tempc = C().Collection
            // tempx = X()
            // tempy = Y()
            // tempc[tempx, tempy, 123] = tempc[tempx, tempy, 123] + 1;

            ImmutableArray<BoundExpression> rewrittenArguments = VisitList(indexerAccess.Arguments);

            CSharpSyntaxNode syntax = indexerAccess.Syntax;
            PropertySymbol indexer = indexerAccess.Indexer;
            ImmutableArray<RefKind> argumentRefKinds = indexerAccess.ArgumentRefKindsOpt;
            bool expanded = indexerAccess.Expanded;
            ImmutableArray<int> argsToParamsOpt = indexerAccess.ArgsToParamsOpt;

            ImmutableArray<ParameterSymbol> parameters = indexer.Parameters;
            BoundExpression[] actualArguments = new BoundExpression[parameters.Length]; // The actual arguments that will be passed; one actual argument per formal parameter.
            ArrayBuilder<BoundAssignmentOperator> storesToTemps = ArrayBuilder<BoundAssignmentOperator>.GetInstance(rewrittenArguments.Length);
            ArrayBuilder<RefKind> refKinds = ArrayBuilder<RefKind>.GetInstance(parameters.Length, RefKind.None);

            // Step one: Store everything that is non-trivial into a temporary; record the
            // stores in storesToTemps and make the actual argument a reference to the temp.
            // Do not yet attempt to deal with params arrays or optional arguments.
            BuildStoresToTemps(expanded, argsToParamsOpt, argumentRefKinds, rewrittenArguments, actualArguments, refKinds, storesToTemps);

            // Step two: If we have a params array, build the array and fill in the argument.
            if (expanded)
            {
                BoundExpression array = BuildParamsArray(syntax, indexer, argsToParamsOpt, rewrittenArguments, parameters, actualArguments[actualArguments.Length - 1]);
                BoundAssignmentOperator storeToTemp;
                var boundTemp = _factory.StoreToTemp(array, out storeToTemp);
                stores.Add(storeToTemp);
                temps.Add(boundTemp.LocalSymbol);
                actualArguments[actualArguments.Length - 1] = boundTemp;
            }

            // Step three: Now fill in the optional arguments. (Dev11 uses the
            // getter for optional arguments in compound assignments.)
            var getMethod = indexer.GetOwnOrInheritedGetMethod();
            Debug.Assert((object)getMethod != null);
            InsertMissingOptionalArguments(syntax, getMethod.Parameters, actualArguments);

            // For a call, step four would be to optimize away some of the temps.  However, we need them all to prevent
            // duplicate side-effects, so we'll skip that step.

            if (indexer.ContainingType.IsComImport)
            {
                RewriteArgumentsForComCall(parameters, actualArguments, refKinds, temps);
            }

            rewrittenArguments = actualArguments.AsImmutableOrNull();

            foreach (BoundAssignmentOperator tempAssignment in storesToTemps)
            {
                temps.Add(((BoundLocal)tempAssignment.Left).LocalSymbol);
                stores.Add(tempAssignment);
            }

            storesToTemps.Free();
            argumentRefKinds = GetRefKindsOrNull(refKinds);
            refKinds.Free();

            // CONSIDER: this is a temporary object that will be rewritten away before this lowering completes.
            // Mitigation: this will only produce short-lived garbage for compound assignments and increments/decrements of indexers.
            return new BoundIndexerAccess(
                syntax,
                transformedReceiver,
                indexer,
                rewrittenArguments,
                default(ImmutableArray<string>),
                argumentRefKinds,
                false,
                default(ImmutableArray<int>),
                indexerAccess.Type);
        }

        private BoundFieldAccess TransformReferenceTypeFieldAccess(BoundFieldAccess fieldAccess, BoundExpression receiver, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            Debug.Assert(receiver.Type.IsReferenceType);
            Debug.Assert(receiver.Kind != BoundKind.TypeExpression);
            BoundExpression rewrittenReceiver = VisitExpression(receiver);

            if (rewrittenReceiver.Type.IsTypeParameter())
            {
                var memberContainingType = fieldAccess.FieldSymbol.ContainingType;

                // From the verifier prospective type parameters do not contain fields or methods.
                // the instance must be "boxed" to access the field
                // It makes sense to box receiver before storing into a temp - no need to box twice.
                rewrittenReceiver = BoxReceiver(rewrittenReceiver, memberContainingType);
            }

            BoundAssignmentOperator assignmentToTemp;
            var receiverTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp);
            stores.Add(assignmentToTemp);
            temps.Add(receiverTemp.LocalSymbol);
            return new BoundFieldAccess(fieldAccess.Syntax, receiverTemp, fieldAccess.FieldSymbol, null);
        }

        private BoundDynamicIndexerAccess TransformDynamicIndexerAccess(BoundDynamicIndexerAccess indexerAccess, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            BoundExpression loweredReceiver;
            if (CanChangeValueBetweenReads(indexerAccess.ReceiverOpt))
            {
                BoundAssignmentOperator assignmentToTemp;
                var temp = _factory.StoreToTemp(VisitExpression(indexerAccess.ReceiverOpt), out assignmentToTemp);
                stores.Add(assignmentToTemp);
                temps.Add(temp.LocalSymbol);
                loweredReceiver = temp;
            }
            else
            {
                loweredReceiver = indexerAccess.ReceiverOpt;
            }

            var arguments = indexerAccess.Arguments;
            var loweredArguments = new BoundExpression[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                if (CanChangeValueBetweenReads(arguments[i]))
                {
                    BoundAssignmentOperator assignmentToTemp;
                    var temp = _factory.StoreToTemp(VisitExpression(arguments[i]), out assignmentToTemp, refKind: indexerAccess.ArgumentRefKindsOpt.RefKinds(i));
                    stores.Add(assignmentToTemp);
                    temps.Add(temp.LocalSymbol);
                    loweredArguments[i] = temp;
                }
                else
                {
                    loweredArguments[i] = arguments[i];
                }
            }

            return new BoundDynamicIndexerAccess(
                indexerAccess.Syntax,
                loweredReceiver,
                loweredArguments.AsImmutableOrNull(),
                indexerAccess.ArgumentNamesOpt,
                indexerAccess.ArgumentRefKindsOpt,
                indexerAccess.ApplicableIndexers,
                indexerAccess.Type);
        }

        /// <summary>
        /// In the expanded form of a compound assignment (or increment/decrement), the LHS appears multiple times.
        /// If we aren't careful, this can result in repeated side-effects.  This creates (ordered) temps for all of the
        /// subexpressions that could result in side-effects and returns a side-effect-free expression that can be used
        /// in place of the LHS in the expanded form.
        /// </summary>
        /// <param name="originalLHS">The LHS sub-expression of the compound assignment (or increment/decrement).</param>
        /// <param name="stores">Populated with a list of assignment expressions that initialize the temporary locals.</param>
        /// <param name="temps">Populated with a list of temporary local symbols.</param>
        /// <param name="isDynamicAssignment">True if the compound assignment is a dynamic operation.</param>
        /// <returns>
        /// A side-effect-free expression representing the LHS.
        /// The returned node needs to be lowered but its children are already lowered.
        /// </returns>
        private BoundExpression TransformCompoundAssignmentLHS(BoundExpression originalLHS, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps, bool isDynamicAssignment)
        {
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

            switch (originalLHS.Kind)
            {
                case BoundKind.PropertyAccess:
                    {
                        // Ref returning properties count as variables and do not undergo the transformation
                        // that value returning propertues require.
                        var propertyAccess = (BoundPropertyAccess)originalLHS;
                        if (propertyAccess.PropertySymbol.RefKind == RefKind.None)
                        {
                            return TransformPropertyAccess(propertyAccess, stores, temps);
                        }
                    }
                    break;

                case BoundKind.IndexerAccess:
                    {
                        // Ref returning indexers count as variables and do not undergo the transformation
                        // that value returning propertues require.
                        var indexerAccess = (BoundIndexerAccess)originalLHS;
                        if (indexerAccess.Indexer.RefKind == RefKind.None)
                        {
                            return TransformIndexerAccess((BoundIndexerAccess)originalLHS, stores, temps);
                        }
                    }
                    break;

                case BoundKind.FieldAccess:
                    {
                        // * If the field is static then no temporaries are needed. 
                        // * If the field is not static and the receiver is of reference type then generate t = r; t.f = t.f + value
                        // * If the field is not static and the receiver is a variable of value type then we'll fall into the
                        //   general variable case below.

                        var fieldAccess = (BoundFieldAccess)originalLHS;
                        BoundExpression receiverOpt = fieldAccess.ReceiverOpt;

                        //If the receiver is static or is the receiver is of kind "Base" or "this", then we can just generate field = field + value
                        if (fieldAccess.FieldSymbol.IsStatic || !CanChangeValueBetweenReads(receiverOpt))
                        {
                            return fieldAccess;
                        }
                        else if (receiverOpt.Type.IsReferenceType)
                        {
                            return TransformReferenceTypeFieldAccess(fieldAccess, receiverOpt, stores, temps);
                        }
                    }
                    break;

                case BoundKind.ArrayAccess:
                    if (isDynamicAssignment)
                    {
                        // In non-dynamic array[index] op= R we emit:
                        //   T& tmp = &array[index];
                        //   *tmp = *L op R;
                        // where T is the type of L.
                        // 
                        // If L is an array access, the assignment is dynamic, the compile-time of the array is dynamic[] 
                        // and the runtime type of the array is not object[] (but e.g. string[]) the pointer approach is broken.
                        // T is Object in such case and we can't take a read-write pointer of type Object& to an array element of non-object type.
                        //
                        // In this case we rewrite the assignment as follows:
                        //
                        //   E t_array = array;
                        //   I t_index = index; (possibly more indices)
                        //   T value = t_array[t_index];
                        //   t_array[t_index] = value op R;

                        var arrayAccess = (BoundArrayAccess)originalLHS;
                        var loweredArray = VisitExpression(arrayAccess.Expression);
                        var loweredIndices = VisitList(arrayAccess.Indices);

                        return SpillArrayElementAccess(loweredArray, loweredIndices, stores, temps);
                    }
                    break;

                case BoundKind.DynamicMemberAccess:
                    return TransformDynamicMemberAccess((BoundDynamicMemberAccess)originalLHS, stores, temps);

                case BoundKind.DynamicIndexerAccess:
                    return TransformDynamicIndexerAccess((BoundDynamicIndexerAccess)originalLHS, stores, temps);

                case BoundKind.Local:
                case BoundKind.Parameter:
                case BoundKind.ThisReference: // a special kind of parameter
                    // No temporaries are needed. Just generate local = local + value
                    return originalLHS;

                case BoundKind.Call:
                    Debug.Assert(((BoundCall)originalLHS).Method.RefKind != RefKind.None);
                    break;

                case BoundKind.AssignmentOperator:
                    Debug.Assert(((BoundAssignmentOperator)originalLHS).RefKind != RefKind.None);
                    break;

                case BoundKind.PointerElementAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(originalLHS.Kind);
            }

            // We made no transformation above. Either we have array[index] += value, 
            // structVariable.field += value, or ref-returning call += value; in all cases
            // way we have a potentially complicated variable-producing expression on the
            // left. Generate ref temp = ref variable; temp = temp + value

            // Rewrite the variable.  Here we depend on the fact that the only forms
            // rewritten here are rewritten the same for lvalues and rvalues.
            BoundExpression rewrittenVariable = VisitExpression(originalLHS);

            BoundAssignmentOperator assignmentToTemp2;
            var variableTemp = _factory.StoreToTemp(rewrittenVariable, out assignmentToTemp2, refKind: RefKind.Ref);
            stores.Add(assignmentToTemp2);
            temps.Add(variableTemp.LocalSymbol);
            return variableTemp;
        }

        private BoundExpression BoxReceiver(BoundExpression rewrittenReceiver, NamedTypeSymbol memberContainingType)
        {
            return MakeConversion(
                rewrittenReceiver.Syntax,
                rewrittenReceiver,
                ConversionKind.Boxing,
                memberContainingType,
                @checked: false,
                constantValueOpt: rewrittenReceiver.ConstantValue);
        }

        private BoundExpression SpillArrayElementAccess(
            BoundExpression loweredExpression,
            ImmutableArray<BoundExpression> loweredIndices,
            ArrayBuilder<BoundExpression> stores,
            ArrayBuilder<LocalSymbol> temps)
        {
            BoundAssignmentOperator assignmentToArrayTemp;
            var arrayTemp = _factory.StoreToTemp(loweredExpression, out assignmentToArrayTemp);
            stores.Add(assignmentToArrayTemp);
            temps.Add(arrayTemp.LocalSymbol);
            var boundTempArray = arrayTemp;

            var boundTempIndices = new BoundExpression[loweredIndices.Length];
            for (int i = 0; i < boundTempIndices.Length; i++)
            {
                if (CanChangeValueBetweenReads(loweredIndices[i]))
                {
                    BoundAssignmentOperator assignmentToTemp;
                    var temp = _factory.StoreToTemp(loweredIndices[i], out assignmentToTemp);
                    stores.Add(assignmentToTemp);
                    temps.Add(temp.LocalSymbol);
                    boundTempIndices[i] = temp;
                }
                else
                {
                    boundTempIndices[i] = loweredIndices[i];
                }
            }

            return _factory.ArrayAccess(boundTempArray, boundTempIndices);
        }

        /// <summary>
        /// Variables local to current frame do not need temps when re-read multiple times
        /// as long as there is no code that may write to locals in between accesses and they
        /// are not captured.
        /// 
        /// Example:
        ///        l += foo(ref l);
        /// 
        /// even though l is a local, we must access it via a temp since "foo(ref l)" may change it
        /// on between accesses. 
        /// </summary>
        internal static bool CanChangeValueBetweenReads(
            BoundExpression expression,
            bool localsMayBeAssignedOrCaptured = true)
        {
            if (expression.IsDefaultValue())
            {
                return false;
            }

            if (expression.ConstantValue != null)
            {
                var type = expression.Type;
                return !ConstantValueIsTrivial(type);
            }

            switch (expression.Kind)
            {
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                    return false;

                case BoundKind.Literal:
                    var type = expression.Type;
                    return !ConstantValueIsTrivial(type);

                case BoundKind.Parameter:
                    return localsMayBeAssignedOrCaptured || ((BoundParameter)expression).ParameterSymbol.RefKind != RefKind.None;

                case BoundKind.Local:
                    return localsMayBeAssignedOrCaptured || ((BoundLocal)expression).LocalSymbol.RefKind != RefKind.None;

                default:
                    return true;
            }
        }

        // a simple check for common non-side-effecting expressions
        internal static bool ReadIsSideeffecting(
            BoundExpression expression)
        {
            if (expression.ConstantValue != null)
            {
                return false;
            }

            if (expression.IsDefaultValue())
            {
                return false;
            }

            switch (expression.Kind)
            {
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.Literal:
                case BoundKind.Parameter:
                case BoundKind.Local:
                case BoundKind.Lambda:
                    return false;

                case BoundKind.Conversion:
                    var conv = (BoundConversion)expression;
                    return conv.ConversionHasSideEffects() ||
                        ReadIsSideeffecting(conv.Operand);

                case BoundKind.ObjectCreationExpression:
                    // common production of lowered conversions to nullable
                    // new S?(arg)
                    if (expression.Type.IsNullableType())
                    {
                        var objCreation = (BoundObjectCreationExpression)expression;
                        return objCreation.Arguments.Length == 1 && ReadIsSideeffecting(objCreation.Arguments[0]);
                    }

                    return false;

                default:
                    return true;
            }
        }

        // nontrivial literals do not change between reads
        // but may require re-constructing, so it is better 
        // to treat them as potentially changing.
        private static bool ConstantValueIsTrivial(TypeSymbol type)
        {
            return (object)type == null ||
                type.SpecialType.IsClrInteger() ||
                type.IsReferenceType ||
                type.IsEnumType();
        }

    }
}
