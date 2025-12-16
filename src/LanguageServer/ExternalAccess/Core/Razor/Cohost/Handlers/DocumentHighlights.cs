// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class DocumentHighlights
{
    public static Task<DocumentHighlight[]?> GetHighlightsAsync(Document document, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var highlightingService = document.Project.Solution.Services.ExportProvider.GetService<IHighlightingService>();

        return DocumentHighlightsHandler.GetHighlightsAsync(globalOptions, highlightingService, document, linePosition, cancellationToken);
    }
}
