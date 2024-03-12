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
        private readonly IServiceBroker _serviceBroker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualDiagnosticsBrokeredDebuggerServices(
        [Import(typeof(SVsFullAccessServiceBroker))]
        IServiceBroker serviceBroker)
        {
            _serviceBroker = serviceBroker;
        }

        public Task<IServiceBroker> GetServiceBrokerAsync()
        {
            return Task.FromResult<IServiceBroker>(_serviceBroker);
        }
    }
}
