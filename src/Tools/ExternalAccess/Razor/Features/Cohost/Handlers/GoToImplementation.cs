// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

using Location = Roslyn.LanguageServer.Protocol.Location;

internal static class GoToImplementation
{
    public static Task<Location[]> FindImplementationsAsync(Document document, LinePosition linePosition, bool supportsVisualStudioExtensions, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var classificationOptions = globalOptions.GetClassificationOptionsProvider();

        return FindImplementationsHandler.FindImplementationsAsync(document, linePosition, classificationOptions, supportsVisualStudioExtensions, cancellationToken);
    }
}
