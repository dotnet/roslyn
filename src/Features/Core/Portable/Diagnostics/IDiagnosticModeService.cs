// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
        Task<DiagnosticMode> GetDiagnosticModeAsync(Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(IDiagnosticModeService)), Shared]
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

            public Task<DiagnosticMode> GetDiagnosticModeAsync(Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
                => Task.FromResult(_workspace.Options.GetOption(diagnosticMode));
        }
    }

    internal static class DiagnosticModeExtensions
    {
        public static Task<DiagnosticMode> GetDiagnosticModeAsync(this Workspace workspace, Option2<DiagnosticMode> option, CancellationToken cancellationToken)
        {
            var service = workspace.Services.GetRequiredService<IDiagnosticModeService>();
            return service.GetDiagnosticModeAsync(option, cancellationToken);
        }

        public static async Task<bool> IsPullDiagnosticsAsync(this Workspace workspace, Option2<DiagnosticMode> option, CancellationToken cancellationToken)
        {
            var mode = await GetDiagnosticModeAsync(workspace, option, cancellationToken).ConfigureAwait(false);
            return mode == DiagnosticMode.Pull;
        }

        public static async Task<bool> IsPushDiagnosticsAsync(this Workspace workspace, Option2<DiagnosticMode> option, CancellationToken cancellationToken)
        {
            var mode = await GetDiagnosticModeAsync(workspace, option, cancellationToken).ConfigureAwait(false);
            return mode == DiagnosticMode.Push;
        }
    }
}
