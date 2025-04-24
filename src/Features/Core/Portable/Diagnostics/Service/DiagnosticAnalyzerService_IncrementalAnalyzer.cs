// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
    {
        return _map.GetValue(workspace, _createIncrementalAnalyzer);
    }

    private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
    {
        // subscribe to active context changed event for new workspace
        workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;

        return new DiagnosticIncrementalAnalyzer(this, AnalyzerInfoCache, this.GlobalOptions);
    }

    private void OnDocumentActiveContextChanged(object? sender, DocumentActiveContextChangedEventArgs e)
        => RequestDiagnosticRefresh();
}
