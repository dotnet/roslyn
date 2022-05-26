// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Host
{
    internal interface IGlobalServiceBroker
    {
        IServiceBroker Instance { get; }
    }

    /// <summary>
    /// Hacky way to expose a <see cref="IServiceBroker"/> to workspace services that expect there to be a global
    /// singleton (like in visual studio).  Effectively the first service that gets called into will record its
    /// broker here for these services to use.
    /// </summary>
    // Note: this Export is only so MEF picks up the exported member internally.
    [Export(typeof(IGlobalServiceBroker)), Shared]
    internal class GlobalServiceBroker : IGlobalServiceBroker
    {
        private static IServiceBroker? s_instance;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GlobalServiceBroker()
        {
        }

        public static void RegisterServiceBroker(IServiceBroker serviceBroker)
        {
            Interlocked.CompareExchange(ref s_instance, serviceBroker, null);
        }

        public IServiceBroker Instance
        {
            get
            {
                Contract.ThrowIfNull(s_instance, "Global service broker not registered");
                return s_instance;
            }
        }
    }
}
