// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

using Location = Roslyn.LanguageServer.Protocol.Location;

internal static class FindAllReferences
{
    public static async Task<SumType<VSInternalReferenceItem, Location>[]?> FindReferencesAsync(Workspace workspace, Document document, LinePosition linePosition, bool supportsVSExtensions, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var metadataAsSourceFileService = document.Project.Solution.Services.ExportProvider.GetService<IMetadataAsSourceFileService>();

        // Passing null here means this will just collect items in an array builder
        var progress = BufferedProgress.Create<SumType<VSInternalReferenceItem, Location>[]>(progress: null);

        await FindAllReferencesHandler.FindReferencesAsync(progress, workspace, document, linePosition, supportsVSExtensions, includeDeclaration: true, globalOptions, metadataAsSourceFileService, AsynchronousOperationListenerProvider.NullListener, cancellationToken).ConfigureAwait(false);

        return progress.GetFlattenedValues();
    }
}
