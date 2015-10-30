// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public class DiagnosticServiceTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGetDiagnostics1()
        {
            using (var workspace = new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic))
            {
                var set = new ManualResetEvent(false);
                var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", string.Empty);

                var source = new TestDiagnosticUpdateSource(false, null);
                var diagnosticService = new DiagnosticService(AggregateAsynchronousOperationListener.EmptyListeners);
                diagnosticService.Register(source);

                diagnosticService.DiagnosticsUpdated += (s, o) => { set.Set(); };

                var id = Tuple.Create(workspace, document);
                var diagnostic = RaiseDiagnosticEvent(set, source, workspace, document.Project.Id, document.Id, id);

                var data1 = diagnosticService.GetDiagnostics(workspace, null, null, null, false, CancellationToken.None);
                Assert.Equal(diagnostic, data1.Single());

                var data2 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, null, null, false, CancellationToken.None);
                Assert.Equal(diagnostic, data2.Single());

                var data3 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, null, false, CancellationToken.None);
                Assert.Equal(diagnostic, data3.Single());

                var data4 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, id, false, CancellationToken.None);
                Assert.Equal(diagnostic, data4.Single());
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGetDiagnostics2()
        {
            using (var workspace = new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic))
            {
                var set = new ManualResetEvent(false);
                var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", string.Empty);
                var document2 = document.Project.AddDocument("TestDocument2", string.Empty);

                var source = new TestDiagnosticUpdateSource(false, null);
                var diagnosticService = new DiagnosticService(AggregateAsynchronousOperationListener.EmptyListeners);
                diagnosticService.Register(source);

                diagnosticService.DiagnosticsUpdated += (s, o) => { set.Set(); };

                var id = Tuple.Create(workspace, document);
                RaiseDiagnosticEvent(set, source, workspace, document.Project.Id, document.Id, id);

                var id2 = Tuple.Create(workspace, document.Project, document);
                RaiseDiagnosticEvent(set, source, workspace, document.Project.Id, document.Id, id2);

                RaiseDiagnosticEvent(set, source, workspace, document2.Project.Id, document2.Id, Tuple.Create(workspace, document2));

                var id3 = Tuple.Create(workspace, document.Project);
                RaiseDiagnosticEvent(set, source, workspace, document.Project.Id, null, id3);
                RaiseDiagnosticEvent(set, source, workspace, null, null, Tuple.Create(workspace));

                var data1 = diagnosticService.GetDiagnostics(workspace, null, null, null, false, CancellationToken.None);
                Assert.Equal(5, data1.Count());

                var data2 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, null, null, false, CancellationToken.None);
                Assert.Equal(4, data2.Count());

                var data3 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, null, id3, false, CancellationToken.None);
                Assert.Equal(1, data3.Count());

                var data4 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, null, false, CancellationToken.None);
                Assert.Equal(2, data4.Count());

                var data5 = diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, id, false, CancellationToken.None);
                Assert.Equal(1, data5.Count());
            }
        }

        private static DiagnosticData RaiseDiagnosticEvent(ManualResetEvent set, TestDiagnosticUpdateSource source, TestWorkspace workspace, ProjectId project, DocumentId document, object id)
        {
            set.Reset();

            var diagnostic = CreateDiagnosticData(workspace, project, document);

            source.RaiseUpdateEvent(
                DiagnosticsUpdatedArgs.DiagnosticsCreated(id, workspace, workspace.CurrentSolution, project, document, ImmutableArray.Create(diagnostic)));

            set.WaitOne();

            return diagnostic;
        }

        private static DiagnosticData CreateDiagnosticData(TestWorkspace workspace, ProjectId projectId, DocumentId documentId)
        {
            return new DiagnosticData(
                                "test1", "Test", "test1 message", "test1 message format",
                                DiagnosticSeverity.Info, false, 1,
                                workspace, projectId, new DiagnosticDataLocation(documentId,
                                    null, "originalFile1", 10, 10, 20, 20));
        }

        private class TestDiagnosticUpdateSource : IDiagnosticUpdateSource
        {
            private bool _support;
            private ImmutableArray<DiagnosticData> _diagnosticData;

            public TestDiagnosticUpdateSource(bool support, DiagnosticData[] diagnosticData)
            {
                _support = support;
                _diagnosticData = (diagnosticData ?? Array.Empty<DiagnosticData>()).ToImmutableArray();
            }

            public bool SupportGetDiagnostics { get { return _support; } }
            public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

            public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                return _support ? _diagnosticData : ImmutableArray<DiagnosticData>.Empty;
            }

            public void RaiseUpdateEvent(DiagnosticsUpdatedArgs args)
            {
                var handler = DiagnosticsUpdated;
                if (handler != null)
                {
                    handler(this, args);
                }
            }
        }
    }
}
