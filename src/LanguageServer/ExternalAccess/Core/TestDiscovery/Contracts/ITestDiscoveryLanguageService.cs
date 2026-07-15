// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.ServiceHub.Client;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Contracts;

/// <summary>
/// Minimal, service contract for initializing source-based test discovery.
/// <para>
/// The discovery RPC contract is defined in the C# Dev Kit project so the discovery surface can evolve
/// without changing Roslyn. The only stable coupling between Roslyn and C# Dev Kit is the service descriptor
/// (<see cref="ITestDiscoveryLanguageService.Descriptor"/>).
/// </para>
/// <para>
/// The implementation is exported via <c>[ExportWorkspaceService(typeof(ITestDiscoveryLanguageService))]</c>
/// and resolved from the host workspace.
/// </para>
/// </summary>
internal interface ITestDiscoveryLanguageService : IWorkspaceService
{
    /// <summary>
    /// The service descriptor for the source-based test discovery brokered service.
    /// </summary>
    ServiceRpcDescriptor Descriptor { get; }

    /// <summary>
    /// Called when the service broker has been fully initialized.
    /// </summary>
    /// <param name="serviceBroker">The fully-initialized service broker.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken);
}
