// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Service exposed to determine what diagnostic mode a workspace is in.  Exposed in this fashion so that individual
    /// workspaces can override that value based on other factors.
    /// </summary>
    internal interface IDiagnosticModeService : IWorkspaceService
    {
        DiagnosticMode GetDiagnosticMode(Option2<DiagnosticMode> diagnosticMode);
    }

    [ExportWorkspaceServiceFactory(typeof(IDiagnosticModeService)), Shared]
    internal class DefaultDiagnosticModeServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultDiagnosticModeServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new DefaultDiagnosticModeService(workspaceServices.Workspace);

        private class DefaultDiagnosticModeService : IDiagnosticModeService
        {
            private readonly Workspace _workspace;

            public DefaultDiagnosticModeService(Workspace workspace)
                => _workspace = workspace;

            public DiagnosticMode GetDiagnosticMode(Option2<DiagnosticMode> diagnosticMode)
                => _workspace.Options.GetOption(diagnosticMode);
        }
    }

    internal static class DiagnosticModeExtensions
    {
        public static DiagnosticMode GetDiagnosticMode(this Workspace workspace, Option2<DiagnosticMode> option)
        {
            var service = workspace.Services.GetRequiredService<IDiagnosticModeService>();
            return service.GetDiagnosticMode(option);
        }

        public static bool IsPullDiagnostics(this Workspace workspace, Option2<DiagnosticMode> option)
        {
            var mode = GetDiagnosticMode(workspace, option);
            return mode == DiagnosticMode.Pull;
        }

        public static bool IsPushDiagnostics(this Workspace workspace, Option2<DiagnosticMode> option)
        {
            var mode = GetDiagnosticMode(workspace, option);
            return mode == DiagnosticMode.Push;
        }
    }
}
