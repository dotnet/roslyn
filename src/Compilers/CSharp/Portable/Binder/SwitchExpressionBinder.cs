// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SwitchExpressionBinder : Binder
    {
        private readonly SwitchExpressionSyntax SwitchExpressionSyntax;

        internal SwitchExpressionBinder(SwitchExpressionSyntax switchExpressionSyntax, Binder next)
            : base(next)
        {
            SwitchExpressionSyntax = switchExpressionSyntax;
        }

        internal override BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node == SwitchExpressionSyntax);

            // Bind switch expression and set the switch governing type.
            var boundInputExpression = BindSwitchGoverningExpression(diagnostics);
            ImmutableArray<BoundSwitchExpressionArm> switchArms = BindSwitchExpressionArms(node, originalBinder, boundInputExpression, diagnostics);
            TypeSymbol? naturalType = InferResultType(boundInputExpression, switchArms, diagnostics);
            bool reportedNotExhaustive = CheckSwitchExpressionExhaustive(node, boundInputExpression, switchArms, out BoundDecisionDag decisionDag, out LabelSymbol? defaultLabel, diagnostics);

            // When the input is constant, we use that to reshape the decision dag that is returned
            // so that flow analysis will see that some of the cases may be unreachable.
            decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(boundInputExpression);

            return new BoundUnconvertedSwitchExpression(
                node, boundInputExpression, switchArms, decisionDag,
                defaultLabel: defaultLabel, reportedNotExhaustive: reportedNotExhaustive, type: naturalType);
        }

        /// <summary>
        /// Build the decision dag, giving an error if some cases are subsumed and a warning if the switch expression is not exhaustive.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="boundInputExpression"></param>
        /// <param name="switchArms"></param>
        /// <param name="decisionDag"></param>
        /// <param name="diagnostics"></param>
        /// <returns>true if there was a non-exhaustive warning reported</returns>
        private bool CheckSwitchExpressionExhaustive(
            SwitchExpressionSyntax node,
            BoundExpression boundInputExpression,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            out BoundDecisionDag decisionDag,
            [NotNullWhen(true)] out LabelSymbol? defaultLabel,
            BindingDiagnosticBag diagnostics)
        {
            defaultLabel = new GeneratedLabelSymbol("default");
            decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchExpression(this.Compilation, node, boundInputExpression, switchArms, defaultLabel, diagnostics);
            var reachableLabels = decisionDag.ReachableLabels;
            bool hasErrors = false;
            foreach (BoundSwitchExpressionArm arm in switchArms)
            {
                hasErrors |= arm.HasErrors;
                if (!hasErrors && !reachableLabels.Contains(arm.Label))
                {
                    diagnostics.Add(ErrorCode.ERR_SwitchArmSubsumed, arm.Pattern.Syntax.Location);
                }
            }

            if (!reachableLabels.Contains(defaultLabel))
            {
                // switch expression is exhaustive; no default label needed.
                defaultLabel = null;
                return false;
            }

            if (hasErrors)
                return true;

            // We only report exhaustive warnings when the default label is reachable through some series of
            // tests that do not include a test in which the value is known to be null.  Handling paths with
            // nulls is the job of the nullable walker.
            bool wasAcyclic = TopologicalSort.TryIterativeSort<BoundDecisionDagNode>(new[] { decisionDag.RootNode }, nonNullSuccessors, out var nodes);
            // Since decisionDag.RootNode is acyclic by construction, its subset of nodes sorted here cannot be cyclic
            Debug.Assert(wasAcyclic);
            foreach (var n in nodes)
            {
                if (n is BoundLeafDecisionDagNode leaf && leaf.Label == defaultLabel)
                {
                    var samplePattern = PatternExplainer.SamplePatternForPathToDagNode(
                        BoundDagTemp.ForOriginalInput(boundInputExpression), nodes, n, nullPaths: false, out bool requiresFalseWhenClause, out bool unnamedEnumValue);
                    ErrorCode warningCode =
                        requiresFalseWhenClause ? ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen :
                        unnamedEnumValue ? ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue :
                        ErrorCode.WRN_SwitchExpressionNotExhaustive;
                    diagnostics.Add(
                        warningCode,
                        node.SwitchKeyword.GetLocation(),
                        samplePattern);
                    return true;
                }
            }

            return false;

            ImmutableArray<BoundDecisionDagNode> nonNullSuccessors(BoundDecisionDagNode n)
            {
                switch (n)
                {
                    case BoundTestDecisionDagNode p:
                        switch (p.Test)
                        {
                            case BoundDagNonNullTest t: // checks that the input is not null
                                return ImmutableArray.Create(p.WhenTrue);
                            case BoundDagExplicitNullTest t: // checks that the input is null
                                return ImmutableArray.Create(p.WhenFalse);
                            default:
                                return BoundDecisionDag.Successors(n);
                        }
                    default:
                        return BoundDecisionDag.Successors(n);
                }
            }
        }

        private static void InferNonTupleResultType(ImmutableArray<BoundSwitchExpressionArm> switchCases, ArrayBuilder<TypeSymbol> typesInOrder)
        {
            var seenTypes = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<TypeSymbol>();
            foreach (var @case in switchCases)
            {
                var type = @case.Value.Type;
                if (type is object && seenTypes.Add(type))
                {
                    typesInOrder.Add(type);
                }
            }
            seenTypes.Free();
        }

        /// <summary>
        /// Infer the result type of the tuple expressions returned by the switch expression arms.
        /// This method is called upon noticing a single arm with a tuple type.
        /// Binding will result in trying to deconstruct the non-tuple expressions in the expression.
        /// </summary>
        private void InferTupleResultType(BoundExpression boundInputExpression, ImmutableArray<BoundSwitchExpressionArm> switchCases, BoundSwitchExpressionArm tupleHintingArm, ArrayBuilder<TypeSymbol> typesInOrder, BindingDiagnosticBag diagnostics)
        {
            var hintingTupleExpression = tupleHintingArm.Value as BoundTupleExpression;
            int cardinality = hintingTupleExpression!.Arguments.Length;

            var tupleArgumentsWithNullLiteral = ArrayPool<bool>.Shared.Rent(cardinality);
            var tupleArgumentTypesInOrder = ArrayPool<ArrayBuilder<TypeSymbol>>.Shared.Rent(cardinality);
            for (int i = 0; i < cardinality; i++)
            {
                tupleArgumentTypesInOrder[i] = ArrayBuilder<TypeSymbol>.GetInstance();
            }

            var seenTupleArgumentTypes = ArrayPool<PooledHashSet<TypeSymbol>>.Shared.Rent(cardinality);
            for (int i = 0; i < cardinality; i++)
            {
                seenTupleArgumentTypes[i] = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<TypeSymbol>();
            }

            bool hasCardinalityMismatch = false;
            var seenTypes = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<TypeSymbol>();
            foreach (var arm in switchCases)
            {
                var tupleExpression = arm.Value as BoundTupleExpression;
                if (tupleExpression is null)
                {
                    if (arm.Value.Type is null)
                    {
                        // Non-tuple expressions without a valid type are ignored
                        // This includes default literals and throw expressions
                        continue;
                    }
                }
                else
                {
                    // Attempt to match cardinality
                    if (tupleExpression.Arguments.Length != cardinality)
                    {
                        // Only report the error once in the entire switch
                        if (hasCardinalityMismatch)
                        {
                            continue;
                        }

                        hasCardinalityMismatch = true;
                        diagnostics.Add(ErrorCode.ERR_SwitchExpressionInferTupleTypeElementCountMismatch, boundInputExpression.Syntax.Location);
                        continue;
                    }
                }

                var type = arm.Value.Type;
                if (type is not null)
                {
                    // The expression is a tuple with a bound type
                    if (seenTypes.Add(type))
                    {
                        typesInOrder.Add(type);
                    }
                    continue;
                }
                else
                {
                    // The expression has no bound type
                    RoslynDebug.AssertNotNull(tupleExpression);
                }

                // Only process the tuple expression's types if no bound type was found
                // TODO: Evaluate whether this ignores valid conversions, in which case the check should be removed
                if (!typesInOrder.Any())
                {
                    for (int tupleArgumentIndex = 0; tupleArgumentIndex < tupleExpression.Arguments.Length; tupleArgumentIndex++)
                    {
                        var argument = tupleExpression.Arguments[tupleArgumentIndex];
                        var argumentType = argument.Type;

                        if (argumentType is null)
                        {
                            tupleArgumentsWithNullLiteral[tupleArgumentIndex] |= argument.IsLiteralNull();
                            continue;
                        }

                        if (seenTupleArgumentTypes[tupleArgumentIndex].Add(argumentType))
                        {
                            tupleArgumentTypesInOrder[tupleArgumentIndex].Add(argumentType);
                        }
                    }
                }
            }

            foreach (var set in seenTupleArgumentTypes)
            {
                set?.Free();
            }
            ArrayPool<PooledHashSet<TypeSymbol>>.Shared.Return(seenTupleArgumentTypes);

            if (!typesInOrder.Any())
            {
                // Iterate through all the possible combinations of inferred types
                // since we didn't find any exact match

                var tupleNestedTypes = ArrayPool<TypeSymbol>.Shared.Rent(cardinality);
                recurseForTypes(0);
                ArrayPool<TypeSymbol>.Shared.Return(tupleNestedTypes);

                // There won't be tuples with too large cardinalities, so recursion should be fine
                void recurseForTypes(int tupleArgumentIndex)
                {
                    if (tupleArgumentIndex == cardinality)
                    {
                        var createdTupleType = createTupleType();
                        bool added = seenTypes.Add(createdTupleType);
                        if (added)
                        {
                            typesInOrder.Add(createdTupleType);
                        }
                        return;
                    }

                    foreach (var orderedType in tupleArgumentTypesInOrder[tupleArgumentIndex])
                    {
                        tupleNestedTypes[tupleArgumentIndex] = orderedType;
                        recurseForTypes(tupleArgumentIndex + 1);

                        if (!orderedType.CanBeAssignedNull())
                        {
                            // We found null assigned to the value,
                            // so we might try recursing for the type's nullable counterpart
                            // There is the possibility this could be handled from the conversions

                            if (tupleArgumentsWithNullLiteral[tupleArgumentIndex])
                            {
                                var nullableOrderedType = Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(orderedType);
                                tupleNestedTypes[tupleArgumentIndex] = nullableOrderedType;
                                recurseForTypes(tupleArgumentIndex + 1);
                            }
                        }
                    }
                }
                NamedTypeSymbol createTupleType()
                {
                    return createTupleTypeImpl(tupleNestedTypes, cardinality, Compilation);
                }
                static NamedTypeSymbol createTupleTypeImpl(TypeSymbol[] nestedTypes, int cardinality, CSharpCompilation compilation)
                {
                    var builder = ArrayBuilder<ITypeSymbol>.GetInstance(cardinality);
                    for (int i = 0; i < cardinality; i++)
                    {
                        builder.Add(nestedTypes[i].GetPublicSymbol());
                    }

                    var elementTypes = builder.ToImmutableAndFree();
                    var type = compilation.CreateTupleTypeSymbol(elementTypes).GetSymbol();
                    return type!;
                }
            }

            foreach (var prderedArgumentTypes in tupleArgumentTypesInOrder)
            {
                prderedArgumentTypes?.Free();
            }
            ArrayPool<ArrayBuilder<TypeSymbol>>.Shared.Return(tupleArgumentTypesInOrder);

            seenTypes.Free();
        }

        /// <summary>
        /// Infer the result type of the switch expression by looking for a common type
        /// to which every arm's expression can be converted.
        /// </summary>
        private TypeSymbol? InferResultType(BoundExpression boundInputExpression, ImmutableArray<BoundSwitchExpressionArm> switchCases, BindingDiagnosticBag diagnostics)
        {
            var typesInOrder = ArrayBuilder<TypeSymbol>.GetInstance();

            BoundSwitchExpressionArm? tupleHintingArm = null;
            foreach (var arm in switchCases)
            {
                if (arm.Value is BoundTupleExpression)
                {
                    tupleHintingArm = arm;
                    break;
                }
            }

            if (tupleHintingArm is not null)
            {
                InferTupleResultType(boundInputExpression, switchCases, tupleHintingArm, typesInOrder, diagnostics);
            }
            else
            {
                InferNonTupleResultType(switchCases, typesInOrder);
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var commonType = BestTypeInferrer.GetBestType(typesInOrder, Conversions, ref useSiteInfo);
            typesInOrder.Free();

            // We've found a candidate common type among those arms that have a type.  Also check that every arm's
            // expression (even those without a type) can be converted to that type.
            if (commonType is object)
            {
                foreach (var @case in switchCases)
                {
                    if (!this.Conversions.ClassifyImplicitConversionFromExpression(@case.Value, commonType, ref useSiteInfo).Exists)
                    {
                        commonType = null;
                        break;
                    }
                }
            }

            diagnostics.Add(SwitchExpressionSyntax, useSiteInfo);
            return commonType;
        }

        private ImmutableArray<BoundSwitchExpressionArm> BindSwitchExpressionArms(SwitchExpressionSyntax node, Binder originalBinder, BoundExpression inputExpression, BindingDiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundSwitchExpressionArm>.GetInstance();
            (TypeSymbol inputType, uint valEscape) = GetInputTypeAndValEscape(inputExpression);
            foreach (var arm in node.Arms)
            {
                var armBinder = originalBinder.GetRequiredBinder(arm);
                Debug.Assert(inputExpression.Type is not null);
                var boundArm = armBinder.BindSwitchExpressionArm(arm, inputType, valEscape, diagnostics);
                builder.Add(boundArm);
            }

            return builder.ToImmutableAndFree();
        }

        internal (TypeSymbol GoverningType, uint GoverningValEscape) GetInputTypeAndValEscape(BoundExpression? inputExpression = null)
        {
            inputExpression ??= BindSwitchGoverningExpression(BindingDiagnosticBag.Discarded);
            Debug.Assert(inputExpression.Type is not null);
            return (inputExpression.Type, GetValEscape(inputExpression, LocalScopeDepth));
        }

        private BoundExpression BindSwitchGoverningExpression(BindingDiagnosticBag diagnostics)
        {
            var switchGoverningExpression = BindRValueWithoutTargetType(SwitchExpressionSyntax.GoverningExpression, diagnostics);
            if (switchGoverningExpression.Type == (object?)null || switchGoverningExpression.Type.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_BadPatternExpression, SwitchExpressionSyntax.GoverningExpression.Location, switchGoverningExpression.Display);
                switchGoverningExpression = this.GenerateConversionForAssignment(CreateErrorType(), switchGoverningExpression, diagnostics);
            }

            return switchGoverningExpression;
        }
    }
}
