// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Preview
{
    public class PreviewWorkspaceTests
    {
        [Fact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewCreationDefault()
        {
            using (var previewWorkspace = new PreviewWorkspace())
            {
                Assert.NotNull(previewWorkspace.CurrentSolution);
            }
        }

        [Fact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewCreationWithExplicitHostServices()
        {
            var assembly = typeof(ISolutionCrawlerRegistrationService).Assembly;
            using (var previewWorkspace = new PreviewWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(assembly))))
            {
                Assert.NotNull(previewWorkspace.CurrentSolution);
            }
        }

        [Fact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewCreationWithSolution()
        {
            using (var custom = new AdhocWorkspace())
            using (var previewWorkspace = new PreviewWorkspace(custom.CurrentSolution))
            {
                Assert.NotNull(previewWorkspace.CurrentSolution);
            }
        }

        [Fact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewAddRemoveProject()
        {
            using (var previewWorkspace = new PreviewWorkspace())
            {
                var solution = previewWorkspace.CurrentSolution;
                var project = solution.AddProject("project", "project.dll", LanguageNames.CSharp);
                Assert.True(previewWorkspace.TryApplyChanges(project.Solution));

                var newSolution = previewWorkspace.CurrentSolution.RemoveProject(project.Id);
                Assert.True(previewWorkspace.TryApplyChanges(newSolution));

                Assert.Equal(0, previewWorkspace.CurrentSolution.ProjectIds.Count);
            }
        }

        [Fact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewProjectChanges()
        {
            using (var previewWorkspace = new PreviewWorkspace())
            {
                var solution = previewWorkspace.CurrentSolution;
                var project = solution.AddProject("project", "project.dll", LanguageNames.CSharp);
                Assert.True(previewWorkspace.TryApplyChanges(project.Solution));

                var addedSolution = previewWorkspace.CurrentSolution.Projects.First()
                                                    .AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
                                                    .AddDocument("document", "").Project.Solution;
                Assert.True(previewWorkspace.TryApplyChanges(addedSolution));
                Assert.Equal(1, previewWorkspace.CurrentSolution.Projects.First().MetadataReferences.Count);
                Assert.Equal(1, previewWorkspace.CurrentSolution.Projects.First().DocumentIds.Count);

                var text = "class C {}";
                var changedSolution = previewWorkspace.CurrentSolution.Projects.First().Documents.First().WithText(SourceText.From(text)).Project.Solution;
                Assert.True(previewWorkspace.TryApplyChanges(changedSolution));
                Assert.Equal(previewWorkspace.CurrentSolution.Projects.First().Documents.First().GetTextAsync().Result.ToString(), text);

                var removedSolution = previewWorkspace.CurrentSolution.Projects.First()
                                                    .RemoveMetadataReference(previewWorkspace.CurrentSolution.Projects.First().MetadataReferences[0])
                                                    .RemoveDocument(previewWorkspace.CurrentSolution.Projects.First().DocumentIds[0]).Solution;

                Assert.True(previewWorkspace.TryApplyChanges(removedSolution));
                Assert.Equal(0, previewWorkspace.CurrentSolution.Projects.First().MetadataReferences.Count);
                Assert.Equal(0, previewWorkspace.CurrentSolution.Projects.First().DocumentIds.Count);
            }
        }

        [WorkItem(923121)]
        [WpfFact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewOpenCloseFile()
        {
            using (var previewWorkspace = new PreviewWorkspace())
            {
                var solution = previewWorkspace.CurrentSolution;
                var project = solution.AddProject("project", "project.dll", LanguageNames.CSharp);
                var document = project.AddDocument("document", "");

                Assert.True(previewWorkspace.TryApplyChanges(document.Project.Solution));

                previewWorkspace.OpenDocument(document.Id);
                Assert.Equal(1, previewWorkspace.GetOpenDocumentIds().Count());
                Assert.True(previewWorkspace.IsDocumentOpen(document.Id));

                previewWorkspace.CloseDocument(document.Id);
                Assert.Equal(0, previewWorkspace.GetOpenDocumentIds().Count());
                Assert.False(previewWorkspace.IsDocumentOpen(document.Id));
            }
        }

        [Fact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewServices()
        {
            using (var previewWorkspace = new PreviewWorkspace(MefV1HostServices.Create(TestExportProvider.ExportProviderWithCSharpAndVisualBasic.AsExportProvider())))
            {
                var service = previewWorkspace.Services.GetService<ISolutionCrawlerRegistrationService>();
                Assert.True(service is PreviewSolutionCrawlerRegistrationService);

                var persistentService = previewWorkspace.Services.GetService<IPersistentStorageService>();
                Assert.NotNull(persistentService);

                var storage = persistentService.GetStorage(previewWorkspace.CurrentSolution);
                Assert.True(storage is NoOpPersistentStorage);
            }
        }

        [WorkItem(923196)]
        [WpfFact, Trait(Traits.Editor, Traits.Editors.Preview)]
        public void TestPreviewDiagnostic()
        {
            var diagnosticService = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue<IDiagnosticAnalyzerService>() as IDiagnosticUpdateSource;

            var taskSource = new TaskCompletionSource<DiagnosticsUpdatedArgs>();
            diagnosticService.DiagnosticsUpdated += (s, a) => taskSource.TrySetResult(a);

            using (var previewWorkspace = new PreviewWorkspace(MefV1HostServices.Create(TestExportProvider.ExportProviderWithCSharpAndVisualBasic.AsExportProvider())))
            {
                var solution = previewWorkspace.CurrentSolution
                                               .AddProject("project", "project.dll", LanguageNames.CSharp)
                                               .AddDocument("document", "class { }")
                                               .Project
                                               .Solution;

                Assert.True(previewWorkspace.TryApplyChanges(solution));

                previewWorkspace.OpenDocument(previewWorkspace.CurrentSolution.Projects.First().DocumentIds[0]);
                previewWorkspace.EnableDiagnostic();

                // wait 20 seconds
                taskSource.Task.Wait(20000);
                if (!taskSource.Task.IsCompleted)
                {
                    // something is wrong
                    FatalError.Report(new System.Exception("not finished after 20 seconds"));
                }

                var args = taskSource.Task.Result;
                Assert.True(args.Diagnostics.Length > 0);
            }
        }

        [WpfFact]
        public async Task TestPreviewDiagnosticTagger()
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync("class { }"))
            using (var previewWorkspace = new PreviewWorkspace(workspace.CurrentSolution))
            {
                //// preview workspace and owner of the solution now share solution and its underlying text buffer
                var hostDocument = workspace.Projects.First().Documents.First();

                //// enable preview diagnostics
                previewWorkspace.EnableDiagnostic();

                var diagnosticsAndErrorsSpans = await SquiggleUtilities.GetDiagnosticsAndErrorSpans(workspace);
                const string AnalzyerCount = "Analyzer Count: ";
                Assert.Equal(AnalzyerCount + 1, AnalzyerCount + diagnosticsAndErrorsSpans.Item1.Length);

                const string SquigglesCount = "Squiggles Count: ";
                Assert.Equal(SquigglesCount + 1, SquigglesCount + diagnosticsAndErrorsSpans.Item2.Count);
            }
        }

        [WpfFact]
        public async Task TestPreviewDiagnosticTaggerInPreviewPane()
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync("class { }"))
            {
                // set up listener to wait until diagnostic finish running
                var diagnosticService = workspace.ExportProvider.GetExportedValue<IDiagnosticService>() as DiagnosticService;

                // no easy way to setup waiter. kind of hacky way to setup waiter
                var source = new CancellationTokenSource();
                var taskSource = new TaskCompletionSource<DiagnosticsUpdatedArgs>();
                diagnosticService.DiagnosticsUpdated += (s, a) =>
                {
                    source.Cancel();

                    source = new CancellationTokenSource();
                    var cancellationToken = source.Token;
                    Task.Delay(2000, cancellationToken).ContinueWith(t => taskSource.TrySetResult(a), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
                };

                var hostDocument = workspace.Projects.First().Documents.First();

                // make a change to remove squiggle
                var oldDocument = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var oldText = oldDocument.GetTextAsync().Result;

                var newDocument = oldDocument.WithText(oldText.WithChanges(new TextChange(new TextSpan(0, oldText.Length), "class C { }")));

                // create a diff view
                WpfTestCase.RequireWpfFact($"{nameof(TestPreviewDiagnosticTaggerInPreviewPane)} creates a {nameof(IWpfDifferenceViewer)}");

                var previewFactoryService = workspace.ExportProvider.GetExportedValue<IPreviewFactoryService>();
                var diffView = (IWpfDifferenceViewer)(await previewFactoryService.CreateChangedDocumentPreviewViewAsync(oldDocument, newDocument, CancellationToken.None));

                var foregroundService = workspace.GetService<IForegroundNotificationService>();
                var optionsService = workspace.Services.GetService<IOptionService>();

                var waiter = new ErrorSquiggleWaiter();
                var listeners = AsynchronousOperationListener.CreateListeners(FeatureAttribute.ErrorSquiggles, waiter);

                // set up tagger for both buffers
                var leftBuffer = diffView.LeftView.BufferGraph.GetTextBuffers(t => t.ContentType.IsOfType(ContentTypeNames.CSharpContentType)).First();
                var leftProvider = new DiagnosticsSquiggleTaggerProvider(optionsService, diagnosticService, foregroundService, listeners);
                var leftTagger = leftProvider.CreateTagger<IErrorTag>(leftBuffer);
                using (var leftDisposable = leftTagger as IDisposable)
                {
                    var rightBuffer = diffView.RightView.BufferGraph.GetTextBuffers(t => t.ContentType.IsOfType(ContentTypeNames.CSharpContentType)).First();
                    var rightProvider = new DiagnosticsSquiggleTaggerProvider(optionsService, diagnosticService, foregroundService, listeners);
                    var rightTagger = rightProvider.CreateTagger<IErrorTag>(rightBuffer);
                    using (var rightDisposable = rightTagger as IDisposable)
                    {
                        // wait up to 20 seconds for diagnostics
                        taskSource.Task.Wait(20000);
                        if (!taskSource.Task.IsCompleted)
                        {
                            // something is wrong
                            FatalError.Report(new System.Exception("not finished after 20 seconds"));
                        }

                        // wait taggers
                        await waiter.CreateWaitTask();

                        // check left buffer
                        var leftSnapshot = leftBuffer.CurrentSnapshot;
                        var leftSpans = leftTagger.GetTags(leftSnapshot.GetSnapshotSpanCollection()).ToList();
                        Assert.Equal(1, leftSpans.Count);

                        // check right buffer
                        var rightSnapshot = rightBuffer.CurrentSnapshot;
                        var rightSpans = rightTagger.GetTags(rightSnapshot.GetSnapshotSpanCollection()).ToList();
                        Assert.Equal(0, rightSpans.Count);
                    }
                }
            }
        }

        private class ErrorSquiggleWaiter : AsynchronousOperationListener { }
    }
}
