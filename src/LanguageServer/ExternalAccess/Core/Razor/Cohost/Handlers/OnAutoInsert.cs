// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class OnAutoInsert
{
    public static Task<VSInternalDocumentOnAutoInsertResponseItem?> GetOnAutoInsertResponseAsync(Document document, LinePosition linePosition, string character, FormattingOptions formattingOptions, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var services = document.Project.Solution.Services.ExportProvider
            .GetExports<IBraceCompletionService, LanguageMetadata>()
            .SelectAsArray(
                predicate: s => s.Metadata.Language == LanguageNames.CSharp,
                selector: s => s.Value);

        return OnAutoInsertHandler.GetOnAutoInsertResponseAsync(globalOptions, services, document, linePosition, character, formattingOptions, includeNewLineBraceFormatting: true, cancellationToken);
    }
}
