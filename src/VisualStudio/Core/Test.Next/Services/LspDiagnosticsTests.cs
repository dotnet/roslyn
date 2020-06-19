﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Threading;
using Moq;
using Nerdbank.Streams;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    [UseExportProvider]
    public class LspDiagnosticsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task AddDiagnosticTestAsync()
        {
            using var workspace = CreateTestWorkspace("", out _);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            // Create a mock that returns a diagnostic for the document.
            SetupMockWithDiagnostics(diagnosticsMock, document.Id, await CreateMockDiagnosticDataAsync(document, "id").ConfigureAwait(false));

            // Publish one document change diagnostic notification ->
            // 1.  doc1 with id.
            //
            // We expect one publish diagnostic notification ->
            // 1.  from doc1 with id.
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 1, document).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Equal(new Uri(document.FilePath), result.Uri);
            Assert.Equal("id", result.Diagnostics.Single().Code);
        }

        [Fact]
        public async Task AddDiagnosticWithMappedFilesTestAsync()
        {
            using var workspace = CreateTestWorkspace("", out _);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            // Create two mapped diagnostics for the document.
            SetupMockWithDiagnostics(diagnosticsMock, document.Id,
                await CreateMockDiagnosticDatasWithMappedLocationAsync(document, ("id1", document.FilePath + "m1"), ("id2", document.FilePath + "m2")).ConfigureAwait(false));

            // Publish one document change diagnostic notification ->
            // 1.  doc1 with id1 = mapped file m1 and id2 = mapped file m2.
            //
            // We expect two publish diagnostic notifications ->
            // 1.  from m1 with id1 (from 1 above).
            // 2.  from m2 with id2 (from 1 above).
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, expectedNumberOfCallbacks: 2, document).ConfigureAwait(false);

            Assert.Equal(2, results.Count);
            Assert.Equal(new Uri(document.FilePath + "m1"), results[0].Uri);
            Assert.Equal("id1", results[0].Diagnostics.Single().Code);

            Assert.Equal(new Uri(document.FilePath + "m2"), results[1].Uri);
            Assert.Equal("id2", results[1].Diagnostics.Single().Code);
        }

        [Fact]
        public async Task AddDiagnosticWithMappedFileToManyDocumentsTestAsync()
        {
            using var workspace = CreateTestWorkspace(new string[] { "", "" }, out _);
            var documents = workspace.CurrentSolution.Projects.First().Documents.ToImmutableArray();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            // Create diagnostic for the first document that has a mapped location.
            var mappedFilePath = documents[0].FilePath + "m1";
            var documentOneDiagnostic = await CreateMockDiagnosticDatasWithMappedLocationAsync(documents[0], ("doc1Diagnostic", mappedFilePath)).ConfigureAwait(false);
            // Create diagnostic for the second document that maps to the same location as the first document diagnostic.
            var documentTwoDiagnostic = await CreateMockDiagnosticDatasWithMappedLocationAsync(documents[1], ("doc2Diagnostic", mappedFilePath)).ConfigureAwait(false);

            SetupMockWithDiagnostics(diagnosticsMock, documents[0].Id, documentOneDiagnostic);
            SetupMockWithDiagnostics(diagnosticsMock, documents[1].Id, documentTwoDiagnostic);

            // Publish two document change diagnostic notifications ->
            // 1.  doc1 with doc1Diagnostic = mapped file m1.
            // 2.  doc2 with doc2Diagnostic = mapped file m1.
            //
            // We expect two publish diagnostic notifications ->
            // 1.  from m1 with doc1Diagnostic (from 1 above).
            // 2.  from m1 with doc1Diagnostic and doc2Diagnostic (from 2 above adding doc2Diagnostic to m1).
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 2, documents[0], documents[1]).ConfigureAwait(false);

            Assert.Equal(2, results.Count);
            var expectedUri = new Uri(mappedFilePath);
            Assert.Equal(expectedUri, results[0].Uri);
            Assert.Equal("doc1Diagnostic", results[0].Diagnostics.Single().Code);

            Assert.Equal(expectedUri, results[1].Uri);
            Assert.Equal(2, results[1].Diagnostics.Length);
            Assert.Contains(results[1].Diagnostics, d => d.Code == "doc1Diagnostic");
            Assert.Contains(results[1].Diagnostics, d => d.Code == "doc2Diagnostic");
        }

        [Fact]
        public async Task RemoveDiagnosticTestAsync()
        {
            using var workspace = CreateTestWorkspace("", out _);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            // Setup the mock so the first call for a document returns a diagnostic, but the second returns empty.
            SetupMockDiagnosticSequence(diagnosticsMock, document.Id,
                await CreateMockDiagnosticDataAsync(document, "id").ConfigureAwait(false),
                ImmutableArray<DiagnosticData>.Empty);

            // Publish two document change diagnostic notifications ->
            // 1.  doc1 with id.
            // 2.  doc1 with empty.
            //
            // We expect two publish diagnostic notifications ->
            // 1.  from doc1 with id.
            // 2.  from doc1 with empty (from 2 above clearing out diagnostics from doc1).
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 2, document, document).ConfigureAwait(false);

            Assert.Equal(2, results.Count);
            Assert.Equal(new Uri(document.FilePath), results[0].Uri);
            Assert.Equal("id", results[0].Diagnostics.Single().Code);

            Assert.Equal(new Uri(document.FilePath), results[1].Uri);
            Assert.True(results[1].Diagnostics.IsEmpty());

            Assert.Empty(testAccessor.GetDocumentIdsInPublishedUris());
            Assert.Empty(testAccessor.GetFileUrisInPublishDiagnostics());
        }

        [Fact]
        public async Task RemoveDiagnosticForMappedFilesTestAsync()
        {
            using var workspace = CreateTestWorkspace("", out _);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);

            var mappedFilePathM1 = document.FilePath + "m1";
            var mappedFilePathM2 = document.FilePath + "m2";
            // Create two mapped diagnostics for the document on first call.
            // On the second call, return only the second mapped diagnostic for the document.
            SetupMockDiagnosticSequence(diagnosticsMock, document.Id,
                await CreateMockDiagnosticDatasWithMappedLocationAsync(document, ("id1", mappedFilePathM1), ("id2", mappedFilePathM2)).ConfigureAwait(false),
                await CreateMockDiagnosticDatasWithMappedLocationAsync(document, ("id2", mappedFilePathM2)).ConfigureAwait(false));

            // Publish three document change diagnostic notifications ->
            // 1.  doc1 with id1 = mapped file m1 and id2 = mapped file m2.
            // 2.  doc1 with just id2 = mapped file m2.
            //
            // We expect four publish diagnostic notifications ->
            // 1.  from m1 with id1 (from 1 above).
            // 2.  from m2 with id2 (from 1 above).
            // 3.  from m1 with empty (from 2 above clearing out diagnostics for m1).
            // 4.  from m2 with id2 (from 2 above clearing out diagnostics for m1).
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 4, document, document).ConfigureAwait(false);

            var mappedFileURIM1 = new Uri(mappedFilePathM1);
            var mappedFileURIM2 = new Uri(mappedFilePathM2);

            Assert.Equal(4, results.Count);

            // First document update.
            Assert.Equal(mappedFileURIM1, results[0].Uri);
            Assert.Equal("id1", results[0].Diagnostics.Single().Code);

            Assert.Equal(mappedFileURIM2, results[1].Uri);
            Assert.Equal("id2", results[1].Diagnostics.Single().Code);

            // Second document update.
            Assert.Equal(mappedFileURIM1, results[2].Uri);
            Assert.True(results[2].Diagnostics.IsEmpty());

            Assert.Equal(mappedFileURIM2, results[3].Uri);
            Assert.Equal("id2", results[3].Diagnostics.Single().Code);

            Assert.Single(testAccessor.GetFileUrisForDocument(document.Id), mappedFileURIM2);
            Assert.Equal("id2", testAccessor.GetDiagnosticsForUriAndDocument(document.Id, mappedFileURIM2).Single().Code);
            Assert.Empty(testAccessor.GetDiagnosticsForUriAndDocument(document.Id, mappedFileURIM1));
        }

        [Fact]
        public async Task RemoveDiagnosticForMappedFileToManyDocumentsTestAsync()
        {
            using var workspace = CreateTestWorkspace(new string[] { "", "" }, out _);
            var documents = workspace.CurrentSolution.Projects.First().Documents.ToImmutableArray();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            // Create diagnostic for the first document that has a mapped location.
            var mappedFilePath = documents[0].FilePath + "m1";
            var documentOneDiagnostic = await CreateMockDiagnosticDatasWithMappedLocationAsync(documents[0], ("doc1Diagnostic", mappedFilePath)).ConfigureAwait(false);
            // Create diagnostic for the second document that maps to the same location as the first document diagnostic.
            var documentTwoDiagnostic = await CreateMockDiagnosticDatasWithMappedLocationAsync(documents[1], ("doc2Diagnostic", mappedFilePath)).ConfigureAwait(false);

            // On the first call for this document, return the mapped diagnostic.  On the second, return nothing.
            SetupMockDiagnosticSequence(diagnosticsMock, documents[0].Id, documentOneDiagnostic, ImmutableArray<DiagnosticData>.Empty);
            // Always return the mapped diagnostic for this document.
            SetupMockWithDiagnostics(diagnosticsMock, documents[1].Id, documentTwoDiagnostic);

            // Publish three document change diagnostic notifications ->
            // 1.  doc1 with doc1Diagnostic = mapped file path m1
            // 2.  doc2 with doc2Diagnostic = mapped file path m1
            // 3.  doc1 with empty.
            //
            // We expect three publish diagnostics ->
            // 1.  from m1 with with doc1Diagnostic (triggered by 1 above to add doc1Diagnostic).
            // 2.  from m1 with doc1Diagnostic and doc2Diagnostic (triggered by 2 above to add doc2Diagnostic).
            // 3.  from m1 with just doc2Diagnostic (triggered by 3 above to remove doc1Diagnostic).
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 3, documents[0], documents[1], documents[0]).ConfigureAwait(false);

            Assert.Equal(3, results.Count);
            var expectedUri = new Uri(mappedFilePath);
            Assert.Equal(expectedUri, results[0].Uri);
            Assert.Equal("doc1Diagnostic", results[0].Diagnostics.Single().Code);

            Assert.Equal(expectedUri, results[1].Uri);
            Assert.Equal(2, results[1].Diagnostics.Length);
            Assert.Contains(results[1].Diagnostics, d => d.Code == "doc1Diagnostic");
            Assert.Contains(results[1].Diagnostics, d => d.Code == "doc2Diagnostic");

            Assert.Equal(expectedUri, results[2].Uri);
            Assert.Equal(1, results[2].Diagnostics.Length);
            Assert.Contains(results[2].Diagnostics, d => d.Code == "doc2Diagnostic");

            Assert.Single(testAccessor.GetFileUrisForDocument(documents[1].Id), expectedUri);
            Assert.Equal("doc2Diagnostic", testAccessor.GetDiagnosticsForUriAndDocument(documents[1].Id, expectedUri).Single().Code);
            Assert.Empty(testAccessor.GetDiagnosticsForUriAndDocument(documents[0].Id, expectedUri));
        }

        [Fact]
        public async Task ClearAllDiagnosticsForMappedFilesTestAsync()
        {
            using var workspace = CreateTestWorkspace("", out _);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            var mappedFilePathM1 = document.FilePath + "m1";
            var mappedFilePathM2 = document.FilePath + "m2";
            // Create two mapped diagnostics for the document on first call.
            // On the second call, return only empty diagnostics.
            SetupMockDiagnosticSequence(diagnosticsMock, document.Id,
                await CreateMockDiagnosticDatasWithMappedLocationAsync(document, ("id1", mappedFilePathM1), ("id2", mappedFilePathM2)).ConfigureAwait(false),
                ImmutableArray<DiagnosticData>.Empty);

            // Publish two document change diagnostic notifications ->
            // 1.  doc1 with id1 = mapped file m1 and id2 = mapped file m2.
            // 2.  doc1 with empty.
            //
            // We expect four publish diagnostic notifications - the first two are the two mapped files from 1.
            // The second two are the two mapped files being cleared by 2.
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 4, document, document).ConfigureAwait(false);

            var mappedFileURIM1 = new Uri(document.FilePath + "m1");
            var mappedFileURIM2 = new Uri(document.FilePath + "m2");

            Assert.Equal(4, results.Count);

            // Document's first update.
            Assert.Equal(mappedFileURIM1, results[0].Uri);
            Assert.Equal("id1", results[0].Diagnostics.Single().Code);

            Assert.Equal(mappedFileURIM2, results[1].Uri);
            Assert.Equal("id2", results[1].Diagnostics.Single().Code);

            // Document's second update.
            Assert.Equal(mappedFileURIM1, results[2].Uri);
            Assert.True(results[2].Diagnostics.IsEmpty());

            Assert.Equal(mappedFileURIM2, results[3].Uri);
            Assert.True(results[3].Diagnostics.IsEmpty());

            Assert.Empty(testAccessor.GetDocumentIdsInPublishedUris());
            Assert.Empty(testAccessor.GetFileUrisInPublishDiagnostics());
        }

        [Fact]
        public async Task ClearAllDiagnosticsForMappedFileToManyDocumentsTestAsync()
        {
            using var workspace = CreateTestWorkspace(new string[] { "", "" }, out _);
            var documents = workspace.CurrentSolution.Projects.First().Documents.ToImmutableArray();

            var diagnosticsMock = new Mock<IDiagnosticService>(MockBehavior.Strict);
            // Create diagnostic for the first document that has a mapped location.
            var mappedFilePath = documents[0].FilePath + "m1";
            var documentOneDiagnostic = await CreateMockDiagnosticDatasWithMappedLocationAsync(documents[0], ("doc1Diagnostic", mappedFilePath)).ConfigureAwait(false);
            // Create diagnostic for the second document that maps to the same location as the first document diagnostic.
            var documentTwoDiagnostic = await CreateMockDiagnosticDatasWithMappedLocationAsync(documents[1], ("doc2Diagnostic", mappedFilePath)).ConfigureAwait(false);

            // On the first call for the documents, return the mapped diagnostic.  On the second, return nothing.
            SetupMockDiagnosticSequence(diagnosticsMock, documents[0].Id, documentOneDiagnostic, ImmutableArray<DiagnosticData>.Empty);
            SetupMockDiagnosticSequence(diagnosticsMock, documents[1].Id, documentTwoDiagnostic, ImmutableArray<DiagnosticData>.Empty);

            // Publish four document change diagnostic notifications ->
            // 1.  doc1 with doc1Diagnostic = mapped file m1.
            // 2.  doc2 with doc2Diagnostic = mapped file m1.
            // 3.  doc1 with empty diagnostics.
            // 4.  doc2 with empty diagnostics.
            //
            // We expect four publish diagnostics ->
            // 1.  from URI m1 with doc1Diagnostic (triggered by 1 above to add doc1Diagnostic).
            // 2.  from URI m1 with doc1Diagnostic and doc2Diagnostic (triggered by 2 above to add doc2Diagnostic).
            // 3.  from URI m1 with just doc2Diagnostic (triggered by 3 above to clear doc1 diagnostic).
            // 4.  from URI m1 with empty (triggered by 4 above to also clear doc2 diagnostic).
            var (testAccessor, results) = await RunPublishDiagnosticsAsync(workspace, diagnosticsMock.Object, 4, documents[0], documents[1], documents[0], documents[1]).ConfigureAwait(false);

            Assert.Equal(4, results.Count);
            var expectedUri = new Uri(mappedFilePath);
            Assert.Equal(expectedUri, results[0].Uri);
            Assert.Equal("doc1Diagnostic", results[0].Diagnostics.Single().Code);

            Assert.Equal(expectedUri, results[1].Uri);
            Assert.Equal(2, results[1].Diagnostics.Length);
            Assert.Contains(results[1].Diagnostics, d => d.Code == "doc1Diagnostic");
            Assert.Contains(results[1].Diagnostics, d => d.Code == "doc2Diagnostic");

            Assert.Equal(expectedUri, results[2].Uri);
            Assert.Equal(1, results[2].Diagnostics.Length);
            Assert.Contains(results[2].Diagnostics, d => d.Code == "doc2Diagnostic");

            Assert.Equal(expectedUri, results[3].Uri);
            Assert.True(results[3].Diagnostics.IsEmpty());

            Assert.Empty(testAccessor.GetDocumentIdsInPublishedUris());
            Assert.Empty(testAccessor.GetFileUrisInPublishDiagnostics());
        }

        private async Task<(InProcLanguageServer.TestAccessor, List<LSP.PublishDiagnosticParams>)> RunPublishDiagnosticsAsync(Workspace workspace, IDiagnosticService diagnosticService,
            int expectedNumberOfCallbacks, params Document[] documentsToPublish)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            var languageServer = CreateLanguageServer(serverStream, serverStream, workspace, diagnosticService);

            // Notification target for tests to recieve the notification details
            var callback = new Callback(expectedNumberOfCallbacks);
            using var jsonRpc = new JsonRpc(clientStream, clientStream, callback);

            // The json rpc messages won't necessarily come back in order by default.
            // So use a synchronization context to preserve the original ordering.
            // https://github.com/microsoft/vs-streamjsonrpc/blob/bc970c61b90db5db135a1b3d1c72ef355c2112af/doc/resiliency.md#when-message-order-is-important
            jsonRpc.SynchronizationContext = new RpcOrderPreservingSynchronizationContext();
            jsonRpc.StartListening();

            // Triggers language server to send notifications.
            foreach (var document in documentsToPublish)
            {
                await languageServer.PublishDiagnosticsAsync(document).ConfigureAwait(false);
            }

            // Waits for all notifications to be recieved.
            await callback.CallbackCompletedTask.Task.ConfigureAwait(false);

            return (languageServer.GetTestAccessor(), callback.Results);

            static InProcLanguageServer CreateLanguageServer(Stream inputStream, Stream outputStream, Workspace workspace, IDiagnosticService mockDiagnosticService)
            {
                var protocol = ((TestWorkspace)workspace).ExportProvider.GetExportedValue<LanguageServerProtocol>();

                var languageServer = new InProcLanguageServer(inputStream, outputStream, protocol, workspace, mockDiagnosticService, clientName: null);
                return languageServer;
            }
        }

        private void SetupMockWithDiagnostics(Mock<IDiagnosticService> diagnosticServiceMock, DocumentId documentId, IEnumerable<DiagnosticData> diagnostics)
        {
            diagnosticServiceMock.Setup(d => d.GetDiagnostics(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), documentId,
                    It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(diagnostics);
        }

        private void SetupMockDiagnosticSequence(Mock<IDiagnosticService> diagnosticServiceMock, DocumentId documentId,
            IEnumerable<DiagnosticData> firstDiagnostics, IEnumerable<DiagnosticData> secondDiagnostics)
        {
            diagnosticServiceMock.SetupSequence(d => d.GetDiagnostics(It.IsAny<Workspace>(), It.IsAny<ProjectId>(), documentId,
                    It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(firstDiagnostics)
                .Returns(secondDiagnostics);
        }

        private async Task<IEnumerable<DiagnosticData>> CreateMockDiagnosticDataAsync(Document document, string id)
        {
            var descriptor = new DiagnosticDescriptor(id, "", "", "", DiagnosticSeverity.Error, true);
            var location = Location.Create(await document.GetRequiredSyntaxTreeAsync(CancellationToken.None).ConfigureAwait(false), new TextSpan());
            return new DiagnosticData[] { DiagnosticData.Create(Diagnostic.Create(descriptor, location), document) };
        }

        private async Task<ImmutableArray<DiagnosticData>> CreateMockDiagnosticDatasWithMappedLocationAsync(Document document, params (string diagnosticId, string mappedFilePath)[] diagnostics)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None).ConfigureAwait(false);

            return diagnostics.Select(d => CreateMockDiagnosticDataWithMappedLocation(document, tree, d.diagnosticId, d.mappedFilePath)).ToImmutableArray();

            static DiagnosticData CreateMockDiagnosticDataWithMappedLocation(Document document, SyntaxTree tree, string id, string mappedFilePath)
            {
                var descriptor = new DiagnosticDescriptor(id, "", "", "", DiagnosticSeverity.Error, true);
                var location = Location.Create(tree, new TextSpan());

                var diagnostic = Diagnostic.Create(descriptor, location);
                return new DiagnosticData(diagnostic.Id,
                    diagnostic.Descriptor.Category,
                    null,
                    null,
                    diagnostic.Severity,
                    diagnostic.DefaultSeverity,
                    diagnostic.Descriptor.IsEnabledByDefault,
                    diagnostic.WarningLevel,
                    diagnostic.Descriptor.CustomTags.AsImmutableOrEmpty(),
                    diagnostic.Properties,
                    document.Project.Id,
                    GetDataLocation(document, mappedFilePath),
                    null,
                    document.Project.Language,
                    diagnostic.Descriptor.Title.ToString(),
                    diagnostic.Descriptor.Description.ToString(),
                    null,
                    diagnostic.IsSuppressed);
            }

            static DiagnosticDataLocation GetDataLocation(Document document, string mappedFilePath)
                => new DiagnosticDataLocation(document.Id, originalFilePath: document.FilePath, mappedFilePath: mappedFilePath);
        }

        /// <summary>
        /// Synchronization context to preserve ordering of the RPC messages
        /// Adapted from https://dev.azure.com/devdiv/DevDiv/VS%20Cloud%20Kernel/_git/DevCore?path=%2Fsrc%2Fclr%2FMicrosoft.ServiceHub.Framework%2FServiceRpcDescriptor%2BRpcOrderPreservingSynchronizationContext.cs
        /// https://github.com/microsoft/vs-streamjsonrpc/issues/440 tracks exposing functionality so we don't need to copy this.
        /// </summary>
        private class RpcOrderPreservingSynchronizationContext : SynchronizationContext, IDisposable
        {
            /// <summary>
            /// The queue of work to execute.
            /// </summary>
            private readonly AsyncQueue<(SendOrPostCallback, object?)> _queue = new AsyncQueue<(SendOrPostCallback, object?)>();

            public RpcOrderPreservingSynchronizationContext()
            {
                // Process the work in the background.
                this.ProcessQueueAsync().Forget();
            }

            public override void Post(SendOrPostCallback d, object? state) => this._queue.Enqueue((d, state));

            public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

            public override SynchronizationContext CreateCopy() => throw new NotSupportedException();

            /// <summary>
            /// Causes this <see cref="SynchronizationContext"/> to reject all future posted work and
            /// releases the queue processor when it is empty.
            /// </summary>
            public void Dispose() => this._queue.Complete();

            /// <summary>
            /// Executes queued work on the threadpool, one at a time.
            /// Don't catch exceptions - let them bubble up to fail the test.
            /// </summary>
            private async Task ProcessQueueAsync()
            {
                while (!this._queue.IsCompleted)
                {
                    var work = await this._queue.DequeueAsync().ConfigureAwait(false);
                    work.Item1(work.Item2);
                }
            }
        }

        private class Callback
        {
            /// <summary>
            /// Task that can be awaited for the all callbacks to complete.
            /// </summary>
            public TaskCompletionSource<object> CallbackCompletedTask { get; }

            /// <summary>
            /// Serialized results of all publish diagnostic notifications recieved by this callback.
            /// </summary>
            public List<LSP.PublishDiagnosticParams> Results { get; }

            /// <summary>
            /// Lock to guard concurrent callbacks.
            /// </summary>
            private readonly object _lock = new object();

            /// <summary>
            /// The expected number of times this callback should be hit.
            /// Used in conjunction with <see cref="_currentNumberOfCallbacks"/>
            /// to determine if the callbacks are complete.
            /// </summary>
            private readonly int _expectedNumberOfCallbacks;

            /// <summary>
            /// The current number of callbacks that this callback has been hit.
            /// </summary>
            private int _currentNumberOfCallbacks;

            public Callback(int expectedNumberOfCallbacks)
            {
                CallbackCompletedTask = new TaskCompletionSource<object>();
                Results = new List<LSP.PublishDiagnosticParams>();
                _expectedNumberOfCallbacks = expectedNumberOfCallbacks;
                _currentNumberOfCallbacks = 0;
            }

            [JsonRpcMethod(LSP.Methods.TextDocumentPublishDiagnosticsName)]
            public Task OnDiagnosticsPublished(JToken input)
            {
                lock (_lock)
                {
                    _currentNumberOfCallbacks++;
                    Contract.ThrowIfTrue(_currentNumberOfCallbacks > _expectedNumberOfCallbacks, "received too many callbacks");

                    var diagnosticParams = input.ToObject<LSP.PublishDiagnosticParams>();
                    Results.Add(diagnosticParams);

                    if (_currentNumberOfCallbacks == _expectedNumberOfCallbacks)
                    {
                        CallbackCompletedTask.SetResult(new object());
                    }

                    return Task.CompletedTask;
                }
            }
        }
    }
}
