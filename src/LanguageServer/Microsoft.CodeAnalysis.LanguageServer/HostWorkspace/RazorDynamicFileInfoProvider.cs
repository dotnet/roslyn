// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Shared]
[Export(typeof(IDynamicFileInfoProvider))]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
[ExportCSharpVisualBasicStatelessLspService(typeof(RazorDynamicFileInfoProvider))]
[Method("razor/dynamicFileInfoChanged")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorDynamicFileInfoProvider(Lazy<RazorWorkspaceListenerInitializer> razorWorkspaceListenerInitializer) : IDynamicFileInfoProvider, ILspServiceNotificationHandler<RazorDynamicFileChangedParams>
{
    private const string ProvideRazorDynamicFileInfoMethodName = "razor/provideDynamicFileInfo";
    private const string RemoveRazorDynamicFileInfoMethodName = "razor/removeDynamicFileInfo";

    private readonly Lazy<RazorWorkspaceListenerInitializer> _razorWorkspaceListenerInitializer = razorWorkspaceListenerInitializer;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    public event EventHandler<string>? Updated;

    public Task HandleNotificationAsync(RazorDynamicFileChangedParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        Updated?.Invoke(this, ProtocolConversions.GetDocumentFilePathFromUri(request.CSharpDocument.Uri));
        return Task.CompletedTask;
    }

    public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        _razorWorkspaceListenerInitializer.Value.NotifyDynamicFile(projectId);

        var requestParams = new RazorProvideDynamicFileParams
        {
            RazorDocument = new()
            {
                Uri = ProtocolConversions.CreateAbsoluteUri(filePath)
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

        if (response.Edits is not null)
        {
            var workspaceManager = LanguageServerHost.Instance.GetRequiredLspService<LspWorkspaceManager>(); ;
            var (workspace, solution, document) = await workspaceManager.GetLspDocumentInfoAsync(response.CSharpDocument, cancellationToken).ConfigureAwait(false);

            var sourceText = document is null
                ? SourceText.From("")
                : await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var version = document is null
                ? VersionStamp.Default
                : await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

            var textChanges = response.Edits.Select(e => new TextChange(e.Span.ToTextSpan(), e.NewText));
            var newText = sourceText.WithChanges(textChanges);

            var textAndVersion = TextAndVersion.Create(newText, version);
            return new DynamicFileInfo(dynamicFileInfoFilePath, SourceCodeKind.Regular, TextLoader.From(textAndVersion), designTimeOnly: true, documentServiceProvider: null);
        }

        return new DynamicFileInfo(dynamicFileInfoFilePath, SourceCodeKind.Regular, EmptyStringTextLoader.Instance, designTimeOnly: true, documentServiceProvider: null);
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

    private sealed class EmptyStringTextLoader : TextLoader
    {
        public static readonly TextLoader Instance = new EmptyStringTextLoader();

        private EmptyStringTextLoader() { }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(TextAndVersion.Create(SourceText.From(""), VersionStamp.Default));
        }
    }
}
