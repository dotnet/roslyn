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
    /// Called as soon as the remote process is created.
    /// </summary>
    /// <returns>Process ID of the remote process and an error message if the server encountered initialization issues.</returns>
    ValueTask<(int processId, string? errorMessage)> InitializeAsync(WorkspaceConfigurationOptions options, string localSettingsDirectory, CancellationToken cancellationToken);
}
