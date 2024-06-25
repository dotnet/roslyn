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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal record class BuildOnlyDiagnosticIdsResult([property: JsonPropertyName("ids")] string[] Ids);

[ExportCSharpVisualBasicStatelessLspService(typeof(BuildOnlyDiagnosticIdsHandler)), Shared]
[Method(BuildOnlyDiagnosticIdsMethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BuildOnlyDiagnosticIdsHandler(
    DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
    [ImportMany] IEnumerable<Lazy<ILspBuildOnlyDiagnostics, ILspBuildOnlyDiagnosticsMetadata>> compilerBuildOnlyDiagnosticsProviders)
                : ILspServiceRequestHandler<BuildOnlyDiagnosticIdsResult>
{
    public const string BuildOnlyDiagnosticIdsMethodName = "workspace/buildOnlyDiagnosticIds";

    private readonly DiagnosticAnalyzerInfoCache.SharedGlobalCache _globalCache = globalCache;
    private readonly ImmutableDictionary<string, string[]> _compilerBuildOnlyDiagnosticIds = compilerBuildOnlyDiagnosticsProviders
        .ToImmutableDictionary(lazy => lazy.Metadata.LanguageName, lazy => lazy.Metadata.BuildOnlyDiagnostics);

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public Task<BuildOnlyDiagnosticIdsResult> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        using var _1 = ArrayBuilder<string>.GetInstance(out var builder);
        foreach (var languageName in context.Solution.Projects.Select(p => p.Language).Distinct())
        {
            if (_compilerBuildOnlyDiagnosticIds.TryGetValue(languageName, out var compilerBuildOnlyDiagnosticIds))
            {
                builder.AddRange(compilerBuildOnlyDiagnosticIds);
            }
        }

        using var _2 = PooledHashSet<(object Reference, string Language)>.GetInstance(out var seenAnalyzerReferencesByLanguage);

        foreach (var project in context.Solution.Projects)
        {
            var analyzersPerReferenceMap = context.Solution.SolutionState.Analyzers.CreateDiagnosticAnalyzersPerReference(project);
            foreach (var (analyzerReference, analyzers) in analyzersPerReferenceMap)
            {
                if (!seenAnalyzerReferencesByLanguage.Add((analyzerReference, project.Language)))
                    continue;

                foreach (var analyzer in analyzers)
                {
                    // We have already added the compiler build-only diagnostics upfront.
                    if (analyzer.IsCompilerAnalyzer())
                        continue;

                    foreach (var buildOnlyDescriptor in _globalCache.AnalyzerInfoCache.GetCompilationEndDiagnosticDescriptors(analyzer))
                    {
                        builder.Add(buildOnlyDescriptor.Id);
                    }
                }
            }
        }

        return Task.FromResult(new BuildOnlyDiagnosticIdsResult(builder.ToArray()));
    }
}
