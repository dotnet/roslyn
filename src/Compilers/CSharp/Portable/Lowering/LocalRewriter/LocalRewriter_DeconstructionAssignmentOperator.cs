// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            var right = node.Right;
            Debug.Assert(right.Conversion.Kind == ConversionKind.Deconstruction);
            Debug.Assert(node.Type.IsTupleType);
            return RewriteDeconstruction(node.Left, right.Conversion, right.Operand, node.IsUsed, (NamedTypeSymbol)node.Type);
        }

        /// <summary>
        /// The left represents a tree of L-values. The structure of right can be missing parts of the tree on the left.
        /// The conversion holds nested conversions and deconstruction information, which matches the tree from the left,
        /// and it provides the information to fill in the missing parts of the tree from the right and convert it to
        /// the tree from the left.
        ///
        /// A bound sequence is returned which has different phases of side-effects:
        /// - the initialization phase includes side-effects from the left, followed by evaluations of the right
        /// - the deconstruction phase includes all the invocations of Deconstruct methods and tuple element accesses below a Deconstruct call
        /// - the conversion phase
        /// - the assignment phase
        /// </summary>
        private BoundExpression? RewriteDeconstruction(BoundTupleExpression left, Conversion conversion, BoundExpression right, bool isUsed, NamedTypeSymbol assignmentResultTupleType)
        {
            var lhsTemps = ArrayBuilder<LocalSymbol>.GetInstance();
            var lhsEffects = ArrayBuilder<BoundExpression>.GetInstance();
            ArrayBuilder<Binder.DeconstructionVariable> lhsTargets = GetAssignmentTargetsAndSideEffects(left, lhsTemps, lhsEffects);
            BoundExpression? result = RewriteDeconstruction(lhsTargets, conversion, right, assignmentResultTupleType, isUsed);
            Binder.DeconstructionVariable.FreeDeconstructionVariables(lhsTargets);
            if (result is null)
            {
                lhsTemps.Free();
                lhsEffects.Free();
                return null;
            }

            return _factory.Sequence(lhsTemps.ToImmutableAndFree(), lhsEffects.ToImmutableAndFree(), result);
        }

        private BoundExpression? RewriteDeconstruction(
            ArrayBuilder<Binder.DeconstructionVariable> lhsTargets,
            Conversion conversion,
            BoundExpression right,
            NamedTypeSymbol assignmentResultTupleType,
            bool isUsed)
        {
            if (right.Kind == BoundKind.ConditionalOperator)
            {
                var conditional = (BoundConditionalOperator)right;
                Debug.Assert(!conditional.IsRef);
                return conditional.Update(
                    conditional.IsRef,
                    VisitExpression(conditional.Condition),
                    RewriteDeconstruction(lhsTargets, conversion, conditional.Consequence, assignmentResultTupleType, isUsed: true)!,
                    RewriteDeconstruction(lhsTargets, conversion, conditional.Alternative, assignmentResultTupleType, isUsed: true)!,
                    conditional.ConstantValueOpt,
                    assignmentResultTupleType,
                    wasTargetTyped: true,
                    assignmentResultTupleType);
            }

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var effects = DeconstructionSideEffects.GetInstance();
            BoundExpression? returnValue = ApplyDeconstructionConversion(lhsTargets, right, conversion, temps, effects, assignmentResultTupleType, isUsed, inInit: true);
            reverseAssignmentsToTargetsIfApplicable();

            effects.Consolidate();

            if (!isUsed)
            {
                // When a deconstruction is not used, the last effect is used as return value
                Debug.Assert(returnValue is null);
                var last = effects.PopLast();
                if (last is null)
                {
                    temps.Free();
                    effects.Free();
                    // Deconstructions with no effects lower to nothing. For example, `(_, _) = (1, 2);`
                    return null;
                }

                return _factory.Sequence(temps.ToImmutableAndFree(), effects.ToImmutableAndFree(), last);
            }
            else
            {
                if (!returnValue!.HasErrors)
                {
                    returnValue = VisitExpression(returnValue);
                }

                return _factory.Sequence(temps.ToImmutableAndFree(), effects.ToImmutableAndFree(), returnValue);
            }

            // Optimize a deconstruction assignment by reversing the order that we store to the final variables.
            void reverseAssignmentsToTargetsIfApplicable()
            {
                PooledHashSet<Symbol>? visitedSymbols = null;

                Debug.Assert(right is not ({ Kind: BoundKind.TupleLiteral } or BoundConversion { Operand.Kind: BoundKind.TupleLiteral }));
                // Here are the general requirements for performing the optimization:
                if (// - the RHS is a tuple literal (which means the temps produced for this assignment are for the tuple elements, which could turn into push-pops into the destination variables)
                    right is { Kind: BoundKind.ConvertedTupleLiteral } or BoundConversion { Operand.Kind: BoundKind.ConvertedTupleLiteral }

                    // - at least one element in the RHS is actually stored to a temp. i.e. it is not a constant expression.
                    && effects.init.Any()

                    // - all variables on the LHS are unique, by-value, and are locals or parameters.
                    //     - Note that this could be expanded into fields of non-nullable value types at some point, but we decided not to invest in that at this time.
                    && canReorderTargetAssignments(lhsTargets, ref visitedSymbols))
                {
                    // Consider a deconstruction assignment like the following:
                    // (a, b, c) = (x, y, z);

                    // (x, y, z) are evaluated into temps, then the temps are stored to the targets:
                    // temp1 = x;
                    // temp2 = y;
                    // temp3 = z;
                    // a = temp1;
                    // b = temp2;
                    // c = temp3;

                    // As an optimization, ensure that assignments from temps to targets happen in the reverse order of effects:
                    // temp1 = x;
                    // temp2 = y;
                    // temp3 = z;
                    // c = temp3;
                    // b = temp2;
                    // a = temp1;

                    // This makes it more likely that the stack optimizer pass will be able to eliminate the temps and replace them with stack push/pops.
                    effects.assignments.ReverseContents();
                }

                visitedSymbols?.Free();
            }

            static bool canReorderTargetAssignments(ArrayBuilder<Binder.DeconstructionVariable> targets, ref PooledHashSet<Symbol>? visitedSymbols)
            {
                // If we know all targets refer to distinct variables, then we can reorder the assignments.
                // We avoid doing this in any cases where aliasing could occur, e.g.:
                // var y = 1;
                // ref var x = ref y;
                // (x, y) = (a, b);

                foreach (var target in targets)
                {
                    Debug.Assert(target is { Single: not null, NestedVariables: null } or { Single: null, NestedVariables: not null });
                    if (target.Single is { } single)
                    {
                        Symbol? symbol;
                        switch (single)
                        {
                            case BoundLocal { LocalSymbol: { RefKind: RefKind.None } localSymbol }:
                                symbol = localSymbol;
                                break;
                            case BoundParameter { ParameterSymbol: { RefKind: RefKind.None } parameterSymbol }:
                                Debug.Assert(!IsCapturedPrimaryConstructorParameter(single));
                                symbol = parameterSymbol;
                                break;
                            case BoundDiscardExpression:
                                // we don't care in what order we assign to these.
                                continue;
                            default:
                                // This deconstruction assigns to a target which is not sufficiently simple.
                                // We can't verify that the deconstruction does not use any aliases to variables.
                                return false;
                        }

                        visitedSymbols ??= PooledHashSet<Symbol>.GetInstance();
                        if (!visitedSymbols.Add(symbol))
                        {
                            // This deconstruction writes to the same target multiple times, e.g:
                            // (x, x) = (a, b);
                            return false;
                        }
                    }
                    else if (!canReorderTargetAssignments(target.NestedVariables!, ref visitedSymbols))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// This method recurses through leftTargets, right and conversion at the same time.
        /// As it does, it collects side-effects into the proper buckets (init, deconstructions, conversions, assignments).
        ///
        /// The side-effects from the right initially go into the init bucket. But once we started drilling into a Deconstruct
        /// invocation, subsequent side-effects from the right go into the deconstructions bucket (otherwise they would
        /// be evaluated out of order).
        /// </summary>
        private BoundExpression? ApplyDeconstructionConversion(
            ArrayBuilder<Binder.DeconstructionVariable> leftTargets,
            BoundExpression right,
            Conversion conversion,
            ArrayBuilder<LocalSymbol> temps,
            DeconstructionSideEffects effects,
            NamedTypeSymbol assignmentResultTupleType,
            bool isUsed,
            bool inInit)
        {
            Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);
            ImmutableArray<BoundExpression> rightParts = GetRightParts(right, conversion, temps, effects, ref inInit);

            ImmutableArray<(BoundValuePlaceholder?, BoundExpression?)> deconstructConversionInfo = conversion.DeconstructConversionInfo;
            Debug.Assert(!deconstructConversionInfo.IsDefault);
            Debug.Assert(leftTargets.Count == rightParts.Length && leftTargets.Count == deconstructConversionInfo.Length);

            var builder = isUsed ? ArrayBuilder<BoundExpression>.GetInstance(leftTargets.Count) : null;
            for (int i = 0; i < leftTargets.Count; i++)
            {
                BoundExpression? resultPart;
                TypeSymbol resultType = assignmentResultTupleType.TupleElementTypesWithAnnotations[i].Type;
                var (placeholder, nestedConversion) = deconstructConversionInfo[i];
                Debug.Assert(placeholder is not null);
                Debug.Assert(nestedConversion is not null);

                if (leftTargets[i].NestedVariables is { } nested)
                {
                    Debug.Assert(resultType.IsTupleType);
                    resultPart = ApplyDeconstructionConversion(
                        nested, rightParts[i],
                        BoundNode.GetConversion(nestedConversion, placeholder), temps, effects,
                        (NamedTypeSymbol)resultType,
                        isUsed, inInit);
                }
                else
                {
                    var rightPart = rightParts[i];
                    if (inInit)
                    {
                        rightPart = EvaluateSideEffectingArgumentToTemp(rightPart, effects.init, temps);
                    }
                    BoundExpression? leftTarget = leftTargets[i].Single;
                    Debug.Assert(leftTarget is { Type: { } });

                    resultPart = EvaluateConversionToTemp(rightPart, placeholder, nestedConversion, temps,
                        effects.conversions);

                    if (leftTarget.Kind != BoundKind.DiscardExpression)
                    {
                        effects.assignments.Add(MakeAssignmentOperator(resultPart.Syntax, leftTarget, resultPart,
                            used: false, isChecked: false, isCompoundAssignment: false));

                        if (ShouldConvertResultOfAssignmentToDynamic(resultType, leftTarget))
                        {
                            Debug.Assert(resultPart.Type is not null);
                            Debug.Assert(!resultPart.Type.IsDynamic());
                            resultPart = _factory.Convert(resultType, resultPart);
                        }

                        Debug.Assert(TypeSymbol.Equals(resultPart.Type, resultType, TypeCompareKind.AllIgnoreOptions));
                    }
                }
                Debug.Assert(builder is null || resultPart is { });
                builder?.Add(resultPart!);
            }

            if (isUsed)
            {
                var tupleType = NamedTypeSymbol.CreateTuple(locationOpt: null, elementTypesWithAnnotations: builder!.SelectAsArray(e => TypeWithAnnotations.Create(e.Type)),
                    elementLocations: default, elementNames: default,
                    compilation: _compilation, shouldCheckConstraints: false, includeNullability: false, errorPositions: default, syntax: (CSharpSyntaxNode)right.Syntax, diagnostics: _diagnostics);

                return new BoundConvertedTupleLiteral(
                    right.Syntax, sourceTuple: null, wasTargetTyped: false, arguments: builder!.ToImmutableAndFree(), argumentNamesOpt: default, inferredNamesOpt: default, tupleType);
            }
            else
            {
                return null;
            }
        }

        private ImmutableArray<BoundExpression> GetRightParts(BoundExpression right, Conversion conversion,
            ArrayBuilder<LocalSymbol> temps, DeconstructionSideEffects effects, ref bool inInit)
        {
            // Example:
            // var (x, y) = new Point(1, 2);
            var deconstructionInfo = conversion.DeconstructionInfo;
            if (!deconstructionInfo.IsDefault)
            {
                Debug.Assert(!IsTupleExpression(right.Kind));

                BoundExpression evaluationResult = EvaluateSideEffectingArgumentToTemp(right,
                    inInit ? effects.init : effects.deconstructions, temps);

                inInit = false;
                return InvokeDeconstructMethod(deconstructionInfo, evaluationResult, effects.deconstructions, temps);
            }

            // Example:
            // var (x, y) = (1, 2);
            if (IsTupleExpression(right.Kind))
            {
                return ((BoundTupleExpression)right).Arguments;
            }

            // Example:
            // (byte x, byte y) = (1, 2);
            // (int x, string y) = (1, null);
            if (right.Kind == BoundKind.Conversion)
            {
                var tupleConversion = (BoundConversion)right;
                if ((tupleConversion.Conversion.Kind == ConversionKind.ImplicitTupleLiteral || tupleConversion.Conversion.Kind == ConversionKind.Identity)
                    && IsTupleExpression(tupleConversion.Operand.Kind))
                {
                    return ((BoundTupleExpression)tupleConversion.Operand).Arguments;
                }
            }

            // Example:
            // var (x, y) = GetTuple();
            // var (x, y) = ((byte, byte)) (1, 2);
            // var (a, _) = ((short, short))((int, int))(1L, 2L);
            Debug.Assert(right.Type is { });
            if (right.Type.IsTupleType)
            {
                inInit = false;
                return AccessTupleFields(VisitExpression(right), temps, effects.deconstructions);
            }

            throw ExceptionUtilities.Unreachable();
        }

        private static bool IsTupleExpression(BoundKind kind)
        {
            return kind == BoundKind.TupleLiteral || kind == BoundKind.ConvertedTupleLiteral;
        }

        // This returns accessors and may create a temp for the tuple, but will not create temps for the tuple elements.
        private ImmutableArray<BoundExpression> AccessTupleFields(BoundExpression expression, ArrayBuilder<LocalSymbol> temps,
            ArrayBuilder<BoundExpression> effects)
        {
            Debug.Assert(expression.Type is { });
            Debug.Assert(expression.Type.IsTupleType);
            var tupleType = expression.Type;
            var tupleElementTypes = tupleType.TupleElementTypesWithAnnotations;

            var numElements = tupleElementTypes.Length;

            // save the target as we need to access it multiple times
            BoundExpression tuple;
            if (CanChangeValueBetweenReads(expression, localsMayBeAssignedOrCaptured: true))
            {
                BoundAssignmentOperator assignmentToTemp;
                BoundLocal savedTuple = _factory.StoreToTemp(expression, out assignmentToTemp);
                effects.Add(assignmentToTemp);
                temps.Add(savedTuple.LocalSymbol);
                tuple = savedTuple;
            }
            else
            {
                tuple = expression;
            }

            // list the tuple fields accessors
            var fields = tupleType.TupleElements;
            var builder = ArrayBuilder<BoundExpression>.GetInstance(numElements);
            for (int i = 0; i < numElements; i++)
            {
                var fieldAccess = MakeTupleFieldAccessAndReportUseSiteDiagnostics(tuple, expression.Syntax, fields[i]);
                builder.Add(fieldAccess);
            }
            return builder.ToImmutableAndFree();
        }

        private BoundExpression EvaluateConversionToTemp(BoundExpression expression, BoundValuePlaceholder placeholder, BoundExpression conversion,
            ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            if (BoundNode.GetConversion(conversion, placeholder).IsIdentity)
            {
                return expression;
            }

            return EvaluateSideEffectingArgumentToTemp(ApplyConversion(conversion, placeholder, expression), effects, temps);
        }

        private ImmutableArray<BoundExpression> InvokeDeconstructMethod(DeconstructMethodInfo deconstruction, BoundExpression target,
            ArrayBuilder<BoundExpression> effects, ArrayBuilder<LocalSymbol> temps)
        {
            AddPlaceholderReplacement(deconstruction.InputPlaceholder, target);

            var outputPlaceholders = deconstruction.OutputPlaceholders;
            var outLocals = ArrayBuilder<BoundExpression>.GetInstance(outputPlaceholders.Length);
            foreach (var outputPlaceholder in outputPlaceholders)
            {
                var localSymbol = new SynthesizedLocal(_factory.CurrentFunction, TypeWithAnnotations.Create(outputPlaceholder.Type), SynthesizedLocalKind.LoweringTemp);

                var localBound = new BoundLocal(target.Syntax, localSymbol, constantValueOpt: null, type: outputPlaceholder.Type)
                { WasCompilerGenerated = true };

                temps.Add(localSymbol);
                AddPlaceholderReplacement(outputPlaceholder, localBound);
                outLocals.Add(localBound);
            }

            effects.Add(VisitExpression(deconstruction.Invocation));

            RemovePlaceholderReplacement(deconstruction.InputPlaceholder);
            foreach (var outputPlaceholder in outputPlaceholders)
            {
                RemovePlaceholderReplacement(outputPlaceholder);
            }

            return outLocals.ToImmutableAndFree();
        }

        /// <summary>
        /// Evaluate side effects into a temp, if any.  Return the expression to give the value later.
        /// </summary>
        /// <param name="arg">The argument to evaluate early.</param>
        /// <param name="effects">A store of the argument into a temp, if necessary, is added here.</param>
        /// <param name="temps">Any generated temps are added here.</param>
        /// <returns>An expression evaluating the argument later (e.g. reading the temp), including a possible deferred user-defined conversion.</returns>
        private BoundExpression EvaluateSideEffectingArgumentToTemp(
            BoundExpression arg,
            ArrayBuilder<BoundExpression> effects,
            ArrayBuilder<LocalSymbol> temps)
        {
            var loweredArg = VisitExpression(arg);
            if (CanChangeValueBetweenReads(loweredArg, localsMayBeAssignedOrCaptured: true, structThisCanChangeValueBetweenReads: true))
            {
                BoundAssignmentOperator store;
                var temp = _factory.StoreToTemp(loweredArg, out store);
                temps.Add(temp.LocalSymbol);
                effects.Add(store);
                return temp;
            }
            else
            {
                return loweredArg;
            }
        }

        /// <summary>
        /// Adds the side effects to effects and returns temporaries to access them.
        /// The caller is responsible for releasing the nested ArrayBuilders.
        /// The variables should be unlowered.
        /// </summary>
        private ArrayBuilder<Binder.DeconstructionVariable> GetAssignmentTargetsAndSideEffects(BoundTupleExpression variables, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            var assignmentTargets = ArrayBuilder<Binder.DeconstructionVariable>.GetInstance(variables.Arguments.Length);

            foreach (var variable in variables.Arguments)
            {
                switch (variable.Kind)
                {
                    case BoundKind.DiscardExpression:
                        assignmentTargets.Add(new Binder.DeconstructionVariable(variable, variable.Syntax));
                        break;

                    case BoundKind.TupleLiteral:
                    case BoundKind.ConvertedTupleLiteral:
                        var tuple = (BoundTupleExpression)variable;
                        assignmentTargets.Add(new Binder.DeconstructionVariable(GetAssignmentTargetsAndSideEffects(tuple, temps, effects), tuple.Syntax));
                        break;

                    default:
                        Debug.Assert(variable.Type is { });
                        var temp = this.TransformCompoundAssignmentLHS(variable, isRegularCompoundAssignment: false,
                                                                       effects, temps, isDynamicAssignment: variable.Type.IsDynamic());
                        assignmentTargets.Add(new Binder.DeconstructionVariable(temp, variable.Syntax));
                        break;
                }
            }

            return assignmentTargets;
        }

        private class DeconstructionSideEffects
        {
            internal ArrayBuilder<BoundExpression> init = null!;
            internal ArrayBuilder<BoundExpression> deconstructions = null!;
            internal ArrayBuilder<BoundExpression> conversions = null!;
            internal ArrayBuilder<BoundExpression> assignments = null!;

            internal static DeconstructionSideEffects GetInstance()
            {
                var result = new DeconstructionSideEffects();
                result.init = ArrayBuilder<BoundExpression>.GetInstance();
                result.deconstructions = ArrayBuilder<BoundExpression>.GetInstance();
                result.conversions = ArrayBuilder<BoundExpression>.GetInstance();
                result.assignments = ArrayBuilder<BoundExpression>.GetInstance();

                return result;
            }

            internal void Consolidate()
            {
                init.AddRange(deconstructions);
                init.AddRange(conversions);
                init.AddRange(assignments);

                deconstructions.Free();
                conversions.Free();
                assignments.Free();
            }

            internal BoundExpression? PopLast()
            {
                if (init.Count == 0)
                {
                    return null;
                }

                var last = init.Last();
                init.RemoveLast();
                return last;
            }

            // This can only be called after Consolidate
            internal ImmutableArray<BoundExpression> ToImmutableAndFree()
            {
                return init.ToImmutableAndFree();
            }

            internal void Free()
            {
                init.Free();
            }
        }
    }
}
