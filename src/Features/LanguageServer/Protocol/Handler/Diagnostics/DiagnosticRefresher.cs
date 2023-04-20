// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal interface IDiagnosticRefresher
{
    /// <summary>
    /// Requests workspace diagnostics refresh.
    /// </summary>
    void RequestWorkspaceRefresh();

    /// <summary>
    /// Requests document diangostics refresh.
    /// </summary>
    void RequestDocumentRefresh(Document document);

}

[Shared]
[Export(typeof(DiagnosticRefresher))]
[Export(typeof(IDiagnosticRefresher))]
internal sealed class DiagnosticRefresher : IDiagnosticRefresher
{
    public event Action? WorkspaceRefreshRequested;
    public event Action<Document>? DocumentRefreshRequested;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DiagnosticRefresher()
    {
    }

    public void RequestWorkspaceRefresh()
        => WorkspaceRefreshRequested?.Invoke();

    public void RequestDocumentRefresh(Document document)
        => DocumentRefreshRequested?.Invoke(document);

}
