// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
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
            Debug.Assert(TypeSymbol.Equals(node.Right.Type, node.Operator.RightType, TypeCompareKind.ConsiderEverything2));
            BoundExpression loweredRight = VisitExpression(node.Right);

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();

            var kind = node.Operator.Kind;
            bool isChecked = kind.IsChecked();
            bool isDynamic = kind.IsDynamic();
            var binaryOperator = kind.Operator();

            // This will be filled in with the LHS that uses temporaries to prevent
            // double-evaluation of side effects.
            BoundExpression transformedLHS = TransformCompoundAssignmentLHS(node.Left, stores, temps, isDynamic);
            var lhsRead = MakeRValue(transformedLHS);
            BoundExpression rewrittenAssignment;

            if (node.Left.Kind == BoundKind.DynamicMemberAccess &&
                (binaryOperator == BinaryOperatorKind.Addition || binaryOperator == BinaryOperatorKind.Subtraction))
            {
                // If this could be an event assignment at runtime, we need to rewrite to the following form:
                // Original:
                //   receiver.EV += handler
                // Rewritten:
                //   dynamic memberAccessReceiver = receiver;
                //   bool isEvent = Runtime.IsEvent(memberAccessReceiver, "EV");
                //   dynamic storeNonEvent = !isEvent ? memberAccessReceiver.EV : null;
                //   var loweredRight = handler; // Only necessary if handler can change values, or is something like a lambda
                //   isEvent ? add_Event(memberAccessReceiver, "EV", loweredRight) : transformedLHS = storeNonEvent + loweredRight;
                //
                // This is to ensure that if handler is something like a lambda, we evaluate fully evaluate the left
                // side before storing the lambda to a temp for use in both possible branches.
                // The first store to memberAccessReceiver has already been taken care of above by TransformCompoundAssignmentLHS

                var eventTemps = ArrayBuilder<LocalSymbol>.GetInstance();
                var sequence = ArrayBuilder<BoundExpression>.GetInstance();

                //   dynamic memberAccessReceiver = receiver;
                var memberAccess = (BoundDynamicMemberAccess)transformedLHS;

                //   bool isEvent = Runtime.IsEvent(memberAccessReceiver, "EV");
                var isEvent = _factory.StoreToTemp(_dynamicFactory.MakeDynamicIsEventTest(memberAccess.Name, memberAccess.Receiver).ToExpression(), out BoundAssignmentOperator isEventAssignment);
                eventTemps.Add(isEvent.LocalSymbol);
                sequence.Add(isEventAssignment);

                // dynamic storeNonEvent = !isEvent ? memberAccessReceiver.EV : null;
                lhsRead = _factory.StoreToTemp(lhsRead, out BoundAssignmentOperator receiverAssignment);
                eventTemps.Add(((BoundLocal)lhsRead).LocalSymbol);
                var storeNonEvent = _factory.StoreToTemp(_factory.Conditional(_factory.Not(isEvent), receiverAssignment, _factory.Null(receiverAssignment.Type), receiverAssignment.Type), out BoundAssignmentOperator nonEventStore);
                eventTemps.Add(storeNonEvent.LocalSymbol);
                sequence.Add(nonEventStore);

                // var loweredRight = handler;
                if (CanChangeValueBetweenReads(loweredRight))
                {
                    loweredRight = _factory.StoreToTemp(loweredRight, out BoundAssignmentOperator possibleHandlerAssignment);
                    eventTemps.Add(((BoundLocal)loweredRight).LocalSymbol);
                    sequence.Add(possibleHandlerAssignment);
                }

                // add_Event(t1, "add_EV");
                var invokeEventAccessor = _dynamicFactory.MakeDynamicEventAccessorInvocation(
                    (binaryOperator == BinaryOperatorKind.Addition ? "add_" : "remove_") + memberAccess.Name,
                    memberAccess.Receiver,
                    loweredRight);

                // transformedLHS = storeNonEvent + loweredRight
                rewrittenAssignment = rewriteAssignment(lhsRead);

                // Final conditional
                var condition = _factory.Conditional(isEvent, invokeEventAccessor.ToExpression(), rewrittenAssignment, rewrittenAssignment.Type);

                rewrittenAssignment = new BoundSequence(node.Syntax, eventTemps.ToImmutableAndFree(), sequence.ToImmutableAndFree(), condition, condition.Type);
            }
            else
            {
                rewrittenAssignment = rewriteAssignment(lhsRead);
            }

            BoundExpression result = (temps.Count == 0 && stores.Count == 0) ?
                rewrittenAssignment :
                new BoundSequence(
                    node.Syntax,
                    temps.ToImmutable(),
                    stores.ToImmutable(),
                    rewrittenAssignment,
                    rewrittenAssignment.Type);

            temps.Free();
            stores.Free();
            return result;

            BoundExpression rewriteAssignment(BoundExpression leftRead)
            {
                SyntaxNode syntax = node.Syntax;

                // OK, we now have the temporary declarations, the temporary stores, and the transformed left hand side.
                // We need to generate
                //
                // xlhs = (FINAL)((LEFT)xlhs op rhs)
                //
                // And then wrap it up with the generated temporaries.
                //
                // (The right hand side has already been converted to the type expected by the operator.)

                BoundExpression opLHS = isDynamic ? leftRead : MakeConversionNode(
                    syntax: syntax,
                    rewrittenOperand: leftRead,
                    conversion: node.LeftConversion,
                    rewrittenType: node.Operator.LeftType,
                    @checked: isChecked);

                BoundExpression operand = MakeBinaryOperator(syntax, node.Operator.Kind, opLHS, loweredRight, node.Operator.ReturnType, node.Operator.Method, isCompoundAssignment: true);

                BoundExpression opFinal = MakeConversionNode(
                    syntax: syntax,
                    rewrittenOperand: operand,
                    conversion: node.FinalConversion,
                    rewrittenType: node.Left.Type,
                    explicitCastInCode: isDynamic,
                    @checked: isChecked);

                return MakeAssignmentOperator(syntax, transformedLHS, opFinal, node.Left.Type, used: used, isChecked: isChecked, isCompoundAssignment: true);
            }
        }

        private BoundExpression TransformPropertyOrEventReceiver(Symbol propertyOrEvent, BoundExpression receiverOpt, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            Debug.Assert(propertyOrEvent.Kind == SymbolKind.Property || propertyOrEvent.Kind == SymbolKind.Event);

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
            if (receiverOpt == null || propertyOrEvent.IsStatic || !CanChangeValueBetweenReads(receiverOpt))
            {
                return receiverOpt;
            }

            Debug.Assert(receiverOpt.Kind != BoundKind.TypeExpression);

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
            stores.Add(assignmentToTemp);
            temps.Add(receiverTemp.LocalSymbol);

            return receiverTemp;
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

            SyntaxNode syntax = indexerAccess.Syntax;
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
            BuildStoresToTemps(
                expanded,
                argsToParamsOpt,
                parameters,
                argumentRefKinds,
                rewrittenArguments,
                forceLambdaSpilling: true, // lambdas must produce exactly one delegate so they must be spilled into a temp
                actualArguments,
                refKinds,
                storesToTemps);

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

            // Step three: Now fill in the optional arguments. (Dev11 uses the getter for optional arguments in
            // compound assignments, but for deconstructions we use the setter if the getter is missing.)
            var accessor = indexer.GetOwnOrInheritedGetMethod() ?? indexer.GetOwnOrInheritedSetMethod();
            InsertMissingOptionalArguments(syntax, accessor.Parameters, actualArguments, refKinds);

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

            // This is a temporary object that will be rewritten away before the lowering completes.
            return new BoundIndexerAccess(
                syntax,
                transformedReceiver,
                indexer,
                rewrittenArguments,
                default(ImmutableArray<string>),
                argumentRefKinds,
                false,
                default(ImmutableArray<int>),
                null,
                indexerAccess.UseSetterForDefaultArgumentGeneration,
                indexerAccess.Type);
        }

        private BoundExpression TransformPatternIndexerAccess(
            BoundIndexOrRangePatternIndexerAccess indexerAccess,
            ArrayBuilder<BoundExpression> stores,
            ArrayBuilder<LocalSymbol> temps,
            bool isDynamicAssignment)
        {
            // A pattern indexer is fundamentally a sequence which ends in either
            // a conventional indexer access or a method call. The lowering of a
            // pattern indexer already lowers everything we need into temps, so
            // the only thing we need to do is lift the stores and temps out of
            // the sequence, and use the final expression as the new argument

            var sequence = VisitIndexOrRangePatternIndexerAccess(indexerAccess, isLeftOfAssignment: true);
            stores.AddRange(sequence.SideEffects);
            temps.AddRange(sequence.Locals);
            return TransformCompoundAssignmentLHS(sequence.Value, stores, temps, isDynamicAssignment);
        }

        /// <summary>
        /// Returns true if the <paramref name="receiver"/> was lowered and transformed.
        /// The <paramref name="receiver"/> is not changed if this function returns false. 
        /// </summary>
        private bool TransformCompoundAssignmentFieldOrEventAccessReceiver(Symbol fieldOrEvent, ref BoundExpression receiver, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            Debug.Assert(fieldOrEvent.Kind == SymbolKind.Field || fieldOrEvent.Kind == SymbolKind.Event);

            //If the receiver is static or is the receiver is of kind "Base" or "this", then we can just generate field = field + value
            if (fieldOrEvent.IsStatic || !CanChangeValueBetweenReads(receiver))
            {
                return true;
            }
            else if (!receiver.Type.IsReferenceType)
            {
                return false;
            }

            Debug.Assert(receiver.Type.IsReferenceType);
            Debug.Assert(receiver.Kind != BoundKind.TypeExpression);
            BoundExpression rewrittenReceiver = VisitExpression(receiver);

            if (rewrittenReceiver.Type.IsTypeParameter())
            {
                var memberContainingType = fieldOrEvent.ContainingType;

                // From the verifier perspective type parameters do not contain fields or methods.
                // the instance must be "boxed" to access the field
                // It makes sense to box receiver before storing into a temp - no need to box twice.
                rewrittenReceiver = BoxReceiver(rewrittenReceiver, memberContainingType);
            }

            BoundAssignmentOperator assignmentToTemp;
            var receiverTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp);
            stores.Add(assignmentToTemp);
            temps.Add(receiverTemp.LocalSymbol);
            receiver = receiverTemp;
            return true;
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
                    var temp = _factory.StoreToTemp(VisitExpression(arguments[i]), out assignmentToTemp, indexerAccess.ArgumentRefKindsOpt.RefKinds(i) != RefKind.None ? RefKind.Ref : RefKind.None);
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
                        // that value returning properties require.
                        var propertyAccess = (BoundPropertyAccess)originalLHS;
                        if (propertyAccess.PropertySymbol.RefKind == RefKind.None)
                        {
                            // This is a temporary object that will be rewritten away before the lowering completes.
                            return propertyAccess.Update(TransformPropertyOrEventReceiver(propertyAccess.PropertySymbol, propertyAccess.ReceiverOpt, stores, temps),
                                                         propertyAccess.PropertySymbol, propertyAccess.ResultKind, propertyAccess.Type);
                        }
                    }
                    break;

                case BoundKind.IndexerAccess:
                    {
                        // Ref returning indexers count as variables and do not undergo the transformation
                        // that value returning properties require.
                        var indexerAccess = (BoundIndexerAccess)originalLHS;
                        if (indexerAccess.Indexer.RefKind == RefKind.None)
                        {
                            return TransformIndexerAccess((BoundIndexerAccess)originalLHS, stores, temps);
                        }
                    }
                    break;

                case BoundKind.IndexOrRangePatternIndexerAccess:
                    {
                        var patternIndexerAccess = (BoundIndexOrRangePatternIndexerAccess)originalLHS;
                        (RefKind refKind, bool isRange) = patternIndexerAccess.PatternSymbol switch
                        {
                            PropertySymbol { RefKind: var r } => (r, false),
                            MethodSymbol { RefKind: var r } => (r, true),
                            var x => throw ExceptionUtilities.UnexpectedValue(x)
                        };
                        if (refKind == RefKind.None)
                        {
                            return TransformPatternIndexerAccess(patternIndexerAccess, stores, temps, isDynamicAssignment);
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

                        if (TransformCompoundAssignmentFieldOrEventAccessReceiver(fieldAccess.FieldSymbol, ref receiverOpt, stores, temps))
                        {
                            return MakeFieldAccess(fieldAccess.Syntax, receiverOpt, fieldAccess.FieldSymbol, fieldAccess.ConstantValueOpt, fieldAccess.ResultKind, fieldAccess.Type, fieldAccess);
                        }
                    }
                    break;

                case BoundKind.ArrayAccess:
                    {
                        var arrayAccess = (BoundArrayAccess)originalLHS;
                        if (isDynamicAssignment || !IsInvariantArray(arrayAccess.Expression.Type))
                        {
                            // In non-dynamic, invariant array[index] op= R we emit:
                            //   T& tmp = &array[index];
                            //   *tmp = *L op R;
                            // where T is the type of L.
                            // 
                            // If L is an array access, the assignment is dynamic, the compile-time of the array is dynamic[] 
                            // and the runtime type of the array is not object[] (but e.g. string[]) the pointer approach is broken.
                            // T is Object in such case and we can't take a read-write pointer of type Object& to an array element of non-object type.
                            //
                            // In the dynamic case, or when the array may be co-variant, we rewrite the assignment as follows:
                            //
                            //   E t_array = array;
                            //   I t_index = index; (possibly more indices)
                            //   T value = t_array[t_index];
                            //   t_array[t_index] = value op R;
                            var loweredArray = VisitExpression(arrayAccess.Expression);
                            var loweredIndices = VisitList(arrayAccess.Indices);

                            return SpillArrayElementAccess(loweredArray, loweredIndices, stores, temps);
                        }
                    }
                    break;

                case BoundKind.DynamicMemberAccess:
                    return TransformDynamicMemberAccess((BoundDynamicMemberAccess)originalLHS, stores, temps);

                case BoundKind.DynamicIndexerAccess:
                    return TransformDynamicIndexerAccess((BoundDynamicIndexerAccess)originalLHS, stores, temps);

                case BoundKind.Local:
                case BoundKind.Parameter:
                case BoundKind.ThisReference: // a special kind of parameter
                case BoundKind.PseudoVariable:
                    // No temporaries are needed. Just generate local = local + value
                    return originalLHS;

                case BoundKind.Call:
                    Debug.Assert(((BoundCall)originalLHS).Method.RefKind != RefKind.None);
                    break;

                case BoundKind.ConditionalOperator:
                    Debug.Assert(((BoundConditionalOperator)originalLHS).IsRef);
                    break;

                case BoundKind.AssignmentOperator:
                    Debug.Assert(((BoundAssignmentOperator)originalLHS).IsRef);
                    break;

                case BoundKind.PointerElementAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                    break;

                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)originalLHS;
                        Debug.Assert(eventAccess.IsUsableAsField);
                        BoundExpression receiverOpt = eventAccess.ReceiverOpt;

                        if (eventAccess.EventSymbol.IsWindowsRuntimeEvent)
                        {
                            // This is a temporary object that will be rewritten away before the lowering completes.
                            return eventAccess.Update(TransformPropertyOrEventReceiver(eventAccess.EventSymbol, eventAccess.ReceiverOpt, stores, temps),
                                                      eventAccess.EventSymbol, eventAccess.IsUsableAsField, eventAccess.ResultKind, eventAccess.Type);
                        }

                        if (TransformCompoundAssignmentFieldOrEventAccessReceiver(eventAccess.EventSymbol, ref receiverOpt, stores, temps))
                        {
                            return MakeEventAccess(eventAccess.Syntax, receiverOpt, eventAccess.EventSymbol, eventAccess.ConstantValue, eventAccess.ResultKind, eventAccess.Type);
                        }
                    }
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

        private static bool IsInvariantArray(TypeSymbol type)
        {
            return (type as ArrayTypeSymbol)?.ElementType.IsSealed == true;
        }

        private BoundExpression BoxReceiver(BoundExpression rewrittenReceiver, NamedTypeSymbol memberContainingType)
        {
            return MakeConversionNode(
                rewrittenReceiver.Syntax,
                rewrittenReceiver,
                Conversion.Boxing,
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
        ///        l += goo(ref l);
        /// 
        /// even though l is a local, we must access it via a temp since "goo(ref l)" may change it
        /// on between accesses.
        ///
        /// Note: In <c>this.x++</c>, <c>this</c> cannot change between reads. But in <c>(this, ...) == (..., this.Mutate())</c> it can.
        /// </summary>
        internal static bool CanChangeValueBetweenReads(
            BoundExpression expression,
            bool localsMayBeAssignedOrCaptured = true,
            bool structThisCanChangeValueBetweenReads = false)
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
                    return structThisCanChangeValueBetweenReads && ((BoundThisReference)expression).Type.IsStructType();

                case BoundKind.BaseReference:
                    return false;

                case BoundKind.Literal:
                    var type = expression.Type;
                    return !ConstantValueIsTrivial(type);

                case BoundKind.Parameter:
                    return localsMayBeAssignedOrCaptured || ((BoundParameter)expression).ParameterSymbol.RefKind != RefKind.None;

                case BoundKind.Local:
                    return localsMayBeAssignedOrCaptured || ((BoundLocal)expression).LocalSymbol.RefKind != RefKind.None;

                case BoundKind.TypeExpression:
                    return false;

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

                case BoundKind.PassByCopy:
                    return ReadIsSideeffecting(((BoundPassByCopy)expression).Expression);

                case BoundKind.ObjectCreationExpression:
                    // common production of lowered conversions to nullable
                    // new S?(arg)
                    if (expression.Type.IsNullableType())
                    {
                        var objCreation = (BoundObjectCreationExpression)expression;
                        return objCreation.Arguments.Length == 1 && ReadIsSideeffecting(objCreation.Arguments[0]);
                    }

                    return true;

                case BoundKind.Call:
                    var call = (BoundCall)expression;
                    var method = call.Method;

                    // common production of lowered lifted operators
                    // GetValueOrDefault is known to be not sideeffecting.
                    if (method.ContainingType?.IsNullableType() == true)
                    {
                        if (IsSpecialMember(method, SpecialMember.System_Nullable_T_GetValueOrDefault) ||
                            IsSpecialMember(method, SpecialMember.System_Nullable_T_get_HasValue))
                        {
                            return ReadIsSideeffecting(call.ReceiverOpt);
                        }
                    }

                    return true;

                default:
                    return true;
            }
        }

        private static bool IsSpecialMember(MethodSymbol method, SpecialMember specialMember)
        {
            method = method.OriginalDefinition;
            return method.ContainingAssembly?.GetSpecialTypeMember(specialMember) == method;
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
