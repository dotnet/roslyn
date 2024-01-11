// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
//using System.Composition;
using System.ComponentModel.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal
{
    internal interface IHostDebuggerServices
    {
        public void AttacheToDebuggerEvent();
    }

    [Export(typeof(IHostDebuggerServices))]
    internal sealed class HostDebuggerServices : IHostDebuggerServices
    {
        IServiceBroker _serviceBroker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HostDebuggerServices(
        [Import(typeof(SVsFullAccessServiceBroker))]
        IServiceBroker serviceBroker)
        {
            _serviceBroker = serviceBroker;
        }

        //[ImportingConstructor]
        //[Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        //public HostDebuggerServices()
        //{
        //}

        public void AttacheToDebuggerEvent()
        {
            var serviceBroker = _serviceBroker;
        }
    }
}
