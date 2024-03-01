// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    /// <summary>
    /// Facade interface for getting various service brokers
    /// </summary>
    internal interface IBrokeredDebuggerServices
    {
        Task<IServiceBroker> GetServiceBrokerAsync();
        Task<IHotReloadSessionNotificationService?> GetHotReloadSessionNotificationServiceAsync();
        Task<IManagedHotReloadAgentManagerService?> GetManagedHotReloadAgentManagerServiceAsync();
        Task<IManagedHotReloadService?> GetManagedHotReloadServiceAsync();
    }
}
