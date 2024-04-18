// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[Export(typeof(IHotReloadDiagnosticManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class HotReloadDiagnosticManager(IDiagnosticsRefresher diagnosticsRefresher) : IHotReloadDiagnosticManager
{
    private ImmutableDictionary<string, ImmutableArray<HotReloadDocumentDiagnostics>> _errors = ImmutableDictionary<string, ImmutableArray<HotReloadDocumentDiagnostics>>.Empty;
    private ImmutableArray<HotReloadDocumentDiagnostics>? _allErrors = null;

    void IHotReloadDiagnosticManager.UpdateErrors(ImmutableArray<HotReloadDocumentDiagnostics> errors, string groupName)
    {
        errors = errors.RemoveAll(d => d.Errors.IsEmpty);

        var oldErrors = _errors;
        if (errors.IsEmpty)
        {
            _errors = _errors.Remove(groupName);
        }
        else
        {
            _errors = _errors.SetItem(groupName, errors);
        }

        if (_errors != oldErrors)
        {
            _allErrors = null;
            diagnosticsRefresher.RequestWorkspaceRefresh();
        }
    }

    void IHotReloadDiagnosticManager.Clear()
    {
        if (!_errors.IsEmpty)
        {
            _errors = ImmutableDictionary<string, ImmutableArray<HotReloadDocumentDiagnostics>>.Empty;
            _allErrors = ImmutableArray<HotReloadDocumentDiagnostics>.Empty;
            diagnosticsRefresher.RequestWorkspaceRefresh();
        }
    }

    ImmutableArray<HotReloadDocumentDiagnostics> IHotReloadDiagnosticManager.Errors
    {
        get
        {
            _allErrors ??= ComputeAllErrors(_errors);
            return _allErrors.Value;
        }
    }

    private static ImmutableArray<HotReloadDocumentDiagnostics> ComputeAllErrors(ImmutableDictionary<string, ImmutableArray<HotReloadDocumentDiagnostics>> errors)
    {
        if (errors.Count == 0)
        {
            return ImmutableArray<HotReloadDocumentDiagnostics>.Empty;
        }

        if (errors.Count == 1)
        {
            return errors.First().Value;
        }

        var allErrors = new Dictionary<DocumentId, List<Diagnostic>>();
        foreach (var group in errors.Values)
        {
            foreach (var documentErrors in group)
            {
                if (!allErrors.TryGetValue(documentErrors.DocumentId, out var list))
                {
                    list = new List<Diagnostic>();
                    allErrors.Add(documentErrors.DocumentId, list);
                }

                list.AddRange(documentErrors.Errors);
            }
        }

        return allErrors
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => new HotReloadDocumentDiagnostics(kvp.Key, kvp.Value.ToImmutableArray())).ToImmutableArray();
    }
}
