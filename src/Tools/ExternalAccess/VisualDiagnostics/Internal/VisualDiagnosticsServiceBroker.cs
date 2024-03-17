// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal
{
    /// <summary>
    /// This is a simple wrapper to succeed at getting the broker service using System.ComponentModel.Composition inside an LSP service OnInitialized factory
    /// </summary>
    [Export(typeof(IVisualDiagnosticsBrokeredDebuggerServices))]
    internal sealed class VisualDiagnosticsBrokeredDebuggerServices : IVisualDiagnosticsBrokeredDebuggerServices
    {
        private readonly Lazy<Task<IBrokeredServiceContainer>> _container;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualDiagnosticsBrokeredDebuggerServices(
        [Import(typeof(SVsBrokeredServiceContainer))]
        Lazy<Task<IBrokeredServiceContainer>> serviceBroker)
        {
            _container = serviceBroker;
        }

        public async Task<IServiceBroker> GetServiceBrokerAsync()
        {
            // Waiting on the container to be created
            await _container.Value.ConfigureAwait(false);
            return _container.Value.Result.GetFullAccessServiceBroker();
        }
    }
}
