// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class AssemblyMetricData : CodeAnalysisMetricData
        {
            private AssemblyMetricData(
                IAssemblySymbol symbol, int maintainabilityIndex,
                ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
                long linesOfCode,
                int cyclomaticComplexity,
                int? depthOfInheritance,
                ImmutableArray<CodeAnalysisMetricData> children)
                : base(symbol, maintainabilityIndex, ComputationalComplexityMetrics.Default,
                      coupledNamedTypes, linesOfCode, cyclomaticComplexity, depthOfInheritance, children)
            {
            }

            internal static async Task<AssemblyMetricData> ComputeAsync(IAssemblySymbol assembly, CodeMetricsAnalysisContext context)
            {
                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(GetChildSymbols(assembly), context).ConfigureAwait(false);
                return ComputeFromChildren(assembly, children, context);
            }

            internal static AssemblyMetricData ComputeSynchronously(IAssemblySymbol assembly, CodeMetricsAnalysisContext context)
            {
                ImmutableArray<CodeAnalysisMetricData> children = ComputeSynchronously(GetChildSymbols(assembly), context);
                return ComputeFromChildren(assembly, children, context);
            }

            private static AssemblyMetricData ComputeFromChildren(IAssemblySymbol assembly, ImmutableArray<CodeAnalysisMetricData> children, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                long linesOfCode = 0;
                int maintainabilityIndexTotal = 0;
                int cyclomaticComplexity = 0;
                int depthOfInheritance = 0;
                int grandChildCount = 0;

                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, child.CoupledNamedTypes);
                    linesOfCode += child.SourceLines;
                    cyclomaticComplexity += child.CyclomaticComplexity;
                    depthOfInheritance = Math.Max(child.DepthOfInheritance.GetValueOrDefault(), depthOfInheritance);

                    // Compat: Maintainability index of an assembly is computed based on the values of types, not namespace children.
                    Debug.Assert(child.Symbol.Kind == SymbolKind.Namespace);
                    Debug.Assert(!child.Children.IsEmpty);
                    Debug.Assert(child.Children.All(grandChild => grandChild.Symbol.Kind == SymbolKind.NamedType));
                    maintainabilityIndexTotal += child.MaintainabilityIndex * child.Children.Length;
                    grandChildCount += child.Children.Length;
                }

                int maintainabilityIndex = grandChildCount > 0 ? MetricsHelper.GetAverageRoundedMetricValue(maintainabilityIndexTotal, grandChildCount) : 100;
                return new AssemblyMetricData(assembly, maintainabilityIndex,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance, children);
            }

            private static ImmutableArray<INamespaceOrTypeSymbol> GetChildSymbols(IAssemblySymbol assembly)
            {
                // Compat: We only create child nodes for namespaces which have at least one type member.
                var includeGlobalNamespace = false;
                var namespacesWithTypeMember = new HashSet<INamespaceSymbol>();

                processNamespace(assembly.GlobalNamespace);

                var builder = ImmutableArray.CreateBuilder<INamespaceOrTypeSymbol>();

                if (includeGlobalNamespace)
                {
                    builder.Add(assembly.GlobalNamespace);
                }

                foreach (INamespaceSymbol @namespace in namespacesWithTypeMember.OrderBy(ns => ns.ToDisplayString()))
                {
                    builder.Add(@namespace);
                }

                return builder.ToImmutable();

                void processNamespace(INamespaceSymbol @namespace)
                {
                    foreach (INamespaceOrTypeSymbol child in @namespace.GetMembers())
                    {
                        if (child.Kind == SymbolKind.Namespace)
                        {
                            processNamespace((INamespaceSymbol)child);
                        }
                        else if (@namespace.IsGlobalNamespace)
                        {
                            includeGlobalNamespace = true;
                        }
                        else if (!child.IsImplicitlyDeclared)
                        {
                            namespacesWithTypeMember.Add(@namespace);
                        }
                    }
                }
            }
        }
    }
}
