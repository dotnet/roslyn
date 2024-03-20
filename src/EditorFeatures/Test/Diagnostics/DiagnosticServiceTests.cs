// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Diagnostics)]
    public class DiagnosticServiceTests
    {
        private static DiagnosticService GetDiagnosticService(TestWorkspace workspace)
        {
            var diagnosticService = Assert.IsType<DiagnosticService>(workspace.ExportProvider.GetExportedValue<IDiagnosticService>());

            return diagnosticService;
        }

        [Fact]
        public void TestCleared()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);
            var mutex = new ManualResetEvent(false);
            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", string.Empty);
            var document2 = document.Project.AddDocument("TestDocument2", string.Empty);

            var diagnosticService = GetDiagnosticService(workspace);

            var source1 = new TestDiagnosticUpdateSource();
            diagnosticService.Register(source1);

            var source2 = new TestDiagnosticUpdateSource();
            diagnosticService.Register(source2);

            diagnosticService.DiagnosticsUpdated += MarkSet;

            // add bunch of data to the service for both sources
            RaiseDiagnosticEvent(mutex, source1, workspace, document.Project.Id, document.Id, Tuple.Create(workspace, document));
            RaiseDiagnosticEvent(mutex, source1, workspace, document.Project.Id, document.Id, Tuple.Create(workspace, document.Project, document));
            RaiseDiagnosticEvent(mutex, source1, workspace, document2.Project.Id, document2.Id, Tuple.Create(workspace, document2));

            RaiseDiagnosticEvent(mutex, source2, workspace, document.Project.Id, null, Tuple.Create(workspace, document.Project));
            RaiseDiagnosticEvent(mutex, source2, workspace, null, null, Tuple.Create(workspace));

            diagnosticService.DiagnosticsUpdated -= MarkSet;

            // confirm clear for a source
            mutex.Reset();
            var count = 0;
            diagnosticService.DiagnosticsUpdated += MarkCalled;

            source1.RaiseDiagnosticsClearedEvent();

            mutex.WaitOne();
            return;

            void MarkCalled(object sender, ImmutableArray<DiagnosticsUpdatedArgs> args)
            {
                foreach (var _ in args)
                {
                    // event is serialized. no concurrent call
                    if (++count == 3)
                    {
                        mutex.Set();
                    }
                }
            }

            void MarkSet(object sender, ImmutableArray<DiagnosticsUpdatedArgs> args)
            {
                foreach (var _ in args)
                    mutex.Set();
            }
        }

        private static DiagnosticData RaiseDiagnosticEvent(ManualResetEvent set, TestDiagnosticUpdateSource source, TestWorkspace workspace, ProjectId? projectId, DocumentId? documentId, object id)
        {
            set.Reset();

            var diagnostic = CreateDiagnosticData(projectId, documentId);

            source.RaiseDiagnosticsUpdatedEvent(
                ImmutableArray.Create(DiagnosticsUpdatedArgs.DiagnosticsCreated(id, workspace, workspace.CurrentSolution, projectId, documentId, ImmutableArray.Create(diagnostic))));

            set.WaitOne();

            return diagnostic;
        }

        private static DiagnosticData CreateDiagnosticData(ProjectId? projectId, DocumentId? documentId)
        {
            return new DiagnosticData(
                id: "test1",
                category: "Test",
                message: "test1 message",
                severity: DiagnosticSeverity.Info,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: false,
                warningLevel: 1,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string?>.Empty,
                projectId,
                location: new DiagnosticDataLocation(new("originalFile1", new(10, 10), new(20, 20)), documentId));
        }

        private class TestDiagnosticUpdateSource : IDiagnosticUpdateSource
        {
            public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>? DiagnosticsUpdated;
            public event EventHandler? DiagnosticsCleared;

            public void RaiseDiagnosticsUpdatedEvent(ImmutableArray<DiagnosticsUpdatedArgs> args)
                => DiagnosticsUpdated?.Invoke(this, args);

            public void RaiseDiagnosticsClearedEvent()
                => DiagnosticsCleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
