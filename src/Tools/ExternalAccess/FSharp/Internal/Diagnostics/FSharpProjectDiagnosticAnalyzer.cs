// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.FSharp)]
    internal class FSharpProjectDiagnosticAnalyzer : ProjectDiagnosticAnalyzer
    {
        private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

        public FSharpProjectDiagnosticAnalyzer()
        {
            _supportedDiagnostics = FSharpDocumentDiagnosticAnalyzer.CreateSupportedDiagnostics();
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var analyzer = project.LanguageServices.GetService<IFSharpProjectDiagnosticAnalyzer>();
            if (analyzer == null)
            {
                return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            }

            return analyzer.AnalyzeProjectAsync(project, cancellationToken);
        }
    }
}
