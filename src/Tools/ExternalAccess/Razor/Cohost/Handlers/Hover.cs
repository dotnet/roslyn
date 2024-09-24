// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class Hover
{
    public static Task<LSP.Hover?> GetHoverAsync(
        Document document,
        LinePosition linePosition,
        bool supportsVSExtensions,
        bool supportsMarkdown,
        CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var options = globalOptions.GetSymbolDescriptionOptions(document.Project.Language);

        return HoverHandler.GetHoverAsync(document, linePosition, options, supportsVSExtensions, supportsMarkdown, cancellationToken);
    }
}
