// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [DiagnosticAnalyzer(InternalLanguageNames.TypeScript)]
    internal sealed class VSTypeScriptDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            var analyzer = document.Project.Services.GetRequiredService<VSTypeScriptDiagnosticAnalyzerLanguageService>().Implementation;
            if (analyzer == null)
            {
                return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
            }

            return analyzer.AnalyzeDocumentSyntaxAsync(document, cancellationToken);
        }

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            var analyzer = document.Project.Services.GetRequiredService<VSTypeScriptDiagnosticAnalyzerLanguageService>().Implementation;
            if (analyzer == null)
            {
                return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
            }

            return analyzer.AnalyzeDocumentSemanticsAsync(document, cancellationToken);
        }
    }
}
