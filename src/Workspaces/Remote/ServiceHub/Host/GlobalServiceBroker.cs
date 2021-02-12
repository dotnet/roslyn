// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Host
{
    /// <summary>
    /// Hacky way to expose a <see cref="IServiceBroker"/> to workspace services that expect there to be a global
    /// singleton (like in visual studio).  Effectively the first service that gets called into will record its
    /// broker here for these services to use.
    /// </summary>
    internal static class GlobalServiceBroker
    {
        private static IServiceBroker? s_instance;

        public static void RegisterServiceBroker(IServiceBroker serviceBroker)
        {
            Interlocked.CompareExchange(ref s_instance, serviceBroker, null);
        }

        public static IServiceBroker GetGlobalInstance()
        {
            Contract.ThrowIfNull(s_instance, "Global service broker not registered");
            return s_instance;
        }
    }
}
