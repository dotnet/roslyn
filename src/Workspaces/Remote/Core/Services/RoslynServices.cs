// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
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
        private static TelemetrySession s_sessionOpt;

        private static readonly object s_hostServicesGuard = new object();

        /// <summary>
        /// This delegate allows test code to override the behavior of <see cref="HostServices"/>.
        /// </summary>
        /// <seealso cref="TestAccessor.HookHostServices"/>
        private static Func<HostServices> s_hostServicesHook;
        private static HostServices s_hostServices;

        // TODO: probably need to split this to private and public services
        public static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
            MefHostServices.DefaultAssemblies
                // This adds the exported MEF services from Workspaces.Desktop
                .Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly)
                // This adds the exported MEF services from the RemoteWorkspaces assembly.
                .Add(typeof(RoslynServices).Assembly)
                .Add(typeof(ICodingConventionsManager).Assembly)
                .Add(typeof(CSharp.CodeLens.CSharpCodeLensDisplayInfoService).Assembly)
                .Add(typeof(VisualBasic.CodeLens.VisualBasicDisplayInfoService).Assembly);

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

        private readonly int _scopeId;

        public RoslynServices(int scopeId, AssetStorage storage, HostServices hostServices)
        {
            _scopeId = scopeId;

            AssetService = new AssetService(_scopeId, storage, SolutionService.PrimaryWorkspace.Services.GetService<ISerializerService>());
            SolutionService = new SolutionService(AssetService);
            CompilationService = new CompilationService(SolutionService);
        }

        public AssetService AssetService { get; }
        public SolutionService SolutionService { get; }
        public CompilationService CompilationService { get; }

        /// <summary>
        /// Set default telemetry session
        /// </summary>
        public static void SetTelemetrySession(TelemetrySession session)
        {
            s_sessionOpt = session;
        }

        /// <summary>
        /// Default telemetry session
        /// </summary>
        public static TelemetrySession SessionOpt => s_sessionOpt;

        /// <summary>
        /// Check whether current user is microsoft internal or not
        /// </summary>
        public static bool IsUserMicrosoftInternal => SessionOpt?.IsUserMicrosoftInternal ?? false;

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RoslynServices _roslynServices;

            public TestAccessor(RoslynServices roslynServices)
            {
                _roslynServices = roslynServices;
            }

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
