// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class HtmlRequestInvokerTest(ITestOutputHelper testOutput) : VisualStudioWorkspaceTestBase(testOutput)
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
    public async Task DiagnosticsRequest_UpdatesUri()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var htmlDocumentUri = new Uri("file://File.razor.html", UriKind.Absolute);
        var requestValidator = (object request) =>
        {
            var diagnosticParams = Assert.IsType<VSInternalDiagnosticParams>(request);
            Assert.Equal(htmlDocumentUri, diagnosticParams.TextDocument!.DocumentUri.GetRequiredParsedUri());
        };

        var diagnosticRequest = new VSInternalDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() }
        };

        await MakeHtmlRequestAsync(document, htmlDocumentUri, requestValidator, VSInternalMethods.DocumentPullDiagnosticName, diagnosticRequest);

        Assert.Equal(document.CreateDocumentUri(), diagnosticRequest.TextDocument!.DocumentUri);
    }

    [Fact]
    public async Task ITextDocumentParamsRequest_UpdatesUri()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var htmlDocumentUri = new Uri("file://File.razor.html", UriKind.Absolute);
        var requestValidator = (object request) =>
        {
            var hoverParams = Assert.IsAssignableFrom<ITextDocumentParams>(request);
            Assert.Equal(htmlDocumentUri, hoverParams.TextDocument!.DocumentUri.GetRequiredParsedUri());
        };

        var hoverRequest = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() }
        };

        await MakeHtmlRequestAsync(document, htmlDocumentUri, requestValidator, Methods.TextDocumentHoverName, hoverRequest);

        Assert.Equal(document.CreateDocumentUri(), hoverRequest.TextDocument!.DocumentUri);
    }

    [Fact]
    public async Task VSCodeActionParamsRequest_UpdatesUri()
    {
        var document = Workspace.CurrentSolution.GetAdditionalDocument(_documentId).AssumeNotNull();

        var htmlDocumentUri = new Uri("file://File.razor.html", UriKind.Absolute);
        var requestValidator = (object request) =>
        {
            var codeActionParams = Assert.IsType<VSCodeActionParams>(request);
            Assert.Equal(htmlDocumentUri, codeActionParams.TextDocument!.DocumentUri.GetRequiredParsedUri());
        };

        var codeActionsRequest = new VSCodeActionParams
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() },
            Range = LspFactory.DefaultRange,
            Context = new VSInternalCodeActionContext()
        };

        await MakeHtmlRequestAsync(document, htmlDocumentUri, requestValidator, Methods.TextDocumentCodeActionName, codeActionsRequest);

        Assert.Equal(document.CreateDocumentUri(), codeActionsRequest.TextDocument!.DocumentUri);
    }

    private async Task MakeHtmlRequestAsync<TRequest>(TextDocument document, Uri htmlDocumentUri, Action<object> requestValidator, string method, TRequest request)
       where TRequest : notnull
    {
        var htmlTextSnapshot = new StringTextSnapshot("");
        var htmlTextBuffer = new TestTextBuffer(htmlTextSnapshot);
        var checksum = await document.GetChecksumAsync(DisposalToken);
        var requestInvoker = new TestLSPRequestInvoker((method, null));
        var lspDocumentManager = new TestDocumentManager();
        var htmlVirtualDocument = new HtmlVirtualDocumentSnapshot(htmlDocumentUri, htmlTextBuffer.CurrentSnapshot, hostDocumentSyncVersion: 1, state: checksum);
        var documentSnapshot = new TestLSPDocumentSnapshot(document.CreateUri(), version: (int)(htmlVirtualDocument.HostDocumentSyncVersion!.Value + 1), htmlVirtualDocument);
        lspDocumentManager.AddDocument(documentSnapshot.Uri, documentSnapshot);

        var publisher = new TestHtmlDocumentPublisher();
        var remoteServiceInvoker = new RemoteServiceInvoker();
        var htmlDocumentSynchronizer = new HtmlDocumentSynchronizer(remoteServiceInvoker, publisher, LoggerFactory);
        var invoker = new HtmlRequestInvoker(requestInvoker, lspDocumentManager, htmlDocumentSynchronizer, NoOpTelemetryReporter.Instance, LoggerFactory);

        var validated = false;
        requestInvoker.RequestAction = r =>
        {
            validated = true;
            requestValidator(r);
        };

        _ = await invoker.MakeHtmlLspRequestAsync<TRequest, object>(
            document,
            method,
            request,
            DisposalToken);

        Assert.True(validated);
    }

    private class RemoteServiceInvoker : IRemoteServiceInvoker
    {
        public ValueTask<TResult?> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null) where TService : class
        {
            return new((TResult?)(object)"");
        }
    }
}
