// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.VisualStudio.Razor.LanguageClient.Cohost.HtmlDocumentSynchronizer;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class HtmlDocumentSynchronizerTest(ITestOutputHelper testOutput) : VisualStudioWorkspaceTestBase(testOutput)
{
    private DocumentId? _documentId;

    protected override void ConfigureWorkspace(AdhocWorkspace workspace)
    {
        var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
        var document = project.AddAdditionalDocument("File.razor", SourceText.From("<div></div>"), filePath: "file://File.razor");
        _documentId = document.Id;

        Assert.True(workspace.TryApplyChanges(document.Project.Solution));
    }

    [Fact]
    public async Task TrySynchronize_FailsIfPublishFails()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher(publishResult: false);
        var remoteServiceInvoker = new RemoteServiceInvoker(document);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        Assert.False((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);
    }

    [Fact]
    public async Task TrySynchronize_NewDocument_Generates()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        Assert.True((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
            });
    }

    [Fact]
    public async Task TrySynchronize_ReopenedDocument_Generates()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var syncResult = await synchronizer.TrySynchronizeAsync(document, DisposalToken);
        Assert.True(syncResult.Synchronized);

        // "Close" the document
        synchronizer.DocumentRemoved(document.CreateUri(), DisposalToken);

        Assert.True((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
                Assert.Equal(syncResult.Checksum, i.Checksum);
            },
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
                Assert.Equal(syncResult.Checksum, i.Checksum); // Same document, so same checksum, even though its a different publish
            });
    }

    [Fact]
    public async Task TrySynchronize_CancelledGeneration_Generates()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        remoteServiceInvoker.OOPReturnsNull = true;
        Assert.False((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);

        remoteServiceInvoker.OOPReturnsNull = false;
        Assert.True((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
            });
    }

    [Fact]
    public async Task TrySynchronize_ExceptionDuringGeneration_Generates()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document, () => throw new Exception());
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        Assert.False((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);

        // Stop throwing exceptions :)
        remoteServiceInvoker.GenerateTask = null;
        Assert.True((await synchronizer.TrySynchronizeAsync(document, DisposalToken)).Synchronized);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
            });
    }

    [Fact]
    public async Task TrySynchronize_WorkspaceMovedForward_NoDocumentChanges_DoesntGenerate()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var version1 = await RazorDocumentVersion.CreateAsync(document, DisposalToken);

        var syncResult = await synchronizer.TrySynchronizeAsync(document, DisposalToken);
        Assert.True(syncResult.Synchronized);

        // Add a new document, moving the workspace forward but leaving our document unaffected
        Assert.True(Workspace.TryApplyChanges(document.Project.AddAdditionalDocument("Foo2.razor", SourceText.From(""), filePath: "file://Foo2.razor").Project.Solution));

        document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();
        var version2 = await RazorDocumentVersion.CreateAsync(document, DisposalToken);

        var syncResult2 = await synchronizer.TrySynchronizeAsync(document, DisposalToken);
        Assert.True(syncResult2.Synchronized);
        Assert.Equal(syncResult.Checksum, syncResult2.Checksum);

        // Validate that the workspace moved forward
        Assert.NotEqual(version1.WorkspaceVersion, version2.WorkspaceVersion);

        // Still only one publish
        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
                Assert.Equal(syncResult.Checksum, i.Checksum);
            });
    }

    [Fact]
    public async Task TrySynchronize_WorkspaceUnchanged_DocumentChanges_Generates()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var version1 = await RazorDocumentVersion.CreateAsync(document, DisposalToken);

        var syncResult = await synchronizer.TrySynchronizeAsync(document, DisposalToken);
        Assert.True(syncResult.Synchronized);

        // Change our document directly, but without applying changes (equivalent to LSP didChange)
        var solution = Workspace.CurrentSolution.WithAdditionalDocumentText(_documentId.AssumeNotNull(), SourceText.From("<span></span>"));
        document = solution.GetAdditionalDocument(_documentId).AssumeNotNull();
        var version2 = await RazorDocumentVersion.CreateAsync(document, DisposalToken);

        var syncResult2 = await synchronizer.TrySynchronizeAsync(document, DisposalToken);
        Assert.True(syncResult2.Synchronized);

        // Validate that the workspace hasn't moved forward
        Assert.Equal(version1.WorkspaceVersion, version2.WorkspaceVersion);
        Assert.NotEqual(syncResult.Checksum, syncResult2.Checksum);

        // We should have two publishes
        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
                Assert.Equal(syncResult.Checksum, i.Checksum);
            },
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<span></span>", i.Text);
                Assert.Equal(syncResult2.Checksum, i.Checksum);
            });
    }

    [Fact]
    public async Task TrySynchronize_RequestOldVersion_ImmediateFail()
    {
        var document1 = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var tcs = new TaskCompletionSource<bool>();
        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document1, () => tcs.Task);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var version1 = await RazorDocumentVersion.CreateAsync(document1, DisposalToken);

        Assert.True(Workspace.TryApplyChanges(Workspace.CurrentSolution.WithAdditionalDocumentText(_documentId.AssumeNotNull(), SourceText.From("<span></span>"))));
        var document2 = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var task = synchronizer.TrySynchronizeAsync(document2, DisposalToken);

        Assert.False((await synchronizer.TrySynchronizeAsync(document1, DisposalToken)).Synchronized);

        tcs.SetResult(true);

        Assert.True((await task).Synchronized);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<span></span>", i.Text);
            });
    }

    [Fact]
    public async Task TrySynchronize_RequestSameVersion_SingleGeneration()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var tcs = new TaskCompletionSource<bool>();
        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document, () => tcs.Task);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var task1 = synchronizer.TrySynchronizeAsync(document, DisposalToken);
        var task2 = synchronizer.TrySynchronizeAsync(document, DisposalToken);

        tcs.SetResult(true);

        await Task.WhenAll(task1, task2);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
            });
    }

    [Fact]
    public async Task TrySynchronize_RequestNewVersion_CancelOldTask()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var tcs = new TaskCompletionSource<bool>();
        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document, () => tcs.Task);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var task1 = synchronizer.TrySynchronizeAsync(document, DisposalToken);

        // Change our document directly, but without applying changes (equivalent to LSP didChange)
        var solution = Workspace.CurrentSolution.WithAdditionalDocumentText(_documentId.AssumeNotNull(), SourceText.From("<span></span>"));
        document = solution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var task2 = synchronizer.TrySynchronizeAsync(document, DisposalToken);

        tcs.SetResult(true);

        await Task.WhenAll(task1, task2);
        Assert.False(task1.VerifyCompleted().Synchronized);

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<span></span>", i.Text);
            });
    }

    [Fact]
    public async Task GetSynchronizationRequestTask_RequestSameVersion_InvokedRemoteOnce()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var tcs = new TaskCompletionSource<bool>();
        var publisher = new TestHtmlDocumentPublisher();

        var remoteInvocations = 0;
        var remoteServiceInvoker = new RemoteServiceInvoker(document, () =>
        {
            remoteInvocations++;
            return tcs.Task;
        });
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var version = await RazorDocumentVersion.CreateAsync(document, DisposalToken);

        var accessor = synchronizer.GetTestAccessor();
        var task1 = accessor.GetSynchronizationRequestTaskAsync(document, version, DisposalToken);
        var task2 = accessor.GetSynchronizationRequestTaskAsync(document, version, DisposalToken);

        Assert.Equal(1, remoteInvocations);
    }

    [Fact]
    public async Task TrySynchronize_RequestSameVersion_NoTimeout()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var tcs = new TaskCompletionSource<bool>();
        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker(document, () => tcs.Task);
        var synchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);

        var task1 = synchronizer.TrySynchronizeAsync(document, DisposalToken);
        await Task.Delay(2000);

        tcs.SetResult(true);

        await task1;

        Assert.Collection(publisher.Publishes,
            i =>
            {
                Assert.Equal(_documentId, i.Document.Id);
                Assert.Equal("<div></div>", i.Text);
            });
    }

    private class RemoteServiceInvoker(TextDocument document, Func<Task>? generateTask = null) : IRemoteServiceInvoker
    {
        private readonly DocumentId _documentId = document.Id;

        public bool OOPReturnsNull { get; set; }
        public Func<Task>? GenerateTask { get; set; } = generateTask;

        public async ValueTask<TResult?> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null) where TService : class
        {
            Assert.Equal(typeof(string), typeof(TResult));
            Assert.Equal(typeof(IRemoteHtmlDocumentService), typeof(TService));

            if (GenerateTask is not null)
            {
                await GenerateTask();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            if (OOPReturnsNull)
            {
                return default;
            }

            return (TResult)(object)(await solution.GetAdditionalDocument(_documentId).AssumeNotNull().GetTextAsync(cancellationToken)).ToString();
        }
    }
}
