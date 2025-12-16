// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

using Location = Roslyn.LanguageServer.Protocol.Location;

internal static class GoToDefinition
{
    public static Task<Location[]?> GetDefinitionsAsync(Workspace workspace, Document document, bool typeOnly, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var metadataAsSourceFileService = document.Project.Solution.Services.ExportProvider.GetService<IMetadataAsSourceFileService>();

        return AbstractGoToDefinitionHandler.GetDefinitionsAsync(globalOptions, metadataAsSourceFileService, workspace, document, typeOnly, linePosition, cancellationToken);
    }
}
