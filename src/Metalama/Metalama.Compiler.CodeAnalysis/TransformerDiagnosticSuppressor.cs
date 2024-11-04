// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler;

internal class TransformerDiagnosticSuppressor : DiagnosticSuppressor
{
    private readonly DiagnosticFilterCollection _filters;

    public TransformerDiagnosticSuppressor(DiagnosticFilterCollection filters)
    {
        _filters = filters;

        SupportedSuppressions = filters.SuppressionDescriptors.ToImmutableArray();
    }

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var filterRunner = new DiagnosticFilterRunner(context.Compilation, context.GetSemanticModel, _filters);


        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var location = diagnostic.Location;

            if (!location.IsInSource || location.IsTransformedLocation())
            {
                // We should not consider this diagnostic because it does not relate to source code.
                continue;
            }

            var sourceTreeFilePath = location.SourceTree?.FilePath;

            if (sourceTreeFilePath == null)
            {
                continue;
            }


            if (!_filters.TryGetFilters(sourceTreeFilePath, diagnostic.Id, out var filters))
            {
                // There is no filter for this diagnostic.
                continue;
            }

         
            if (filterRunner.TryGetSuppression(diagnostic, context.CancellationToken, out var suppression))
            {
                context.ReportSuppression(suppression);
            }
        }
    }
}
