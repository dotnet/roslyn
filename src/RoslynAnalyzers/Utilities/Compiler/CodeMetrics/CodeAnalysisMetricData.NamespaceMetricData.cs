// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class NamespaceMetricData : CodeAnalysisMetricData
        {
            internal NamespaceMetricData(
                INamespaceSymbol symbol,
                int maintainabilityIndex,
                ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
                long linesOfCode,
                int cyclomaticComplexity,
                int? depthOfInheritance,
                ImmutableArray<CodeAnalysisMetricData> children)
                : base(symbol, maintainabilityIndex, ComputationalComplexityMetrics.Default,
                      coupledNamedTypes, linesOfCode, cyclomaticComplexity, depthOfInheritance, children)
            {
            }

            internal static async Task<NamespaceMetricData> ComputeAsync(INamespaceSymbol @namespace, CodeMetricsAnalysisContext context)
            {
                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(GetChildSymbols(@namespace), context).ConfigureAwait(false);
                return ComputeFromChildren(@namespace, children, context);
            }

            internal static NamespaceMetricData ComputeSynchronously(INamespaceSymbol @namespace, CodeMetricsAnalysisContext context)
            {
                ImmutableArray<CodeAnalysisMetricData> children = ComputeSynchronously(GetChildSymbols(@namespace), context);
                return ComputeFromChildren(@namespace, children, context);
            }

            private static NamespaceMetricData ComputeFromChildren(INamespaceSymbol @namespace, ImmutableArray<CodeAnalysisMetricData> children, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                int maintainabilityIndexTotal = 0;
                int cyclomaticComplexity = 0;
                int depthOfInheritance = 0;
                long childrenLinesOfCode = 0;

                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, child.CoupledNamedTypes);
                    maintainabilityIndexTotal += child.MaintainabilityIndex;
                    cyclomaticComplexity += child.CyclomaticComplexity;
                    depthOfInheritance = Math.Max(child.DepthOfInheritance.GetValueOrDefault(), depthOfInheritance);

                    // Avoid double counting lines for nested types.
                    if (child.Symbol.ContainingType == null)
                    {
                        childrenLinesOfCode += child.SourceLines;
                    }
                }

                long linesOfCode = @namespace.IsImplicitlyDeclared ?
                    childrenLinesOfCode :
                    MetricsHelper.GetLinesOfCode(@namespace.DeclaringSyntaxReferences, @namespace, context);
                int maintainabilityIndex = !children.IsEmpty ? MetricsHelper.GetAverageRoundedMetricValue(maintainabilityIndexTotal, children.Length) : 100;
                return new NamespaceMetricData(@namespace, maintainabilityIndex,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance, children);
            }

            private static ImmutableArray<INamespaceOrTypeSymbol> GetChildSymbols(INamespaceSymbol @namespace)
            {
                // Compat: Create child nodes for types and nested types within the namespace.
                //         Child namespaces are directly child nodes of assembly.
                var typesInNamespace = new HashSet<INamedTypeSymbol>();
                foreach (INamedTypeSymbol typeMember in @namespace.GetTypeMembers())
                {
                    processType(typeMember);
                }

                var builder = ImmutableArray.CreateBuilder<INamespaceOrTypeSymbol>();
                foreach (INamedTypeSymbol namedType in typesInNamespace.OrderBy(t => t.ToDisplayString()))
                {
                    builder.Add(namedType);
                }

                return builder.ToImmutable();

                void processType(INamedTypeSymbol namedType)
                {
                    typesInNamespace.Add(namedType);
                    foreach (INamedTypeSymbol nestedType in namedType.GetTypeMembers())
                    {
                        processType(nestedType);
                    }
                }
            }
        }
    }
}
