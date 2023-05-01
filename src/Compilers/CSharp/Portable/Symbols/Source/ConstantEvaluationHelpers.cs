// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ConstantEvaluationHelpers
    {
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal readonly struct FieldInfo
        {
            public readonly SourceFieldSymbolWithSyntaxReference Field;
            public readonly bool StartsCycle;

            public FieldInfo(SourceFieldSymbolWithSyntaxReference field, bool startsCycle)
            {
                this.Field = field;
                this.StartsCycle = startsCycle;
            }

            private string GetDebuggerDisplay()
            {
                var value = this.Field.ToString();
                if (this.StartsCycle)
                {
                    value += " [cycle]";
                }
                return value;
            }
        }

        /// <summary>
        /// Generate a list containing the given field and all dependencies
        /// of that field that require evaluation. The list is ordered by
        /// dependencies, with fields with no dependencies first. Cycles are
        /// broken at the first field lexically in the cycle. If multiple threads
        /// call this method with the same field, the order of the fields
        /// returned should be the same, although some fields may be missing
        /// from the lists in some threads as other threads evaluate fields.
        /// </summary>
        internal static void OrderAllDependencies(
            this SourceFieldSymbolWithSyntaxReference field,
            ArrayBuilder<FieldInfo> order,
            bool earlyDecodingWellKnownAttributes)
        {
            Debug.Assert(order.Count == 0);

            var graph = PooledDictionary<SourceFieldSymbolWithSyntaxReference, Node<SourceFieldSymbolWithSyntaxReference>>.GetInstance();

            CreateGraph(graph, field, earlyDecodingWellKnownAttributes);

            Debug.Assert(graph.Count >= 1);
            CheckGraph(graph);

#if DEBUG
            var fields = ArrayBuilder<SourceFieldSymbolWithSyntaxReference>.GetInstance();
            fields.AddRange(graph.Keys);
#endif

            OrderGraph(graph, order);

#if DEBUG
            // Verify all entries in the graph are in the ordered list.
            var map = new HashSet<SourceFieldSymbolWithSyntaxReference>(order.Select(o => o.Field).Distinct());
            Debug.Assert(fields.All(f => map.Contains(f)));
            fields.Free();
#endif

            graph.Free();
        }

        private struct Node<T> where T : class
        {
            /// <summary>
            /// The set of fields on which the field depends.
            /// </summary>
            public ImmutableHashSet<T> Dependencies;

            /// <summary>
            /// The set of fields that depend on the field.
            /// </summary>
            public ImmutableHashSet<T> DependedOnBy;
        }

        /// <summary>
        /// Build a dependency graph (a map from
        /// field to dependencies).
        /// </summary>
        private static void CreateGraph(
            Dictionary<SourceFieldSymbolWithSyntaxReference, Node<SourceFieldSymbolWithSyntaxReference>> graph,
            SourceFieldSymbolWithSyntaxReference field,
            bool earlyDecodingWellKnownAttributes)
        {
            var pending = ArrayBuilder<SourceFieldSymbolWithSyntaxReference>.GetInstance();
            pending.Push(field);

            while (pending.Count > 0)
            {
                field = pending.Pop();

                Node<SourceFieldSymbolWithSyntaxReference> node;
                if (graph.TryGetValue(field, out node))
                {
                    if (node.Dependencies != null)
                    {
                        // Already visited node.
                        continue;
                    }
                }
                else
                {
                    node = new Node<SourceFieldSymbolWithSyntaxReference>();
                    node.DependedOnBy = ImmutableHashSet<SourceFieldSymbolWithSyntaxReference>.Empty;
                }

                var dependencies = field.GetConstantValueDependencies(earlyDecodingWellKnownAttributes);
                // GetConstantValueDependencies will return an empty set if
                // the constant value has already been calculated. That avoids
                // calculating the full graph repeatedly. For instance with
                // "enum E { M0 = 0, M1 = M0 + 1, ..., Mn = Mn-1 + 1 }", we'll calculate
                // the graph M0, ..., Mi for the first field we evaluate, Mi. But for
                // the next field, Mj, we should only calculate the graph Mi, ..., Mj.
                node.Dependencies = dependencies;
                graph[field] = node;

                foreach (var dependency in dependencies)
                {
                    pending.Push(dependency);

                    if (!graph.TryGetValue(dependency, out node))
                    {
                        node = new Node<SourceFieldSymbolWithSyntaxReference>();
                        node.DependedOnBy = ImmutableHashSet<SourceFieldSymbolWithSyntaxReference>.Empty;
                    }

                    node.DependedOnBy = node.DependedOnBy.Add(field);
                    graph[dependency] = node;
                }
            }

            pending.Free();
        }

        private static void OrderGraph(
            Dictionary<SourceFieldSymbolWithSyntaxReference, Node<SourceFieldSymbolWithSyntaxReference>> graph,
            ArrayBuilder<FieldInfo> order)
        {
            Debug.Assert(graph.Count > 0);

            PooledHashSet<SourceFieldSymbolWithSyntaxReference> lastUpdated = null;
            ArrayBuilder<SourceFieldSymbolWithSyntaxReference> fieldsInvolvedInCycles = null;

            while (graph.Count > 0)
            {
                // Get the set of fields in the graph that have no dependencies.
                var search = ((IEnumerable<SourceFieldSymbolWithSyntaxReference>)lastUpdated) ?? graph.Keys;
                var set = ArrayBuilder<SourceFieldSymbolWithSyntaxReference>.GetInstance();
                foreach (var field in search)
                {
                    Node<SourceFieldSymbolWithSyntaxReference> node;
                    if (graph.TryGetValue(field, out node))
                    {
                        if (node.Dependencies.Count == 0)
                        {
                            set.Add(field);
                        }
                    }
                }

                lastUpdated?.Free();
                lastUpdated = null;
                if (set.Count > 0)
                {
                    var updated = PooledHashSet<SourceFieldSymbolWithSyntaxReference>.GetInstance();

                    // Remove fields with no dependencies from the graph.
                    foreach (var field in set)
                    {
                        var node = graph[field];

                        // Remove the field from the Dependencies
                        // of each field that depends on it.
                        foreach (var dependedOnBy in node.DependedOnBy)
                        {
                            var n = graph[dependedOnBy];
                            n.Dependencies = n.Dependencies.Remove(field);
                            graph[dependedOnBy] = n;
                            updated.Add(dependedOnBy);
                        }

                        graph.Remove(field);
                    }

                    CheckGraph(graph);

                    // Add the set to the ordered list.
                    foreach (var item in set)
                    {
                        order.Add(new FieldInfo(item, startsCycle: false));
                    }

                    lastUpdated = updated;
                }
                else
                {
                    // All fields have dependencies which means all fields are involved
                    // in cycles. Break the first cycle found. (Note some fields may have
                    // dependencies but are not strictly part of any cycle. For instance,
                    // B and C in: "enum E { A = A | B, B = C, C = D, D = D }").
                    var field = GetStartOfFirstCycle(graph, ref fieldsInvolvedInCycles);

                    // Break the dependencies.
                    var node = graph[field];

                    // Remove the field from the DependedOnBy
                    // of each field it has as a dependency.
                    foreach (var dependency in node.Dependencies)
                    {
                        var n = graph[dependency];
                        n.DependedOnBy = n.DependedOnBy.Remove(field);
                        graph[dependency] = n;
                    }

                    node = graph[field];
                    var updated = PooledHashSet<SourceFieldSymbolWithSyntaxReference>.GetInstance();

                    // Remove the field from the Dependencies
                    // of each field that depends on it.
                    foreach (var dependedOnBy in node.DependedOnBy)
                    {
                        var n = graph[dependedOnBy];
                        n.Dependencies = n.Dependencies.Remove(field);
                        graph[dependedOnBy] = n;
                        updated.Add(dependedOnBy);
                    }

                    graph.Remove(field);

                    CheckGraph(graph);

                    // Add the start of the cycle to the ordered list.
                    order.Add(new FieldInfo(field, startsCycle: true));

                    lastUpdated = updated;
                }

                set.Free();
            }

            lastUpdated?.Free();
            fieldsInvolvedInCycles?.Free();
        }

        private static SourceFieldSymbolWithSyntaxReference GetStartOfFirstCycle(
            Dictionary<SourceFieldSymbolWithSyntaxReference, Node<SourceFieldSymbolWithSyntaxReference>> graph,
            ref ArrayBuilder<SourceFieldSymbolWithSyntaxReference> fieldsInvolvedInCycles)
        {
            Debug.Assert(graph.Count > 0);

            if (fieldsInvolvedInCycles is null)
            {
                fieldsInvolvedInCycles = ArrayBuilder<SourceFieldSymbolWithSyntaxReference>.GetInstance(graph.Count);
                // We sort fields that belong to the same compilation by location to process cycles in deterministic order.
                // Relative order between compilations is not important, cycles do not cross compilation boundaries. 
                fieldsInvolvedInCycles.AddRange(graph.Keys.GroupBy(static f => f.DeclaringCompilation).
                    SelectMany(static g => g.OrderByDescending((f1, f2) => g.Key.CompareSourceLocations(f1.ErrorLocation, f2.ErrorLocation))));
            }

            while (true)
            {
                SourceFieldSymbolWithSyntaxReference field = fieldsInvolvedInCycles.Pop();

                if (graph.ContainsKey(field) && IsPartOfCycle(graph, field))
                {
                    return field;
                }
            }
        }

        private static bool IsPartOfCycle(
            Dictionary<SourceFieldSymbolWithSyntaxReference, Node<SourceFieldSymbolWithSyntaxReference>> graph,
            SourceFieldSymbolWithSyntaxReference field)
        {
            var set = PooledHashSet<SourceFieldSymbolWithSyntaxReference>.GetInstance();
            var stack = ArrayBuilder<SourceFieldSymbolWithSyntaxReference>.GetInstance();

            SourceFieldSymbolWithSyntaxReference stopAt = field;
            bool result = false;
            stack.Push(field);

            while (stack.Count > 0)
            {
                field = stack.Pop();
                var node = graph[field];

                if (node.Dependencies.Contains(stopAt))
                {
                    result = true;
                    break;
                }

                foreach (var dependency in node.Dependencies)
                {
                    if (set.Add(dependency))
                    {
                        stack.Push(dependency);
                    }
                }
            }

            stack.Free();
            set.Free();
            return result;
        }

        [Conditional("DEBUG")]
        private static void CheckGraph(Dictionary<SourceFieldSymbolWithSyntaxReference, Node<SourceFieldSymbolWithSyntaxReference>> graph)
        {
            // Avoid O(n^2) behavior by checking
            // a maximum number of entries.
            int i = 10;

            foreach (var pair in graph)
            {
                var field = pair.Key;
                var node = pair.Value;

                Debug.Assert(node.Dependencies != null);
                Debug.Assert(node.DependedOnBy != null);

                foreach (var dependency in node.Dependencies)
                {
                    Node<SourceFieldSymbolWithSyntaxReference> n;
                    var ok = graph.TryGetValue(dependency, out n);
                    Debug.Assert(ok);
                    Debug.Assert(n.DependedOnBy.Contains(field));
                }

                foreach (var dependedOnBy in node.DependedOnBy)
                {
                    Node<SourceFieldSymbolWithSyntaxReference> n;
                    var ok = graph.TryGetValue(dependedOnBy, out n);
                    Debug.Assert(ok);
                    Debug.Assert(n.Dependencies.Contains(field));
                }

                i--;
                if (i == 0)
                {
                    break;
                }
            }

            Debug.Assert(graph.Values.Sum(n => n.DependedOnBy.Count) == graph.Values.Sum(n => n.Dependencies.Count));
        }
    }
}
