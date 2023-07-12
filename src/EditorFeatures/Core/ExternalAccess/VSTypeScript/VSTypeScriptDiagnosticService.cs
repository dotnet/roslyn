// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Export(typeof(IVSTypeScriptDiagnosticService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class VSTypeScriptDiagnosticService(IDiagnosticService service, IGlobalOptionService globalOptions) : IVSTypeScriptDiagnosticService
    {
        private readonly IDiagnosticService _service = service;
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        public async Task<ImmutableArray<VSTypeScriptDiagnosticData>> GetPushDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            // this is the TS entrypoint to get push diagnostics.  Only return diagnostics if we're actually in push-mode.
            var diagnosticMode = _globalOptions.GetDiagnosticMode();
            if (diagnosticMode != DiagnosticMode.SolutionCrawlerPush)
                return ImmutableArray<VSTypeScriptDiagnosticData>.Empty;

            var result = await _service.GetDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(data => new VSTypeScriptDiagnosticData(data));
        }

        public IDisposable RegisterDiagnosticsUpdatedEventHandler(Action<VSTypeScriptDiagnosticsUpdatedArgsWrapper> action)
            => new EventHandlerWrapper(_service, action);

        private sealed class EventHandlerWrapper : IDisposable
        {
            private readonly IDiagnosticService _service;
            private readonly EventHandler<DiagnosticsUpdatedArgs> _handler;

            internal EventHandlerWrapper(IDiagnosticService service, Action<VSTypeScriptDiagnosticsUpdatedArgsWrapper> action)
            {
                _service = service;
                _handler = (sender, args) => action(new VSTypeScriptDiagnosticsUpdatedArgsWrapper(args));
                _service.DiagnosticsUpdated += _handler;
            }

            public void Dispose()
            {
                _service.DiagnosticsUpdated -= _handler;
            }
        }
    }
}
