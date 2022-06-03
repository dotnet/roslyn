// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    public class DiagnosticServiceTests
    {
        private static DiagnosticService GetDiagnosticService(TestWorkspace workspace)
        {
            var diagnosticService = Assert.IsType<DiagnosticService>(workspace.ExportProvider.GetExportedValue<IDiagnosticService>());

            // These tests were originally written under the assumption that the diagnostic service will not be
            // initialized with listeners. If this check ever fails, the tests that use this method should be reviewed
            // for impact.
            Assert.Empty(diagnosticService.GetTestAccessor().EventListenerTracker.GetTestAccessor().EventListeners);

            return diagnosticService;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task TestGetDiagnostics1()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);
            var mutex = new ManualResetEvent(false);
            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", string.Empty);

            var source = new TestDiagnosticUpdateSource(false, null);
            var diagnosticService = GetDiagnosticService(workspace);
            diagnosticService.Register(source);

            diagnosticService.DiagnosticsUpdated += (s, o) => { mutex.Set(); };

            var id = Tuple.Create(workspace, document);
            var diagnostic = RaiseDiagnosticEvent(mutex, source, workspace, document.Project.Id, document.Id, id);

            var diagnosticMode = DiagnosticMode.Default;

            var data1 = await diagnosticService.GetPushDiagnosticsAsync(workspace, null, null, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(diagnostic, data1.Single());

            var data2 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, null, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(diagnostic, data2.Single());

            var data3 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, document.Id, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(diagnostic, data3.Single());

            var data4 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, document.Id, id, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(diagnostic, data4.Single());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task TestGetDiagnostics2()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);
            var mutex = new ManualResetEvent(false);
            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", string.Empty);
            var document2 = document.Project.AddDocument("TestDocument2", string.Empty);

            var source = new TestDiagnosticUpdateSource(false, null);
            var diagnosticService = GetDiagnosticService(workspace);
            diagnosticService.Register(source);

            diagnosticService.DiagnosticsUpdated += (s, o) => { mutex.Set(); };

            var id = Tuple.Create(workspace, document);
            RaiseDiagnosticEvent(mutex, source, workspace, document.Project.Id, document.Id, id);

            var id2 = Tuple.Create(workspace, document.Project, document);
            RaiseDiagnosticEvent(mutex, source, workspace, document.Project.Id, document.Id, id2);

            RaiseDiagnosticEvent(mutex, source, workspace, document2.Project.Id, document2.Id, Tuple.Create(workspace, document2));

            var id3 = Tuple.Create(workspace, document.Project);
            RaiseDiagnosticEvent(mutex, source, workspace, document.Project.Id, null, id3);
            RaiseDiagnosticEvent(mutex, source, workspace, null, null, Tuple.Create(workspace));

            var diagnosticMode = DiagnosticMode.Default;

            var data1 = await diagnosticService.GetPushDiagnosticsAsync(workspace, null, null, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(5, data1.Count());

            var data2 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, null, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(4, data2.Count());

            var data3 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, null, id3, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(1, data3.Count());

            var data4 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, document.Id, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(2, data4.Count());

            var data5 = await diagnosticService.GetPushDiagnosticsAsync(workspace, document.Project.Id, document.Id, id, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(1, data5.Count());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task TestCleared()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures);
            var mutex = new ManualResetEvent(false);
            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", string.Empty);
            var document2 = document.Project.AddDocument("TestDocument2", string.Empty);

            var diagnosticService = GetDiagnosticService(workspace);

            var source1 = new TestDiagnosticUpdateSource(support: false, diagnosticData: null);
            diagnosticService.Register(source1);

            var source2 = new TestDiagnosticUpdateSource(support: false, diagnosticData: null);
            diagnosticService.Register(source2);

            diagnosticService.DiagnosticsUpdated += MarkSet;

            // add bunch of data to the service for both sources
            RaiseDiagnosticEvent(mutex, source1, workspace, document.Project.Id, document.Id, Tuple.Create(workspace, document));
            RaiseDiagnosticEvent(mutex, source1, workspace, document.Project.Id, document.Id, Tuple.Create(workspace, document.Project, document));
            RaiseDiagnosticEvent(mutex, source1, workspace, document2.Project.Id, document2.Id, Tuple.Create(workspace, document2));

            RaiseDiagnosticEvent(mutex, source2, workspace, document.Project.Id, null, Tuple.Create(workspace, document.Project));
            RaiseDiagnosticEvent(mutex, source2, workspace, null, null, Tuple.Create(workspace));

            var diagnosticMode = DiagnosticMode.Default;

            // confirm data is there.
            var data1 = await diagnosticService.GetPushDiagnosticsAsync(workspace, null, null, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(5, data1.Count());

            diagnosticService.DiagnosticsUpdated -= MarkSet;

            // confirm clear for a source
            mutex.Reset();
            var count = 0;
            diagnosticService.DiagnosticsUpdated += MarkCalled;

            source1.RaiseDiagnosticsClearedEvent();

            mutex.WaitOne();

            // confirm there are 2 data left
            var data2 = await diagnosticService.GetPushDiagnosticsAsync(workspace, null, null, null, includeSuppressedDiagnostics: false, diagnosticMode, CancellationToken.None);
            Assert.Equal(2, data2.Count());

            void MarkCalled(object sender, DiagnosticsUpdatedArgs args)
            {
                // event is serialized. no concurrent call
                if (++count == 3)
                {
                    mutex.Set();
                }
            }

            void MarkSet(object sender, DiagnosticsUpdatedArgs args)
            {
                mutex.Set();
            }
        }

        private static DiagnosticData RaiseDiagnosticEvent(ManualResetEvent set, TestDiagnosticUpdateSource source, TestWorkspace workspace, ProjectId projectId, DocumentId documentId, object id)
        {
            set.Reset();

            var diagnostic = CreateDiagnosticData(projectId, documentId);

            source.RaiseDiagnosticsUpdatedEvent(
                DiagnosticsUpdatedArgs.DiagnosticsCreated(id, workspace, workspace.CurrentSolution, projectId, documentId, ImmutableArray.Create(diagnostic)));

            set.WaitOne();

            return diagnostic;
        }

        private static DiagnosticData CreateDiagnosticData(ProjectId projectId, DocumentId documentId)
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
                properties: ImmutableDictionary<string, string>.Empty,
                projectId,
                location: new DiagnosticDataLocation(documentId, null, "originalFile1", 10, 10, 20, 20));
        }

        private class TestDiagnosticUpdateSource : IDiagnosticUpdateSource
        {
            private readonly bool _support;
            private readonly ImmutableArray<DiagnosticData> _diagnosticData;

            public TestDiagnosticUpdateSource(bool support, DiagnosticData[] diagnosticData)
            {
                _support = support;
                _diagnosticData = (diagnosticData ?? Array.Empty<DiagnosticData>()).ToImmutableArray();
            }

            public bool SupportGetDiagnostics { get { return _support; } }
            public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;
            public event EventHandler DiagnosticsCleared;

            public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
                => new(_support ? _diagnosticData : ImmutableArray<DiagnosticData>.Empty);

            public void RaiseDiagnosticsUpdatedEvent(DiagnosticsUpdatedArgs args)
                => DiagnosticsUpdated?.Invoke(this, args);

            public void RaiseDiagnosticsClearedEvent()
                => DiagnosticsCleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
