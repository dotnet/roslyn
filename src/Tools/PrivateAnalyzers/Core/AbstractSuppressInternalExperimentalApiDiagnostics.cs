// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

internal abstract class AbstractSuppressInternalExperimentalApiDiagnostics : DiagnosticSuppressor
{
    private static readonly ImmutableArray<SuppressionDescriptor> s_supportedSuppressions;

    static AbstractSuppressInternalExperimentalApiDiagnostics()
    {
        var builder = ImmutableArray.CreateBuilder<SuppressionDescriptor>();
        foreach (var field in typeof(ExperimentalApis).GetTypeInfo().DeclaredFields)
        {
            if (field.IsStatic && field.GetRawConstantValue() is string experimentalApi)
            {
                builder.Add(new SuppressionDescriptor(PrivateDiagnosticIds.AllowInternalExperiments, experimentalApi, "Allow use of private experiments in non-public APIs."));
            }
        }

        s_supportedSuppressions = builder.ToImmutable();
    }

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => s_supportedSuppressions;

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (!diagnostic.Location.IsInSource)
                continue;

            var tree = diagnostic.Location.SourceTree;
            var root = tree.GetRoot(context.CancellationToken);
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (IsExposedLocation(context, token, context.CancellationToken))
                continue;

            var descriptor = SupportedSuppressions.First(suppression => suppression.SuppressedDiagnosticId == diagnostic.Id);
            context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
        }
    }

    protected abstract bool IsExposedLocation(SuppressionAnalysisContext context, SyntaxToken token, CancellationToken cancellationToken);
}
