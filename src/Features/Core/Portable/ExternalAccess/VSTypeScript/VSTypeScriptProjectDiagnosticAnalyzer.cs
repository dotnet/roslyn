// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [DiagnosticAnalyzer(InternalLanguageNames.TypeScript)]
    internal sealed class VSTypeScriptProjectDiagnosticAnalyzer : ProjectDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var analyzer = project.LanguageServices.GetRequiredService<VSTypeScriptDiagnosticAnalyzerLanguageService>().Implementation;
            if (analyzer == null)
            {
                return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            }

            return analyzer.AnalyzeProjectAsync(project, cancellationToken);
        }
    }
}
