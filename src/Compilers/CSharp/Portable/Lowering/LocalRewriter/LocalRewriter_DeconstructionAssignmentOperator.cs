﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            var right = node.Right;
            Debug.Assert(right.Conversion.Kind == ConversionKind.Deconstruction);

            return RewriteDeconstruction(node.Left, right.Conversion, right.Operand, node.IsUsed);
        }

        /// <summary>
        /// The left represents a tree of L-values. The structure of right can be missing parts of the tree on the left.
        /// The conversion holds nested conversisons and deconstruction information, which matches the tree from the left,
        /// and it provides the information to fill in the missing parts of the tree from the right and convert it to
        /// the tree from the left.
        ///
        /// A bound sequence is returned which has different phases of side-effects:
        /// - the initialization phase includes side-effects from the left, followed by evaluations of the right
        /// - the deconstruction phase includes all the invocations of Deconstruct methods and tuple element accesses below a Deconstruct call
        /// - the conversion phase
        /// - the assignment phase
        /// </summary>
        private BoundExpression RewriteDeconstruction(BoundTupleExpression left, Conversion conversion, BoundExpression right, bool isUsed)
        {
            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var effects = DeconstructionSideEffects.GetInstance();
            ArrayBuilder<Binder.DeconstructionVariable> lhsTargets = GetAssignmentTargetsAndSideEffects(left, temps, effects.init);

            BoundExpression returnValue = ApplyDeconstructionConversion(lhsTargets, right, conversion, temps, effects, isUsed, inInit: true);
            effects.Consolidate();

            BoundExpression result;
            if (!isUsed)
            {
                // When a deconstruction is not used, the last effect is used as return value
                Debug.Assert(returnValue is null);
                var last = effects.PopLast();
                if (last is null)
                {
                    // Deconstructions with no effects lower to nothing. For example, `(_, _) = (1, 2);`
                    result = null;
                    temps.Free();
                    _ = effects.ToImmutableAndFree();
                }
                else
                {
                    result = _factory.Sequence(temps.ToImmutableAndFree(), effects.ToImmutableAndFree(), last);
                }
            }
            else
            {
                if (!returnValue.HasErrors)
                {
                    returnValue = VisitExpression(returnValue);
                }
                result = _factory.Sequence(temps.ToImmutableAndFree(), effects.ToImmutableAndFree(), returnValue);
            }

            Binder.DeconstructionVariable.FreeDeconstructionVariables(lhsTargets);
            return result;
        }

        /// <summary>
        /// This method recurses through leftTargets, right and conversion at the same time.
        /// As it does, it collects side-effects into the proper buckets (init, deconstructions, conversions, assignments).
        ///
        /// The side-effects from the right initially go into the init bucket. But once we started drilling into a Deconstruct
        /// invocation, subsequent side-effects from the right go into the deconstructions bucket (otherwise they would
        /// be evaluated out of order).
        /// </summary>
        private BoundExpression ApplyDeconstructionConversion(ArrayBuilder<Binder.DeconstructionVariable> leftTargets,
            BoundExpression right, Conversion conversion, ArrayBuilder<LocalSymbol> temps, DeconstructionSideEffects effects,
            bool isUsed, bool inInit)
        {
            Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);
            ImmutableArray<BoundExpression> rightParts = GetRightParts(right, conversion, ref temps, effects, ref inInit);

            ImmutableArray<Conversion> underlyingConversions = conversion.UnderlyingConversions;
            Debug.Assert(!underlyingConversions.IsDefault);
            Debug.Assert(leftTargets.Count == rightParts.Length && leftTargets.Count == conversion.UnderlyingConversions.Length);

            var builder = isUsed ? ArrayBuilder<BoundExpression>.GetInstance(leftTargets.Count) : null;
            for (int i = 0; i < leftTargets.Count; i++)
            {
                BoundExpression resultPart;
                if (leftTargets[i].HasNestedVariables)
                {
                    resultPart = ApplyDeconstructionConversion(leftTargets[i].NestedVariables, rightParts[i],
                        underlyingConversions[i], temps, effects, isUsed, inInit);
                }
                else
                {
                    var rightPart = rightParts[i];
                    if (inInit)
                    {
                        rightPart = EvaluateSideEffectingArgumentToTemp(VisitExpression(rightPart), inInit ? effects.init : effects.deconstructions, ref temps);
                    }
                    BoundExpression leftTarget = leftTargets[i].Single;

                    resultPart = EvaluateConversionToTemp(rightPart, underlyingConversions[i], leftTarget.Type, temps,
                        effects.conversions);

                    if (leftTarget.Kind != BoundKind.DiscardExpression)
                    {
                        effects.assignments.Add(MakeAssignmentOperator(resultPart.Syntax, leftTarget, resultPart, leftTarget.Type,
                            used: true, isChecked: false, isCompoundAssignment: false));
                    }
                }
                builder?.Add(resultPart);
            }

            if (isUsed)
            {
                var tupleType = TupleTypeSymbol.Create(locationOpt: null, elementTypes: builder.SelectAsArray(e => e.Type),
                    elementLocations: default, elementNames: default,
                    compilation: _compilation, shouldCheckConstraints: false, errorPositions: default);

                return new BoundTupleLiteral(right.Syntax, argumentNamesOpt: default, inferredNamesOpt: default,
                    arguments: builder.ToImmutableAndFree(), type: tupleType);
            }
            else
            {
                return null;
            }
        }

        private ImmutableArray<BoundExpression> GetRightParts(BoundExpression right, Conversion conversion,
            ref ArrayBuilder<LocalSymbol> temps, DeconstructionSideEffects effects, ref bool inInit)
        {
            // Example:
            // var (x, y) = new Point(1, 2);
            var deconstructionInfo = conversion.DeconstructionInfo;
            if (!deconstructionInfo.IsDefault)
            {
                Debug.Assert(!IsTupleExpression(right.Kind));

                BoundExpression evaluationResult = EvaluateSideEffectingArgumentToTemp(VisitExpression(right),
                    inInit ? effects.init : effects.deconstructions, ref temps);

                inInit = false;
                return InvokeDeconstructMethod(deconstructionInfo, evaluationResult, effects.deconstructions, ref temps);
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
            if (right.Type.IsTupleType)
            {
                inInit = false;
                return AccessTupleFields(VisitExpression(right), temps, effects.deconstructions);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static bool IsTupleExpression(BoundKind kind)
        {
            return kind == BoundKind.TupleLiteral || kind == BoundKind.ConvertedTupleLiteral;
        }

        // This returns accessors and may create a temp for the tuple, but will not create temps for the tuple elements.
        private ImmutableArray<BoundExpression> AccessTupleFields(BoundExpression expression, ArrayBuilder<LocalSymbol> temps,
            ArrayBuilder<BoundExpression> effects)
        {
            Debug.Assert(expression.Type.IsTupleType);
            var tupleType = expression.Type;
            var tupleElementTypes = tupleType.TupleElementTypes;

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
                var field = fields[i];

                DiagnosticInfo useSiteInfo = field.GetUseSiteDiagnostic();
                if ((object)useSiteInfo != null && useSiteInfo.Severity == DiagnosticSeverity.Error)
                {
                    Symbol.ReportUseSiteDiagnostic(useSiteInfo, _diagnostics, expression.Syntax.Location);
                }
                var fieldAccess = MakeTupleFieldAccess(expression.Syntax, field, tuple, null, LookupResultKind.Empty);
                builder.Add(fieldAccess);
            }
            return builder.ToImmutableAndFree();
        }

        private BoundExpression EvaluateConversionToTemp(BoundExpression expression, Conversion conversion,
            TypeSymbol destinationType, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            if (conversion.IsIdentity)
            {
                return expression;
            }
            var evalConversion = MakeConversionNode(expression.Syntax, expression, conversion, destinationType, @checked: false);
            return EvaluateSideEffectingArgumentToTemp(evalConversion, effects, ref temps);
        }

        private ImmutableArray<BoundExpression> InvokeDeconstructMethod(DeconstructionInfo deconstruction, BoundExpression target,
            ArrayBuilder<BoundExpression> effects, ref ArrayBuilder<LocalSymbol> temps)
        {
            AddPlaceholderReplacement(deconstruction.InputPlaceholder, target);

            var outputPlaceholders = deconstruction.OutputPlaceholders;
            var outLocals = ArrayBuilder<BoundExpression>.GetInstance(outputPlaceholders.Length);
            foreach (var outputPlaceholder in outputPlaceholders)
            {
                var localSymbol = new SynthesizedLocal(_factory.CurrentMethod, outputPlaceholder.Type, SynthesizedLocalKind.LoweringTemp);

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

        BoundExpression EvaluateSideEffectingArgumentToTemp(BoundExpression arg, ArrayBuilder<BoundExpression> effects,
            ref ArrayBuilder<LocalSymbol> temps)
        {
            if (CanChangeValueBetweenReads(arg, localsMayBeAssignedOrCaptured: true))
            {
                BoundAssignmentOperator store;
                var temp = _factory.StoreToTemp(arg, out store);

                if (temps == null)
                {
                    temps = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                temps.Add(temp.LocalSymbol);
                effects.Add(store);
                return temp;
            }
            else
            {
                return arg;
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
                        var tuple = (BoundTupleExpression)variable;
                        assignmentTargets.Add(new Binder.DeconstructionVariable(GetAssignmentTargetsAndSideEffects(tuple, temps, effects), tuple.Syntax));
                        break;

                    case BoundKind.ConvertedTupleLiteral:
                        throw ExceptionUtilities.UnexpectedValue(variable.Kind);

                    default:
                        var temp = this.TransformCompoundAssignmentLHS(variable, effects, temps, isDynamicAssignment: variable.Type.IsDynamic());
                        assignmentTargets.Add(new Binder.DeconstructionVariable(temp, variable.Syntax));
                        break;
                }
            }

            return assignmentTargets;
        }

        internal class DeconstructionSideEffects
        {
            internal ArrayBuilder<BoundExpression> init;
            internal ArrayBuilder<BoundExpression> deconstructions;
            internal ArrayBuilder<BoundExpression> conversions;
            internal ArrayBuilder<BoundExpression> assignments;

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

            internal BoundExpression PopLast()
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
        }
    }
}
