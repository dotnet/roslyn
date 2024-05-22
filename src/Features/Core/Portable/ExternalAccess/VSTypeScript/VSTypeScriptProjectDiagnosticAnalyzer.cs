// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[DiagnosticAnalyzer(InternalLanguageNames.TypeScript)]
internal sealed class VSTypeScriptProjectDiagnosticAnalyzer : ProjectDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];

    public override Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
    {
        var analyzer = project.Services.GetRequiredService<VSTypeScriptDiagnosticAnalyzerLanguageService>().Implementation;
        if (analyzer == null)
        {
            return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
        }

        return analyzer.AnalyzeProjectAsync(project, cancellationToken);
    }
}
