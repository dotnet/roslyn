// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal
{
    /// <summary>
    /// This is a simple wrapper to export IOnServiceBrokerInitialized to the ServiceBrokerFactory and delegate back to 
    /// to the LSP services once the broker service is initialized. 
    /// </summary>
    [Export]
    [Export(typeof(IOnServiceBrokerInitialized))]
    internal sealed class VisualDiagnosticsServiceBroker : IOnServiceBrokerInitialized
    {
        public IOnServiceBrokerInitialized? NotifyServiceBrokerInitialized { get; set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualDiagnosticsServiceBroker()
        {
        }

        public void OnServiceBrokerInitialized(IServiceBroker serviceBroker)
        {
            NotifyServiceBrokerInitialized?.OnServiceBrokerInitialized(serviceBroker);
        }
    }
}
