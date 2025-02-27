// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[Export(typeof(IDynamicFileInfoProvider))]
[Export(typeof(RazorDynamicFileInfoProvider))]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
internal partial class RazorDynamicFileInfoProvider : IDynamicFileInfoProvider
{
    private const string ProvideRazorDynamicFileInfoMethodName = "razor/provideDynamicFileInfo";
    private const string RemoveRazorDynamicFileInfoMethodName = "razor/removeDynamicFileInfo";

    private readonly Lazy<RazorWorkspaceListenerInitializer> _razorWorkspaceListenerInitializer;
    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly AsyncBatchingWorkQueue<string> _updateWorkQueue;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorDynamicFileInfoProvider(
        Lazy<RazorWorkspaceListenerInitializer> razorWorkspaceListenerInitializer,
        LanguageServerWorkspaceFactory workspaceFactory,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _razorWorkspaceListenerInitializer = razorWorkspaceListenerInitializer;
        _updateWorkQueue = new AsyncBatchingWorkQueue<string>(
            TimeSpan.FromMilliseconds(200),
            UpdateAsync,
            listenerProvider.GetListener(nameof(RazorDynamicFileInfoProvider)),
            CancellationToken.None);
        _workspaceFactory = workspaceFactory;
    }

    public event EventHandler<string>? Updated;

    public void Update(string filePath)
    {
        _updateWorkQueue.AddWork(filePath);
    }

    public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        _razorWorkspaceListenerInitializer.Value.NotifyDynamicFile(projectId);

        var razorUri = ProtocolConversions.CreateAbsoluteUri(filePath);
        var requestParams = new RazorProvideDynamicFileParams
        {
            RazorDocument = new()
            {
                Uri = razorUri
            }
        };

        Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
        var clientLanguageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();

        var response = await clientLanguageServerManager.SendRequestAsync<RazorProvideDynamicFileParams, RazorProvideDynamicFileResponse>(
            ProvideRazorDynamicFileInfoMethodName, requestParams, cancellationToken);

        if (response.CSharpDocument is null)
        {
            return null;
        }

        // Since we only sent one file over, we should get either zero or one URI back
        var responseUri = response.CSharpDocument.Uri;
        var dynamicFileInfoFilePath = ProtocolConversions.GetDocumentFilePathFromUri(responseUri);

        if (response.Updates is not null)
        {
            var textDocument = await _workspaceFactory.Workspace.CurrentSolution.GetTextDocumentAsync(response.CSharpDocument, cancellationToken).ConfigureAwait(false);
            var checksum = Convert.FromBase64String(response.Checksum);
            var textLoader = new TextChangesTextLoader(
                textDocument,
                response.Updates,
                checksum,
                response.ChecksumAlgorithm,
                response.SourceEncodingCodePage,
                razorUri);

            return new DynamicFileInfo(
                dynamicFileInfoFilePath,
                SourceCodeKind.Regular,
                textLoader,
                designTimeOnly: true,
                documentServiceProvider: null);
        }

        return new DynamicFileInfo(
            dynamicFileInfoFilePath,
            SourceCodeKind.Regular,
            EmptyStringTextLoader.Instance,
            designTimeOnly: true,
            documentServiceProvider: null);
    }

    public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        var notificationParams = new RazorRemoveDynamicFileParams
        {
            CSharpDocument = new()
            {
                Uri = ProtocolConversions.CreateAbsoluteUri(filePath)
            }
        };

        Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
        var clientLanguageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();

        return clientLanguageServerManager.SendNotificationAsync(
            RemoveRazorDynamicFileInfoMethodName, notificationParams, cancellationToken).AsTask();
    }

    private ValueTask UpdateAsync(ImmutableSegmentedList<string> paths, CancellationToken token)
    {
        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();
            Updated?.Invoke(this, path);
        }

        return ValueTask.CompletedTask;
    }

    private sealed class EmptyStringTextLoader() : TextLoader
    {
        public static readonly TextLoader Instance = new EmptyStringTextLoader();

        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(TextAndVersion.Create(SourceText.From(""), VersionStamp.Default));
        }
    }
}
