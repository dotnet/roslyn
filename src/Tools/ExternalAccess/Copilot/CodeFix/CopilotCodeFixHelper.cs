// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.CodeFix;

internal static class CopilotCodeFixHelper
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithNoCodeFixAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not SemanticModel semanticModel)
            return []; ;

        var diagnostics = semanticModel.GetDiagnostics(span, cancellationToken);
        if (diagnostics.IsEmpty)
            return [];

        if (document.Project.Solution.Services.ExportProvider.GetExports<ICodeFixService>().SingleOrDefault()?.Value is not ICodeFixService codeFixService)
            return diagnostics;

        if (document.Project.Solution.Services.ExportProvider.GetExports<IGlobalOptionService>().SingleOrDefault()?.Value is not IGlobalOptionService globalOptionsService)
            return diagnostics;

        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var remainingDiagnostics);
        remainingDiagnostics.AddRange(diagnostics);

        await foreach (var fixCollection in codeFixService.StreamFixesForDiagnosticsAsync(document, span,
                            diagnostics.Select(diagnostic => diagnostic.Id).ToImmutableArray(),
                            new DefaultCodeActionRequestPriorityProvider(), globalOptionsService.GetCodeActionOptionsProvider(),
                            cancellationToken))
        {
            RemoveFixasbleDiagnostics(fixCollection);
        }

        return remainingDiagnostics.ToImmutable();

        void RemoveFixasbleDiagnostics(CodeFixCollection fixCollection)
        {
            foreach (var fix in fixCollection.Fixes)
            {
                if (remainingDiagnostics.FirstOrDefault(diagnostic => fix.PrimaryDiagnostic.Id == diagnostic.Id &&
                                                                      fix.PrimaryDiagnostic.Location.SourceSpan == diagnostic.Location.SourceSpan)
                    is Diagnostic fixableDiagnostic)
                {
                    remainingDiagnostics.Remove(fixableDiagnostic);
                }
            }
        }
    }
}
