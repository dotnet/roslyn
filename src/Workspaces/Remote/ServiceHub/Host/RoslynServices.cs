// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Various Roslyn services provider.
    /// </summary>
    internal sealed class RoslynServices
    {
        private static readonly object s_hostServicesGuard = new object();

        /// <summary>
        /// This delegate allows test code to override the behavior of <see cref="HostServices"/>.
        /// </summary>
        /// <seealso cref="TestAccessor.HookHostServices"/>
        private static Func<HostServices>? s_hostServicesHook;
        private static HostServices? s_hostServices;

        internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
            MefHostServices.DefaultAssemblies
                .Add(typeof(RoslynServices).Assembly)
                .Add(typeof(RemotableDataService).Assembly);

        public static HostServices HostServices
        {
            get
            {
                if (s_hostServices != null)
                {
                    return s_hostServices;
                }

                lock (s_hostServicesGuard)
                {
                    return s_hostServices ??= s_hostServicesHook?.Invoke() ?? MefHostServices.Create(RemoteHostAssemblies);
                }
            }
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
#pragma warning disable IDE0052 // Remove unread private members - hold onto the Roslyn services.
            private readonly RoslynServices _roslynServices;
#pragma warning restore IDE0052 // Remove unread private members

            public TestAccessor(RoslynServices roslynServices)
                => _roslynServices = roslynServices;

            /// <summary>
            /// Injects replacement behavior for the <see cref="HostServices"/> property.
            /// </summary>
            internal static void HookHostServices(Func<HostServices> hook)
            {
                s_hostServicesHook = hook;

                // The existing container, if any, is not retained past this call.
                s_hostServices = null;
            }
        }
    }
}
