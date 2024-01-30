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

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.CodeFixes;

internal static class CopilotCodeFixHelper
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithNoFixAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        if (document.Project.Solution.Services.ExportProvider.GetExports<ICodeFixService>().SingleOrDefault()?.Value is not ICodeFixService codeFixService)
            return [];

        if (document.Project.Solution.Services.ExportProvider.GetExports<IGlobalOptionService>().SingleOrDefault()?.Value is not IGlobalOptionService globalOptionsService)
            return [];

        return await codeFixService.GetDiagnosticsWithNoFixAsync(document, span,
            new DefaultCodeActionRequestPriorityProvider(), globalOptionsService.GetCodeActionOptionsProvider(),
            cancellationToken).ConfigureAwait(false);
    }
}
