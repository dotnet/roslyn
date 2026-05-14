// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class WorkDoneProgressTests : AbstractLanguageServerProtocolTests
{
    private const string Title = "Test progress";
    private const string StartMessage = "Starting";
    private const string ReportMessage = "Working";
    private const string EndMessage = "Finished";
    private static readonly string CancelledMessage = LanguageServerProtocolResources.Cancelled;

    public WorkDoneProgressTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(TestWorkDoneProgressServiceFactory));

    [Theory, CombinatorialData]
    public async Task ProgressCanBeCreatedReportedAndCompleted(bool mutatingLspWorkspace)
    {
        var clientCallbackTarget = new ClientCallbackTarget();
        await using var server = await CreateTestServerAsync(mutatingLspWorkspace, clientCallbackTarget);

        await GetTestService(server).RunCompleteWorkDoneProgress();

        var end = await clientCallbackTarget.WaitForEndAsync();
        var progressReports = clientCallbackTarget.GetProgressReports();
        Assert.Collection(
            progressReports,
            progressReport => Assert.IsType<WorkDoneProgressBegin>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressReport>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressEnd>(progressReport.Value));

        Assert.All(progressReports, progressReport => Assert.Equal(end.Token, progressReport.Token));
    }

    [Theory, CombinatorialData]
    public async Task ThrowingDuringProgressCompletesProgress(bool mutatingLspWorkspace)
    {
        var clientCallbackTarget = new ClientCallbackTarget();
        await using var server = await CreateTestServerAsync(mutatingLspWorkspace, clientCallbackTarget);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await GetTestService(server).RunThrowingWorkDoneProgress());

        await clientCallbackTarget.WaitForEndAsync();

        var progressReports = clientCallbackTarget.GetProgressReports();
        Assert.Collection(
            progressReports,
            progressReport => Assert.IsType<WorkDoneProgressBegin>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressReport>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressEnd>(progressReport.Value));

        Assert.Equal(EndMessage, ((WorkDoneProgressEnd)progressReports[2].Value).Message);
    }

    [Theory, CombinatorialData]
    public async Task ClientCancellingProgressDoesNotReceiveProgressEnd(bool mutatingLspWorkspace)
    {
        var clientCallbackTarget = new ClientCallbackTarget();
        await using var server = await CreateTestServerAsync(mutatingLspWorkspace, clientCallbackTarget);

        var requestTask = GetTestService(server).RunClientCancellationWorkDoneProgress();
        var report = await clientCallbackTarget.WaitForReportAsync();

        await server.ExecuteNotificationAsync(Methods.WindowWorkDoneProgressCancelName, new WorkDoneProgressCancelParams
        {
            Token = report.Token,
        });

        await requestTask;

        var progressReports = clientCallbackTarget.GetProgressReports();
        Assert.Collection(
            progressReports,
            progressReport => Assert.IsType<WorkDoneProgressBegin>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressReport>(progressReport.Value));
    }

    [Theory, CombinatorialData]
    public async Task ServerCancellationCancelsProgressOnClient(bool mutatingLspWorkspace)
    {
        var clientCallbackTarget = new ClientCallbackTarget();
        await using var server = await CreateTestServerAsync(mutatingLspWorkspace, clientCallbackTarget);

        var serverCancellationTokenSource = new CancellationTokenSource();

        // Task to hold open the progress on the server until we've observed the server cancellation on the client.
        var serverProgressCompletionSource = new TaskCompletionSource<object?>();
        var requestTask = GetTestService(server).RunServerCancellationWorkDoneProgress(serverProgressCompletionSource, serverCancellationTokenSource.Token);
        await clientCallbackTarget.WaitForReportAsync();

        // Cancel the progress using the fake server cancellation token.  This should cause the client to receive a progress end with a cancellation message.
        serverCancellationTokenSource.Cancel();
        await clientCallbackTarget.WaitForServerCancelledAsync();

        // Complete the server progress task to allow the server to finish and dispose of the progress reporter.
        serverProgressCompletionSource.SetResult(null);
        await requestTask;

        var progressReports = clientCallbackTarget.GetProgressReports();
        Assert.Collection(
            progressReports,
            progressReport => Assert.IsType<WorkDoneProgressBegin>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressReport>(progressReport.Value),
            progressReport => Assert.IsType<WorkDoneProgressEnd>(progressReport.Value));

        Assert.Equal(CancelledMessage, ((WorkDoneProgressEnd)progressReports[2].Value).Message);
    }

    private async Task<TestLspServer> CreateTestServerAsync(bool mutatingLspWorkspace, ClientCallbackTarget clientCallbackTarget)
    {
        var initializationOptions = new InitializationOptions
        {
            ClientCapabilities = new ClientCapabilities
            {
                Window = new WindowClientCapabilities
                {
                    WorkDoneProgress = true,
                },
            },
            ClientTarget = clientCallbackTarget,
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
        };

        return await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, initializationOptions);
    }

    private static TestWorkDoneProgressService GetTestService(TestLspServer server)
        => server.GetRequiredLspService<TestWorkDoneProgressService>();

    [ExportCSharpVisualBasicLspServiceFactory(typeof(TestWorkDoneProgressService)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestWorkDoneProgressServiceFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
            => new TestWorkDoneProgressService(lspServices.GetRequiredService<WorkDoneProgressManager>());
    }

    internal sealed class TestWorkDoneProgressService(WorkDoneProgressManager workDoneProgressManager) : ILspService
    {
        public async Task RunCompleteWorkDoneProgress()
        {
            await using var progress = await CreateProgressAndReport(CancellationToken.None);
        }

        public async Task RunThrowingWorkDoneProgress()
        {
            await using var progress = await CreateProgressAndReport(CancellationToken.None);
            throw new InvalidOperationException("Test progress failed.");
        }

        public async Task RunClientCancellationWorkDoneProgress()
        {
            await using var progress = await CreateProgressAndReport(CancellationToken.None);
            await WaitForCancellationAsync(progress.CancellationToken);
        }

        public async Task RunServerCancellationWorkDoneProgress(TaskCompletionSource<object?> serverProgressCompletedSource, CancellationToken serverCancellationToken)
        {
            await using var progress = await CreateProgressAndReport(serverCancellationToken);
            await WaitForCancellationAsync(serverCancellationToken);

            await serverProgressCompletedSource.Task;
        }

        private async Task<IWorkDoneProgressReporter> CreateProgressAndReport(CancellationToken cancellationToken)
        {
            var progress = await workDoneProgressManager.CreateWorkDoneProgressAsync(
                reportProgressToClient: true,
                title: Title,
                startMessage: StartMessage,
                endMessage: EndMessage,
                clientCanCancel: true,
                serverCancellationToken: cancellationToken);

            progress.Report(new WorkDoneProgressReport
            {
                Message = ReportMessage,
                Cancellable = true,
                Percentage = 50,
            });

            return progress;
        }

        private static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private sealed class ClientCallbackTarget
    {
        private readonly object _gate = new();
        private bool _createReceived = false;
        private readonly TaskCompletionSource<ProgressReportParams> _reportSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ProgressReportParams> _endSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ProgressReportParams> _cancelledEndSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<ProgressReportParams> _progressReports = [];

        [JsonRpcMethod(Methods.WindowWorkDoneProgressCreateName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleCreateWorkDoneProgressAsync(WorkDoneProgressCreateParams _, CancellationToken _1)
        {
            lock (_gate)
            {
                Contract.ThrowIfTrue(_createReceived, "Received multiple create progress calls.");
                _createReceived = true;
            }

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ProgressNotificationName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleProgressAsync(JsonElement progressParams, CancellationToken _)
        {
            var progressReport = progressParams.Deserialize<ProgressReportParams>(ProtocolConversions.LspJsonSerializerOptions);

            lock (_gate)
            {
                Contract.ThrowIfFalse(_createReceived, "Received progress report before create.");
                _progressReports.Add(progressReport);
                switch (progressReport.Value)
                {
                    case WorkDoneProgressEnd { Message: EndMessage }:
                        _endSource.TrySetResult(progressReport);
                        break;
                    case WorkDoneProgressEnd { Message: var message } when message == CancelledMessage:
                        _cancelledEndSource.TrySetResult(progressReport);
                        break;
                    case WorkDoneProgressReport:
                        _reportSource.TrySetResult(progressReport);
                        break;
                }
            }

            return Task.CompletedTask;
        }

        public async Task<ProgressReportParams> WaitForReportAsync()
            => await _reportSource.Task;

        public async Task<ProgressReportParams> WaitForEndAsync()
            => await _endSource.Task;

        public async Task<ProgressReportParams> WaitForServerCancelledAsync()
            => await _cancelledEndSource.Task;

        public ImmutableArray<ProgressReportParams> GetProgressReports()
        {
            lock (_gate)
            {
                return [.. _progressReports];
            }
        }
    }

    private readonly record struct ProgressReportParams(
        [property: JsonPropertyName("token")] string Token,
        [property: JsonPropertyName("value")] WorkDoneProgress Value);
}
