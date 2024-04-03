// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    /// <summary>
    /// Workspace service responsible for starting a Visual Diagnostic session on the LSP server
    /// </summary>
    internal interface IVisualDiagnosticsLanguageService : IWorkspaceService, IDisposable
    {
        /// <summary>
        /// Initialize the diagnostic host
        /// </summary>
        /// <param name="serviceBroker">Service broker</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        Task InitializeAsync(IServiceBroker serviceBroker, CancellationToken token);
    }
}
