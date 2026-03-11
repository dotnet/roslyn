// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities;

[ExportDynamicFileInfoProvider("cshtml", "vbhtml")]
[Shared]
[PartNotDiscoverable]
internal sealed class TestDynamicFileInfoProviderThatProducesNoFiles : IDynamicFileInfoProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestDynamicFileInfoProviderThatProducesNoFiles()
    {
    }

    event EventHandler<string> IDynamicFileInfoProvider.Updated { add { } remove { } }

    public async Task<DynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        => null;

    public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
