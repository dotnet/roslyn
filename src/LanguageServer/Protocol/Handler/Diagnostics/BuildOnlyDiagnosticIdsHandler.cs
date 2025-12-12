// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed record class BuildOnlyDiagnosticIdsResult([property: JsonPropertyName("ids")] string[] Ids);

[ExportCSharpVisualBasicStatelessLspService(typeof(BuildOnlyDiagnosticIdsHandler)), Shared]
[Method(BuildOnlyDiagnosticIdsMethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BuildOnlyDiagnosticIdsHandler(
    [ImportMany] IEnumerable<Lazy<ILspBuildOnlyDiagnostics, ILspBuildOnlyDiagnosticsMetadata>> compilerBuildOnlyDiagnosticsProviders)
    : ILspServiceRequestHandler<BuildOnlyDiagnosticIdsResult>
{
    public const string BuildOnlyDiagnosticIdsMethodName = "workspace/buildOnlyDiagnosticIds";

    private readonly ImmutableDictionary<string, string[]> _compilerBuildOnlyDiagnosticIds = compilerBuildOnlyDiagnosticsProviders
        .ToImmutableDictionary(lazy => lazy.Metadata.LanguageName, lazy => lazy.Metadata.BuildOnlyDiagnostics);

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public async Task<BuildOnlyDiagnosticIdsResult> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var solution = context.Solution;
        Contract.ThrowIfNull(solution);

        using var _ = ArrayBuilder<string>.GetInstance(out var builder);
        foreach (var languageName in solution.Projects.Select(p => p.Language).Distinct())
        {
            if (_compilerBuildOnlyDiagnosticIds.TryGetValue(languageName, out var compilerBuildOnlyDiagnosticIds))
                builder.AddRange(compilerBuildOnlyDiagnosticIds);
        }

        var diagnosticService = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        builder.AddRange(await diagnosticService.GetCompilationEndDiagnosticDescriptorIdsAsync(
            solution, cancellationToken).ConfigureAwait(false));

        return new BuildOnlyDiagnosticIdsResult(builder.ToArray());
    }
}
