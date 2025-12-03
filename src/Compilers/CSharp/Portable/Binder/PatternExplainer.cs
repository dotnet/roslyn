// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class PatternExplainer
    {
        private class NoRemainingValuesException : Exception { }

        /// <summary>
        /// Find the shortest path from the root node to the node of interest.
        /// </summary>
        /// <param name="nodes">The set of nodes in topological order.</param>
        /// <param name="node">The node of interest.</param>
        /// <param name="nullPaths">Whether to permit following paths that test for null.</param>
        /// <param name="requiresFalseWhenClause">set to true if the returned path requires some when clause to evaluate to 'false'</param>
        /// <returns>The shortest path, excluding the node of interest.</returns>
        private static ImmutableArray<BoundDecisionDagNode> ShortestPathToNode(
            ImmutableArray<BoundDecisionDagNode> nodes,
            BoundDecisionDagNode node,
            bool nullPaths,
            out bool requiresFalseWhenClause)
        {
            // compute the distance from each node to the endpoint.
            var dist = PooledDictionary<BoundDecisionDagNode, (int distance, BoundDecisionDagNode next)>.GetInstance();
            int nodeCount = nodes.Length;
            int infinity = 2 * nodeCount + 2;
            int distance(BoundDecisionDagNode x)
            {
                if (x == null)
                    return infinity;
                if (dist.TryGetValue(x, out var v))
                    return v.distance;
                Debug.Assert(!nodes.Contains(x));
                return infinity;
            }

            for (int i = nodeCount - 1; i >= 0; i--)
            {
                var n = nodes[i];
                dist.Add(n, n switch
                {
                    BoundEvaluationDecisionDagNode e => (distance(e.Next), e.Next),
                    BoundTestDecisionDagNode { Test: BoundDagNonNullTest } t when !nullPaths => (1 + distance(t.WhenTrue), t.WhenTrue),
                    BoundTestDecisionDagNode { Test: BoundDagExplicitNullTest } t when !nullPaths => (1 + distance(t.WhenFalse), t.WhenFalse),
                    BoundTestDecisionDagNode t when distance(t.WhenTrue) is var trueDist1 && distance(t.WhenFalse) is var falseDist1 =>
                        (trueDist1 <= falseDist1) ? (1 + trueDist1, t.WhenTrue) : (1 + falseDist1, t.WhenFalse),
                    BoundWhenDecisionDagNode w when distance(w.WhenTrue) is var trueDist2 && distance(w.WhenFalse) is var falseDist2 =>
                        // add nodeCount to the distance if we need to flag that the path requires failure of a when clause
                        (trueDist2 <= falseDist2) ? (1 + trueDist2, w.WhenTrue) : (1 + (falseDist2 < nodeCount ? nodeCount : 0) + falseDist2, w.WhenFalse),
                    // treat the endpoint as distance 1.
                    // treat other nodes as not on the path to the endpoint
                    _ => ((n == node) ? 1 : infinity, null),
                });
            }

            // trace a path from the root node to the node of interest
            var distanceToNode = dist[nodes[0]].distance;
            requiresFalseWhenClause = distanceToNode > nodeCount;
            var result = ArrayBuilder<BoundDecisionDagNode>.GetInstance(capacity: distanceToNode);
            for (BoundDecisionDagNode n = nodes[0]; n != node;)
            {
                result.Add(n);
                switch (n)
                {
                    case BoundEvaluationDecisionDagNode e:
                        n = e.Next;
                        break;
                    case BoundTestDecisionDagNode t:
                        (int d, BoundDecisionDagNode next) = dist[t];
                        Debug.Assert(next != null);
                        Debug.Assert(distance(next) == (d - 1));
                        n = next;
                        break;
                    case BoundWhenDecisionDagNode w:
                        result.RemoveLast();
                        n = w.WhenFalse;
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable();
                }
            }

            dist.Free();
            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Enumerates the paths from the root node to the node of interest, and invokes the handler
        /// on each one until the handler returns false.
        /// The order is deterministic, but we're not starting from the shortest path.
        /// </summary>
        /// <param name="rootNode">The root node of the DAG.</param>
        /// <param name="targetNode">The node of interest.</param>
        /// <param name="nullPaths">Whether to permit following paths that test for null.</param>
        /// <param name="handler">Handler to call back for every path to the target node.</param>
        private static void VisitPathsToNode(BoundDecisionDagNode rootNode, BoundDecisionDagNode targetNode, bool nullPaths,
            Func<ImmutableArray<BoundDecisionDagNode>, bool, bool> handler)
        {
#nullable enable
            var pathBuilder = ArrayBuilder<BoundDecisionDagNode>.GetInstance();
            var stack = ArrayBuilder<BoundDecisionDagNode?>.GetInstance();
            exploreToNode(rootNode, currentRequiresFalseWhenClause: false);
            stack.Free();
            pathBuilder.Free();
            return;

            // Recursive exploration helper
            // Returns true to continue, false to stop
            bool exploreToNode(BoundDecisionDagNode? currentNode, bool currentRequiresFalseWhenClause)
            {
                if (currentNode is null)
                {
                    return true;
                }

                int stackSize = stack.Count;
                stack.Push(currentNode);

                do
                {
                    currentNode = stack.Pop();

                    if (currentNode is null)
                    {
                        pathBuilder.Pop();
                        continue;
                    }

                    if (currentNode == targetNode)
                    {
                        if (!handler(pathBuilder.ToImmutable(), currentRequiresFalseWhenClause))
                        {
                            stack.Count = stackSize;
                            return false;
                        }

                        continue;
                    }

                    pathBuilder.Push(currentNode);

                    switch (currentNode)
                    {
                        case BoundLeafDecisionDagNode:
                            break;

                        case BoundTestDecisionDagNode test:
                            stack.Push(null); // marker to pop from pathBuilder

                            bool skipWhenTrue = test.Test is BoundDagExplicitNullTest && !nullPaths;
                            bool skipWhenFalse = test.Test is BoundDagNonNullTest && !nullPaths;

                            if (!skipWhenFalse && test.WhenFalse is not null)
                            {
                                stack.Push(test.WhenFalse);
                            }

                            if (!skipWhenTrue && test.WhenTrue is not null)
                            {
                                stack.Push(test.WhenTrue);
                            }

                            continue;

                        case BoundEvaluationDecisionDagNode evaluation:

                            if (evaluation.Next is not null)
                            {
                                stack.Push(null); // marker to pop from pathBuilder
                                stack.Push(evaluation.Next);
                                continue;
                            }
                            break;

                        case BoundWhenDecisionDagNode whenNode:
                            pathBuilder.Pop();
                            if (!exploreToNode(whenNode.WhenFalse, currentRequiresFalseWhenClause: true))
                            {
                                stack.Count = stackSize;
                                return false;
                            }

                            continue;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(currentNode.Kind);
                    }

                    pathBuilder.Pop();
                }
                while (stack.Count > stackSize);

                return true;
            }
#nullable disable
        }

        /// <summary>
        /// Return a sample pattern that would lead to the given decision dag node.
        /// </summary>
        /// <param name="nodes">A topologically sorted list of nodes in the decision dag.</param>
        /// <param name="targetNode">A node of interest (typically, the default node for a non-exhaustive switch).</param>
        /// <param name="nullPaths">Permit the use of "null" paths on tests which check for null.</param>
        /// <returns></returns>
        internal static string SamplePatternForPathToDagNode(
            BoundDagTemp rootIdentifier,
            ImmutableArray<BoundDecisionDagNode> nodes,
            BoundDecisionDagNode targetNode,
            bool nullPaths,
            out bool requiresFalseWhenClause,
            out bool unnamedEnumValue)
        {
#if DEBUG
            // Exercise enumeration of all paths to node
            VisitPathsToNode(nodes[0], targetNode, nullPaths: true, handler: (currentPathToNode, currentRequiresFalseWhenClause) => true);
            VisitPathsToNode(nodes[0], targetNode, nullPaths: false, handler: (currentPathToNode, currentRequiresFalseWhenClause) => true);
#endif

            unnamedEnumValue = false;

            // Compute the path to the node, excluding the node itself.
            var shortestPathToNode = ShortestPathToNode(nodes, targetNode, nullPaths, out requiresFalseWhenClause);
            gatherConstraintsAndEvaluations(targetNode, shortestPathToNode, out var constraints, out var evaluations);

            try
            {
                return SamplePatternForTemp(rootIdentifier, constraints, evaluations, requireExactType: false, ref unnamedEnumValue);
            }
            catch (NoRemainingValuesException)
            {
            }

            // In rare cases, the shortest path isn't the one that yields a sample
            return samplePatternFromOtherPaths(rootIdentifier, nodes[0], targetNode, nullPaths, out requiresFalseWhenClause, out unnamedEnumValue);

            static string samplePatternFromOtherPaths(BoundDagTemp rootIdentifier, BoundDecisionDagNode rootNode,
                BoundDecisionDagNode targetNode, bool nullPaths, out bool requiresFalseWhenClause, out bool unnamedEnumValue)
            {
                string altSamplePatternForTemp = null;
                bool altRequiresFalseWhenClause = false;
                bool altUnnamedEnumValue = false;

                VisitPathsToNode(rootNode, targetNode, nullPaths, handler: (currentPathToNode, currentRequiresFalseWhenClause) =>
                {
                    altRequiresFalseWhenClause = currentRequiresFalseWhenClause;
                    gatherConstraintsAndEvaluations(targetNode, currentPathToNode, out var constraints, out var evaluations);

                    try
                    {
                        altUnnamedEnumValue = false;
                        altSamplePatternForTemp = SamplePatternForTemp(rootIdentifier, constraints, evaluations, requireExactType: false, ref altUnnamedEnumValue);
                        return false; // we've successfully produced a sample, so stop exploring paths
                    }
                    catch (NoRemainingValuesException)
                    {
                        return true;
                    }
                });

                if (altSamplePatternForTemp is not null)
                {
                    unnamedEnumValue = altUnnamedEnumValue;
                    requiresFalseWhenClause = altRequiresFalseWhenClause;
                    return altSamplePatternForTemp;
                }

                throw ExceptionUtilities.Unreachable();
            }

            static void gatherConstraintsAndEvaluations(BoundDecisionDagNode targetNode, ImmutableArray<BoundDecisionDagNode> pathToNode,
                out Dictionary<BoundDagTemp, ArrayBuilder<(BoundDagTest, bool)>> constraints,
                out Dictionary<BoundDagTemp, ArrayBuilder<BoundDagEvaluation>> evaluations)
            {
                constraints = new Dictionary<BoundDagTemp, ArrayBuilder<(BoundDagTest, bool)>>();
                evaluations = new Dictionary<BoundDagTemp, ArrayBuilder<BoundDagEvaluation>>();
                for (int i = 0, n = pathToNode.Length; i < n; i++)
                {
                    BoundDecisionDagNode node = pathToNode[i];
                    switch (node)
                    {
                        case BoundTestDecisionDagNode t:
                            {
                                BoundDecisionDagNode nextNode = (i < n - 1) ? pathToNode[i + 1] : targetNode;
                                bool sense = t.WhenTrue == nextNode || (t.WhenFalse != nextNode && t.WhenTrue is BoundWhenDecisionDagNode);
                                BoundDagTest test = t.Test;
                                BoundDagTemp temp = test.Input;
                                if (test is BoundDagTypeTest && sense == false)
                                {
                                    // A failed type test is not very useful in constructing a counterexample,
                                    // at least not without discriminated unions, so we just drop them.
                                }
                                else
                                {
                                    if (!constraints.TryGetValue(temp, out var constraintBuilder))
                                    {
                                        constraints.Add(temp, constraintBuilder = new ArrayBuilder<(BoundDagTest, bool)>());
                                    }
                                    constraintBuilder.Add((test, sense));
                                }
                            }
                            break;
                        case BoundEvaluationDecisionDagNode e:
                            {
                                BoundDagTemp temp = e.Evaluation.Input;
                                if (!evaluations.TryGetValue(temp, out var evaluationBuilder))
                                {
                                    evaluations.Add(temp, evaluationBuilder = new ArrayBuilder<BoundDagEvaluation>());
                                }
                                evaluationBuilder.Add(e.Evaluation);
                            }
                            break;
                    }
                }
            }
        }

        private static string SamplePatternForTemp(
            BoundDagTemp input,
            Dictionary<BoundDagTemp, ArrayBuilder<(BoundDagTest test, bool sense)>> constraintMap,
            Dictionary<BoundDagTemp, ArrayBuilder<BoundDagEvaluation>> evaluationMap,
            bool requireExactType,
            ref bool unnamedEnumValue)
        {
            var constraints = getArray(constraintMap, input);
            var evaluations = getArray(evaluationMap, input);

            return
                tryHandleSingleTest() ??
                tryHandleTypeTestAndTypeEvaluation(ref unnamedEnumValue) ??
                tryHandleUnboxNullableValueType(ref unnamedEnumValue) ??
                tryHandleTuplePattern(ref unnamedEnumValue) ??
                tryHandleNumericLimits(ref unnamedEnumValue) ??
                tryHandleRecursivePattern(ref unnamedEnumValue) ??
                tryHandleListPattern(ref unnamedEnumValue) ??
                produceFallbackPattern();

            static ImmutableArray<T> getArray<T>(Dictionary<BoundDagTemp, ArrayBuilder<T>> map, BoundDagTemp temp)
            {
                return map.TryGetValue(temp, out var builder) ? builder.ToImmutable() : ImmutableArray<T>.Empty;
            }

            // Handle the special case of a single test that is not handled.
            string tryHandleSingleTest()
            {
                if (evaluations.IsEmpty && constraints.Length == 1)
                {
                    switch (constraints[0])
                    {
                        case (test: BoundDagNonNullTest _, sense: var sense):
                            return !sense ? "null" : requireExactType ? input.Type.ToDisplayString() : "not null";
                        case (test: BoundDagExplicitNullTest _, sense: var sense):
                            return sense ? "null" : requireExactType ? input.Type.ToDisplayString() : "not null";
                        case (test: BoundDagTypeTest { Type: var testedType }, sense: var sense):
                            Debug.Assert(sense); // we have dropped failing type tests
                            return testedType.ToDisplayString();
                    }
                }

                return null;
            }

            // Handle the special case of a type test and a type evaluation.
            string tryHandleTypeTestAndTypeEvaluation(ref bool unnamedEnumValue)
            {
                if (evaluations.Length == 1 && constraints.Length == 1 &&
                    constraints[0] is (BoundDagTypeTest { Type: var constraintType }, true) &&
                    evaluations[0] is BoundDagTypeEvaluation { Type: var evaluationType } te &&
                    constraintType.Equals(evaluationType, TypeCompareKind.AllIgnoreOptions))
                {
                    var typedTemp = new BoundDagTemp(te.Syntax, te.Type, te);
                    return SamplePatternForTemp(typedTemp, constraintMap, evaluationMap, requireExactType: true, ref unnamedEnumValue);
                }

                return null;
            }

            // Handle the special case of a null test and a type evaluation to unbox a nullable value type
            string tryHandleUnboxNullableValueType(ref bool unnamedEnumValue)
            {
                if (evaluations.Length == 1 && constraints.Length == 1 &&
                    constraints[0] is (BoundDagNonNullTest _, true) &&
                    evaluations[0] is BoundDagTypeEvaluation { Type: var evaluationType } te &&
                    input.Type.IsNullableType() && input.Type.GetNullableUnderlyingType().Equals(evaluationType, TypeCompareKind.AllIgnoreOptions))
                {
                    var typedTemp = new BoundDagTemp(te.Syntax, te.Type, te);
                    var result = SamplePatternForTemp(typedTemp, constraintMap, evaluationMap, requireExactType: false, ref unnamedEnumValue);
                    // We need a null check. If not included in the result, add it.
                    return (result == "_") ? "not null" : result;
                }

                return null;
            }

            // Handle the special case of a list pattern
            string tryHandleListPattern(ref bool unnamedEnumValue)
            {
                if (constraints.IsEmpty && evaluations.IsEmpty)
                    return null;

                // not-null tests are implicitly incorporated into a list pattern
                if (!constraints.All(isNotNullTest))
                {
                    return null;
                }

                if (evaluations[0] is BoundDagPropertyEvaluation { IsLengthOrCount: true } lengthOrCount)
                {
                    BoundDagSliceEvaluation slice = null;
                    for (int i = 1; i < evaluations.Length; i++)
                    {
                        switch (evaluations[i])
                        {
                            case BoundDagIndexerEvaluation:
                                continue;
                            case BoundDagSliceEvaluation e:
                                if (slice != null)
                                {
                                    // A list pattern can only support a single slice within.
                                    // We won't try to generate a list pattern if there's more.
                                    return null;
                                }
                                slice = e;
                                continue;
                            default:
                                return null;
                        }
                    }

                    var lengthTemp = new BoundDagTemp(lengthOrCount.Syntax, lengthOrCount.Property.Type, lengthOrCount);
                    var lengthValues = (IValueSet<int>)computeRemainingValues(ValueSetFactory.ForLength, getArray(constraintMap, lengthTemp));
                    int lengthValue = lengthValues.Sample.Int32Value;
                    if (slice != null)
                    {
                        if (lengthValues.All(BinaryOperatorKind.Equal, lengthValue))
                        {
                            // Bail if there's a slice but only one length value is remained.
                            // That could happen with nested slice patterns or length tests
                            // and also with very long list patterns in certain conditions.
                            return null;
                        }

                        if (slice.StartIndex - slice.EndIndex > lengthValue)
                        {
                            // Bail if the sample value is less than the required minimum length by the slice
                            // to avoid generating an incorrect example.
                            return null;
                        }
                    }

                    var subpatterns = new ArrayBuilder<string>(lengthValue);
                    subpatterns.AddMany("_", lengthValue);
                    for (int i = 1; i < evaluations.Length; i++)
                    {
                        switch (evaluations[i])
                        {
                            case BoundDagIndexerEvaluation e:
                                var indexerTemp = new BoundDagTemp(e.Syntax, e.IndexerType, e);
                                int index = e.Index;
                                int effectiveIndex = index < 0 ? lengthValue + index : index;
                                if (effectiveIndex < 0 || effectiveIndex >= lengthValue)
                                    return null;
                                var oldPattern = subpatterns[effectiveIndex];
                                var newPattern = SamplePatternForTemp(indexerTemp, constraintMap, evaluationMap, requireExactType: false, ref unnamedEnumValue);
                                subpatterns[effectiveIndex] = makeConjunct(oldPattern, newPattern);
                                continue;
                            case BoundDagSliceEvaluation e:
                                Debug.Assert(e == slice);
                                continue;
                            case var v:
                                throw ExceptionUtilities.UnexpectedValue(v);
                        }
                    }

                    if (slice != null)
                    {
                        var sliceTemp = new BoundDagTemp(slice.Syntax, slice.SliceType, slice);
                        var slicePattern = SamplePatternForTemp(sliceTemp, constraintMap, evaluationMap, requireExactType: false, ref unnamedEnumValue);
                        if (slicePattern != "_")
                        {
                            // If the slice is not matched against any pattern, the slice pattern would
                            // have no effect on the output given the provided sample length value.
                            subpatterns.Insert(slice.StartIndex, $".. {slicePattern}");
                        }
                    }

                    return "[" + string.Join(", ", subpatterns) + "]";
                }

                return null;
            }

            // Handle the special case of a tuple pattern
            string tryHandleTuplePattern(ref bool unnamedEnumValue)
            {
                if (input.Type.IsTupleType &&
                    constraints.IsEmpty &&
                    evaluations.All(e => e is BoundDagFieldEvaluation { Field: var field } && field.IsTupleElement()))
                {
                    var elements = input.Type.TupleElements;
                    int cardinality = elements.Length;
                    var subpatterns = new ArrayBuilder<string>(cardinality);
                    subpatterns.AddMany("_", cardinality);
                    foreach (BoundDagFieldEvaluation e in evaluations)
                    {
                        var elementTemp = new BoundDagTemp(e.Syntax, e.Field.Type, e);
                        var index = e.Field.TupleElementIndex;
                        if (index < 0 || index >= cardinality)
                            return null;

                        var oldPattern = subpatterns[index];
                        var newPattern = SamplePatternForTemp(elementTemp, constraintMap, evaluationMap, requireExactType: false, ref unnamedEnumValue);
                        subpatterns[index] = makeConjunct(oldPattern, newPattern);
                    }

                    return "(" + string.Join(", ", subpatterns) + ")" + (subpatterns.Count == 1 ? " { }" : null);
                }

                return null;
            }

            // Handle the special case of numeric limits
            string tryHandleNumericLimits(ref bool unnamedEnumValue)
            {
                if (evaluations.IsEmpty &&
                    constraints.All(t => t switch
                    {
                        (BoundDagValueTest _, _) => true,
                        (BoundDagRelationalTest _, _) => true,
                        (BoundDagExplicitNullTest _, false) => true,
                        (BoundDagNonNullTest _, true) => true,
                        _ => false
                    }) &&
                    ValueSetFactory.ForInput(input) is { } fac)
                {
                    // All we have are numeric constraints. Process them to compute a value not covered.
                    IValueSet remainingValues = computeRemainingValues(fac, constraints);
                    if (remainingValues.Complement().IsEmpty)
                        return "_";

                    return SampleValueString(remainingValues, input.Type, requireExactType: requireExactType, unnamedEnumValue: ref unnamedEnumValue);
                }

                return null;
            }

            // Handle the special case of a recursive pattern
            string tryHandleRecursivePattern(ref bool unnamedEnumValue)
            {
                if (constraints.IsEmpty && evaluations.IsEmpty)
                    return null;

                // not-null tests are implicitly incorporated into a recursive pattern
                if (!constraints.All(isNotNullTest))
                {
                    return null;
                }

                string deconstruction = null;
                var properties = new Dictionary<Symbol, string>();
                bool needsPropertyString = false;

                foreach (var eval in evaluations)
                {
                    switch (eval)
                    {
                        case BoundDagDeconstructEvaluation e:
                            var method = e.DeconstructMethod;
                            int extensionExtra = method.RequiresInstanceReceiver ? 0 : 1;
                            int count = method.Parameters.Length - extensionExtra;
                            var subpatternBuilder = new StringBuilder("(");
                            for (int j = 0; j < count; j++)
                            {
                                var elementTemp = new BoundDagTemp(e.Syntax, method.Parameters[j + extensionExtra].Type, e, j);
                                var newPattern = SamplePatternForTemp(elementTemp, constraintMap, evaluationMap, requireExactType: false, ref unnamedEnumValue);
                                if (j != 0)
                                    subpatternBuilder.Append(", ");
                                subpatternBuilder.Append(newPattern);
                            }
                            subpatternBuilder.Append(')');
                            var result = subpatternBuilder.ToString();
                            if (deconstruction != null && needsPropertyString)
                            {
                                deconstruction = deconstruction + " { }";
                                needsPropertyString = properties.Count != 0;
                            }

                            deconstruction = (deconstruction is null) ? result : deconstruction + " and " + result;
                            needsPropertyString |= count == 1;
                            break;
                        case BoundDagFieldEvaluation e:
                            {
                                var subInput = new BoundDagTemp(e.Syntax, e.Field.Type, e);
                                var subPattern = SamplePatternForTemp(subInput, constraintMap, evaluationMap, false, ref unnamedEnumValue);
                                properties.Add(e.Field, subPattern);
                            }
                            break;
                        case BoundDagPropertyEvaluation e:
                            {
                                var subInput = new BoundDagTemp(e.Syntax, e.Property.Type, e);
                                var subPattern = SamplePatternForTemp(subInput, constraintMap, evaluationMap, false, ref unnamedEnumValue);
                                properties.Add(e.Property, subPattern);
                            }
                            break;
                        default:
                            return null;
                    }
                }

                string typeName = requireExactType ? input.Type.ToDisplayString() : null;
                needsPropertyString |= deconstruction == null && typeName == null || properties.Count != 0;
                var propertyString = needsPropertyString ? (deconstruction != null ? " {" : "{") + string.Join(", ", properties.Select(kvp => $" {kvp.Key.Name}: {kvp.Value}")) + " }" : null;
                Debug.Assert(typeName != null || deconstruction != null || propertyString != null);
                return typeName + deconstruction + propertyString;
            }

            // Produce a fallback pattern when we were not able to produce a more specific pattern.
            string produceFallbackPattern()
            {
                return requireExactType ? input.Type.ToDisplayString() : "_";
            }

            IValueSet computeRemainingValues(IValueSetFactory fac, ImmutableArray<(BoundDagTest test, bool sense)> constraints)
            {
                var remainingValues = fac.AllValues;
                foreach (var constraint in constraints)
                {
                    var (test, sense) = constraint;
                    switch (test)
                    {
                        case BoundDagValueTest v:
                            addRelation(BinaryOperatorKind.Equal, v.Value);
                            break;
                        case BoundDagRelationalTest r:
                            addRelation(r.Relation, r.Value);
                            break;
                    }

                    void addRelation(BinaryOperatorKind relation, ConstantValue value)
                    {
                        if (value.IsBad)
                            return;
                        var filtered = fac.Related(relation, value);
                        if (!sense)
                            filtered = filtered.Complement();
                        remainingValues = remainingValues.Intersect(filtered);
                    }
                }

                return remainingValues;
            }

            static string makeConjunct(string oldPattern, string newPattern) => (oldPattern, newPattern) switch
            {
                ("_", var x) => x,
                (var x, "_") => x,
                (var x, var y) => x + " and " + y
            };

            static bool isNotNullTest((BoundDagTest test, bool sense) constraint)
            {
                return constraint is
                    (test: BoundDagNonNullTest _, sense: true) or
                    (test: BoundDagExplicitNullTest _, sense: false);
            }
        }

        private static string SampleValueString(IValueSet remainingValues, TypeSymbol type, bool requireExactType, ref bool unnamedEnumValue)
        {
            // In rare cases it's possible the DAG path we analyzed yields empty remaining values
            if (remainingValues.IsEmpty)
                throw new NoRemainingValuesException();

            // If the input is an enumeration type, see if any declared enumeration constant values are in the set.
            // If so, that is what to report.
            if (type is NamedTypeSymbol { TypeKind: TypeKind.Enum } e)
            {
                foreach (var declaredMember in e.GetMembers())
                {
                    if (declaredMember is FieldSymbol { IsConst: true, IsStatic: true, DeclaredAccessibility: Accessibility.Public } field &&
                        field.GetConstantValue(ConstantFieldsInProgress.Empty, false) is ConstantValue constantValue &&
                        remainingValues.Any(BinaryOperatorKind.Equal, constantValue))
                    {
                        return field.ToDisplayString();
                    }
                }

                unnamedEnumValue = true;
            }

            var sample = remainingValues.Sample;
            if (sample != null)
                return ValueString(sample, type, requireExactType);

            // IValueSet.Sample cannot produce a sample of type `nint` or `nuint` outside the range
            // of values of `int` and `uint`. So if we get here we need to produce a pattern indicating
            // such an out-of-range value.

            var underlyingType = type.EnumUnderlyingTypeOrSelf();
            Debug.Assert(underlyingType.IsNativeIntegerType);
            if (underlyingType.SpecialType == SpecialType.System_IntPtr)
            {
                if (remainingValues.Any(BinaryOperatorKind.GreaterThan, ConstantValue.Create(int.MaxValue)))
                    return $"> ({type.ToDisplayString()})int.MaxValue";

                if (remainingValues.Any(BinaryOperatorKind.LessThan, ConstantValue.Create(int.MinValue)))
                    return $"< ({type.ToDisplayString()})int.MinValue";
            }
            else if (underlyingType.SpecialType == SpecialType.System_UIntPtr)
            {
                if (remainingValues.Any(BinaryOperatorKind.GreaterThan, ConstantValue.Create(uint.MaxValue)))
                    return $"> ({type.ToDisplayString()})uint.MaxValue";
            }

            throw ExceptionUtilities.Unreachable();
        }

        private static string ValueString(ConstantValue value, TypeSymbol type, bool requireExactType)
        {
            bool requiresCast = (type.IsEnumType() || requireExactType || type.IsNativeIntegerType) &&
                !(typeHasExactTypeLiteral(type) && !value.IsNull);
            string valueString = PrimitiveValueString(value, type.EnumUnderlyingTypeOrSelf());
            return requiresCast ? $"({type.ToDisplayString()}){valueString}" : valueString;

            static bool typeHasExactTypeLiteral(TypeSymbol type) => type.SpecialType switch
            {
                SpecialType.System_Int32 => true,
                SpecialType.System_Int64 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_UInt64 => true,
                SpecialType.System_String => true,
                SpecialType.System_Decimal => true,
                SpecialType.System_Single => true,
                SpecialType.System_Double => true,
                SpecialType.System_Boolean => true,
                SpecialType.System_Char => true,
                _ => false,
            };
        }

        private static string PrimitiveValueString(ConstantValue value, TypeSymbol type)
        {
            if (value.IsNull)
                return "null";

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr when type.IsNativeIntegerType:
                case SpecialType.System_UIntPtr when type.IsNativeIntegerType:
                case SpecialType.System_Decimal:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                    return ObjectDisplay.FormatPrimitive(value.Value, ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeTypeSuffix | ObjectDisplayOptions.UseQuotes);

                case SpecialType.System_Single:
                    return value.SingleValue switch
                    {
                        float.NaN => "float.NaN",
                        float.NegativeInfinity => "float.NegativeInfinity",
                        float.PositiveInfinity => "float.PositiveInfinity",
                        var x => ObjectDisplay.FormatPrimitive(x, ObjectDisplayOptions.IncludeTypeSuffix)
                    };

                case SpecialType.System_Double:
                    return value.DoubleValue switch
                    {
                        double.NaN => "double.NaN",
                        double.NegativeInfinity => "double.NegativeInfinity",
                        double.PositiveInfinity => "double.PositiveInfinity",
                        var x => ObjectDisplay.FormatPrimitive(x, ObjectDisplayOptions.IncludeTypeSuffix)
                    };

                default:
                    return "_";
            }
        }
    }
}
