// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

internal abstract partial class AbstractObjectBrowserLibraryManager
{
    internal async Task<bool> TryFillDescriptionAsync(
        ObjectListItem listItem,
        IVsObjectBrowserDescription3 description,
        _VSOBJDESCOPTIONS options,
        CancellationToken cancellationToken)
    {
        var project = GetProject(listItem);
        if (project == null)
            return false;

        return await CreateDescriptionBuilder(description, listItem, project)
            .TryBuildAsync(options, cancellationToken)
            .ConfigureAwait(true);
    }
}
