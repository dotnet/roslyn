// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Various Roslyn services provider.
    /// 
    /// TODO: change all these services to WorkspaceServices
    /// </summary>
    internal sealed class RoslynServices
    {
        private static TelemetrySession? s_telemetrySession;

        private static readonly object s_hostServicesGuard = new object();

        /// <summary>
        /// This delegate allows test code to override the behavior of <see cref="HostServices"/>.
        /// </summary>
        /// <seealso cref="TestAccessor.HookHostServices"/>
        private static Func<HostServices>? s_hostServicesHook;
        private static HostServices? s_hostServices;

        // TODO: probably need to split this to private and public services
        public static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
            MefHostServices.DefaultAssemblies
                // This adds the exported MEF services from the RemoteWorkspaces assembly.
                .Add(typeof(RoslynServices).Assembly)
                .Add(typeof(ICodingConventionsManager).Assembly);

        public static HostServices HostServices
        {
            get
            {
                if (s_hostServicesHook != null)
                {
                    return s_hostServicesHook();
                }

                if (s_hostServices != null)
                {
                    return s_hostServices;
                }

                lock (s_hostServicesGuard)
                {
                    return s_hostServices ?? (s_hostServices = MefHostServices.Create(RemoteHostAssemblies));
                }
            }
        }

        /// <summary>
        /// Set default telemetry session
        /// </summary>
        public static void SetTelemetrySession(TelemetrySession session)
            => s_telemetrySession = session;

        /// <summary>
        /// Default telemetry session
        /// </summary>
        public static TelemetrySession? TelemetrySession => s_telemetrySession;

        /// <summary>
        /// Check whether current user is microsoft internal or not
        /// </summary>
        public static bool IsUserMicrosoftInternal => TelemetrySession?.IsUserMicrosoftInternal ?? false;

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
