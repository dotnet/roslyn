// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal interface IDiagnosticsRefresher
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
