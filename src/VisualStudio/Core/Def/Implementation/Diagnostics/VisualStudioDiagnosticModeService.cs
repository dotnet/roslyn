// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [ExportWorkspaceService(typeof(IDiagnosticModeService), ServiceLayer.Host), Shared]
    internal class VisualStudioDiagnosticModeServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly Shell.IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDiagnosticModeServiceFactory(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = (Shell.IAsyncServiceProvider)serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new VisualStudioDiagnosticModeService(_threadingContext, _serviceProvider, workspaceServices.Workspace);

        private class VisualStudioDiagnosticModeService : IDiagnosticModeService
        {
            private readonly IThreadingContext _threadingContext;
            private readonly Shell.IAsyncServiceProvider _serviceProvider;
            private readonly Workspace _workspace;
            private readonly Dictionary<Option2<DiagnosticMode>, AsyncLazy<DiagnosticMode>> _optionToMode = new();

            public VisualStudioDiagnosticModeService(
                IThreadingContext threadingContext,
                Shell.IAsyncServiceProvider serviceProvider,
                Workspace workspace)
            {
                _threadingContext = threadingContext;
                _serviceProvider = serviceProvider;
                _workspace = workspace;
            }

            public Task<DiagnosticMode> GetDiagnosticModeAsync(Option2<DiagnosticMode> option, CancellationToken cancellationToken)
            {
                var lazy = GetLazy(option);
                return lazy.GetValueAsync(cancellationToken);
            }

            private AsyncLazy<DiagnosticMode> GetLazy(Option2<DiagnosticMode> option)
            {
                lock (_optionToMode)
                {
                    if (!_optionToMode.TryGetValue(option, out var lazy))
                    {
                        lazy = new AsyncLazy<DiagnosticMode>(c => ComputeDiagnosticModeAsync(option, c), cacheResult: true);
                        _optionToMode.Add(option, lazy);
                    }

                    return lazy;
                }
            }

            private async Task<DiagnosticMode> ComputeDiagnosticModeAsync(Option2<DiagnosticMode> option, CancellationToken cancellationToken)
            {
                var inCodeSpacesServer = await IsInCodeSpacesServerAsync(cancellationToken).ConfigureAwait(false);

                // If we're in the code-spaces server, we only support pull diagnostics.  This is because the only way
                // for diagnostics to make it through from the  server to the client is through the codespaces LSP
                // channel, which is only pull based.
                if (inCodeSpacesServer)
                    return DiagnosticMode.Pull;

                // Otherwise, defer to the workspace+option to determine what mode we're in.
                return _workspace.Options.GetOption(option);
            }

            private async Task<bool> IsInCodeSpacesServerAsync(CancellationToken cancellationToken)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var shell = await _serviceProvider.GetServiceAsync<SVsShell, IVsShell>().ConfigureAwait(true);
                ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID11.VSSPROPID_ShellMode, out var result));
                var shellMode = (__VSShellMode)result;
                return shellMode == __VSShellMode.VSSM_Server;
            }
        }
    }
}
