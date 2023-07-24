// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler
{
    internal class TransformerDiagnosticSuppressor : DiagnosticSuppressor
    {
        private readonly DiagnosticFilters _filters;

        public TransformerDiagnosticSuppressor(DiagnosticFilters filters)
        {
            _filters = filters;
            this.SupportedSuppressions = filters.Filters.Select(f => f.Descriptor).ToImmutableArray();
        }

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                var location = diagnostic.Location;

                if (!location.IsInSource || TreeTracker.IsTransformedLocation(location))
                {
                    // We should not consider this diagnostic because it does not relate to source code.
                    continue;
                }

                if (!this._filters.FiltersByDiagnosticId.TryGetValue(diagnostic.Id, out var filters))
                {
                    // There is no filter for this diagnostic.
                    continue;
                }

                var model = context.GetSemanticModel(location.SourceTree);

                for (var node = location.SourceTree.GetRoot().FindNode(location.SourceSpan, getInnermostNodeForTie: true);
                    node != null;
                    node = node.Parent)
                {
                    var declaredSymbols = model.GetDeclaredSymbolsForNode(node);
                    Debug.Assert(declaredSymbols != null);

                    foreach (var symbol in declaredSymbols)
                    {
                        DiagnosticFilteringRequest request = new(diagnostic, node, context.Compilation, symbol);
                        foreach (var filter in filters)
                        {
                            filter.Filter(request);

                            if (request.IsSuppressed)
                            {
                                context.ReportSuppression(Suppression.Create(filter.Descriptor, diagnostic));
                                goto nextDiagnostic;

                            }
                        }
                    }
                }

nextDiagnostic:;
            }
        }
    }
}
