// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class EditAndContinueDiagnosticUpdateSourceTests
    {
        [Fact]
        public void ReportDiagnostics()
        {
            var source = new EditAndContinueDiagnosticUpdateSource();

            var updates = new List<string>();

            source.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs e)
                => updates.Add($"{e.Kind} p={e.ProjectId} d={e.DocumentId}: {string.Join(",", e.Diagnostics.Select(d => d.Id.ToString()))}");

            var srcC1 = "class C1 {}";
            var srcC2 = "class C2 {}";
            var srcD1 = "class D1 {}";
            var srcD2 = "class D2 {}";
            var docC1 = new TestHostDocument(srcC1, displayName: "DocC1");
            var docC2 = new TestHostDocument(srcC2, displayName: "DocC2");
            var docD1 = new TestHostDocument(srcD1, displayName: "DocD1");
            var docD2 = new TestHostDocument(srcD2, displayName: "DocD2");

            var workspace = new TestWorkspace();
            var projC = new TestHostProject(workspace, "ProjC");
            projC.AddDocument(docC1);
            projC.AddDocument(docC2);
            var projD = new TestHostProject(workspace, "ProjD");
            projD.AddDocument(docD1);
            projD.AddDocument(docD2);

            workspace.AddTestProject(projC);
            workspace.AddTestProject(projD);

            var treeC1 = workspace.CurrentSolution.GetDocument(docC1.Id).GetSyntaxTreeAsync().Result;
            var treeC2 = workspace.CurrentSolution.GetDocument(docC2.Id).GetSyntaxTreeAsync().Result;
            var treeD1 = workspace.CurrentSolution.GetDocument(docD1.Id).GetSyntaxTreeAsync().Result;
            var treeD2 = workspace.CurrentSolution.GetDocument(docD2.Id).GetSyntaxTreeAsync().Result;

            var diagnostics = new[]
            {
                Diagnostic.Create(new DiagnosticDescriptor("TST0001", "title1", "message1", "category", DiagnosticSeverity.Error, true), Location.Create(treeC1, new TextSpan(1, 1))),
                Diagnostic.Create(new DiagnosticDescriptor("TST0002", "title2", "message2", "category", DiagnosticSeverity.Error, true), Location.Create(treeC1, new TextSpan(1, 2))),
                Diagnostic.Create(new DiagnosticDescriptor("TST0003", "title3", "message3", "category", DiagnosticSeverity.Error, true), Location.Create(treeD1, new TextSpan(1, 2))),
                Diagnostic.Create(new DiagnosticDescriptor("TST0004", "title4", "message4", "category", DiagnosticSeverity.Error, true), Location.Create(treeD2, new TextSpan(1, 2))),
                Diagnostic.Create(new DiagnosticDescriptor("TST0005", "title5", "message5", "category", DiagnosticSeverity.Error, true), Location.None),
            };

            updates.Clear();
            source.ReportDiagnostics(workspace.CurrentSolution, projC.Id, diagnostics);
            AssertEx.Equal(new[]
            {
                $"DiagnosticsCreated p={projC.Id} d={docC1.Id}: TST0001,TST0002",
                $"DiagnosticsCreated p={projC.Id} d={docD1.Id}: TST0003",
                $"DiagnosticsCreated p={projC.Id} d={docD2.Id}: TST0004",
                $"DiagnosticsCreated p={projC.Id} d=: TST0005"
            }, updates);

            updates.Clear();
            source.ReportDiagnostics(workspace.CurrentSolution, projD.Id, diagnostics);
            AssertEx.Equal(new[]
            {
                $"DiagnosticsCreated p={projD.Id} d={docC1.Id}: TST0001,TST0002",
                $"DiagnosticsCreated p={projD.Id} d={docD1.Id}: TST0003",
                $"DiagnosticsCreated p={projD.Id} d={docD2.Id}: TST0004",
                $"DiagnosticsCreated p={projD.Id} d=: TST0005"
            }, updates);

            updates.Clear();
            source.ReportDiagnostics(workspace.CurrentSolution, null, diagnostics);
            AssertEx.Equal(new[]
            {
                $"DiagnosticsCreated p= d={docC1.Id}: TST0001,TST0002",
                $"DiagnosticsCreated p= d={docD1.Id}: TST0003",
                $"DiagnosticsCreated p= d={docD2.Id}: TST0004",
                $"DiagnosticsCreated p= d=: TST0005"
            }, updates);
        }
    }
}
