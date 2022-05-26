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
    internal sealed class VSTypeScriptDiagnosticService : IVSTypeScriptDiagnosticService
    {
        private readonly IDiagnosticService _service;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptDiagnosticService(IDiagnosticService service, IGlobalOptionService globalOptions)
        {
            _service = service;
            _globalOptions = globalOptions;
        }

        public async Task<ImmutableArray<VSTypeScriptDiagnosticData>> GetPushDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            var result = await _service.GetPushDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, _globalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode), cancellationToken).ConfigureAwait(false);
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
