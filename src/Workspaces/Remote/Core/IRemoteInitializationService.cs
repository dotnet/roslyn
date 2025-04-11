// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote;

internal interface IRemoteInitializationService
{
    /// <summary>
    /// Initializes values including <see cref="WorkspaceConfigurationOptions"/> for the process.
    /// Called as soon as the remote process is created but can't guarantee that solution entities (projects, documents, syntax trees) have not been created beforehand.
    /// </summary>
    /// <returns>Process ID of the remote process.</returns>
    ValueTask<int> InitializeAsync(WorkspaceConfigurationOptions options, string localSettingsDirectory, CancellationToken cancellationToken);
}
