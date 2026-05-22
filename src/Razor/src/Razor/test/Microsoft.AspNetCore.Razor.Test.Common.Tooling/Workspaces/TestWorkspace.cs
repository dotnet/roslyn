// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.AspNetCore.Razor.Test.Common.Workspaces;

public static class TestWorkspace
{
    private static readonly object s_workspaceLock = new();

    public static Workspace Create(Action<AdhocWorkspace>? configure = null)
        => Create(services: null, configure: configure);

    public static AdhocWorkspace CreateWithDiagnosticAnalyzers(ExportProvider exportProvider)
    {
        var hostServices = MefHostServices.Create(exportProvider.AsCompositionContext());

        var workspace = Create(hostServices);

        AddAnalyzersToWorkspace(workspace);

        return workspace;
    }

    public static AdhocWorkspace Create(HostServices? services, Action<AdhocWorkspace>? configure = null)
    {
        lock (s_workspaceLock)
        {
            var workspace = services is null
                ? new AdhocWorkspace()
                : new AdhocWorkspace(services);

            configure?.Invoke(workspace);

            return workspace;
        }
    }

    private static void AddAnalyzersToWorkspace(Workspace workspace)
    {
        var analyzerLoader = RazorTestAnalyzerLoader.CreateAnalyzerAssemblyLoader();

        var analyzerPaths = new DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.dll")
            .Where(f => f.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) && !f.Name.Contains("LanguageServer") && !f.Name.Contains("Test.Utilities"))
            .Select(f => f.FullName)
            .ToImmutableArray();
        var references = new List<AnalyzerFileReference>();
        foreach (var analyzerPath in analyzerPaths)
        {
            if (File.Exists(analyzerPath))
            {
                references.Add(new AnalyzerFileReference(analyzerPath, analyzerLoader));
            }
        }

        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(references));
    }
}
