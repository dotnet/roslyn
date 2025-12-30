// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation;

[ExportWorkspaceService(typeof(INavigateToLinkService), layer: ServiceLayer.Default)]
[Shared]
internal sealed class DefaultNavigateToLinkService : INavigateToLinkService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultNavigateToLinkService()
    {
    }

    public Task<bool> TryNavigateToLinkAsync(Uri uri, CancellationToken cancellationToken)
        => SpecializedTasks.False;
}
