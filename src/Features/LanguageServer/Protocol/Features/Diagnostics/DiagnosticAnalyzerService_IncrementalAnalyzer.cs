// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    public DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
    {
        return _map.GetValue(workspace, _createIncrementalAnalyzer);
    }

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
    {
        // subscribe to active context changed event for new workspace
        workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;

        return new DiagnosticIncrementalAnalyzer(this, CorrelationIdFactory.GetNextId(), workspace, AnalyzerInfoCache);
    }

    private void OnDocumentActiveContextChanged(object? sender, DocumentActiveContextChangedEventArgs e)
        => Reanalyze(e.Solution.Workspace, projectIds: null, documentIds: SpecializedCollections.SingletonEnumerable(e.NewActiveContextDocumentId), highPriority: true);
}
