// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract partial class AbstractDiagnosticsRefreshQueue
{
    [Shared]
    [Export(typeof(Refresher))]
    [Export(typeof(IDiagnosticsRefresher))]
    internal sealed class Refresher : IDiagnosticsRefresher
    {
        /// <summary>
        /// Incremented every time a refresh is requested.
        /// </summary>
        private int _globalStateVersion;

        public event Action? WorkspaceRefreshRequested;
        public event Action? CodeAnalysisRefreshRequested;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Refresher()
        {
        }

        public void RequestWorkspaceRefresh()
        {
            // bump version before sending the request to the client:
            Interlocked.Increment(ref _globalStateVersion);

            WorkspaceRefreshRequested?.Invoke();
        }

        public void RequestCodeAnalysisRefresh()
        {
            // bump version before sending the request to the client:
            Interlocked.Increment(ref _globalStateVersion);

            CodeAnalysisRefreshRequested?.Invoke();
        }

        public int GlobalStateVersion
            => _globalStateVersion;
    }
}
