// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(IDynamicFileInfoProvider)), Shared]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
internal class RazorDynamicFileInfoProvider : IDynamicFileInfoProvider
{
    private const string ProvideRazorDynamicFileInfoMethodName = "razor/provideDynamicFileInfo";

    [DataContract]
    private class ProvideDynamicFileParams
    {
        [DataMember(Name = "razorFiles")]
        public required Uri[] RazorFiles { get; set; }
    }

    [DataContract]
    private class ProvideDynamicFileResponse
    {
        [DataMember(Name = "generatedFiles")]
        public required Uri[] GeneratedFiles { get; set; }
    }

    private const string RemoveRazorDynamicFileInfoMethodName = "razor/removeDynamicFileInfo";

    [DataContract]
    private class RemoveDynamicFileParams
    {
        [DataMember(Name = "razorFiles")]
        public required Uri[] RazorFiles { get; set; }
    }

#pragma warning disable CS0067 // We won't fire the Updated event -- we expect Razor to send us textual changes via didChange instead
    public event EventHandler<string>? Updated;
#pragma warning restore CS0067

    private readonly Lazy<RazorWorkspaceListenerInitializer> _razorWorkspaceListenerInitializer;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorDynamicFileInfoProvider(Lazy<RazorWorkspaceListenerInitializer> razorWorkspaceListenerInitializer)
    {
        _razorWorkspaceListenerInitializer = razorWorkspaceListenerInitializer;
    }

    public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        _razorWorkspaceListenerInitializer.Value.NotifyDynamicFile(projectId);

        var requestParams = new ProvideDynamicFileParams { RazorFiles = [ProtocolConversions.CreateAbsoluteUri(filePath)] };

        Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
        var clientLanguageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();

        var response = await clientLanguageServerManager.SendRequestAsync<ProvideDynamicFileParams, ProvideDynamicFileResponse>(
            ProvideRazorDynamicFileInfoMethodName, requestParams, cancellationToken);

        // Since we only sent one file over, we should get either zero or one URI back
        var responseUri = response.GeneratedFiles.SingleOrDefault();

        if (responseUri == null)
        {
            return null;
        }
        else
        {
            var dynamicFileInfoFilePath = ProtocolConversions.GetDocumentFilePathFromUri(responseUri);
            return new DynamicFileInfo(dynamicFileInfoFilePath, SourceCodeKind.Regular, EmptyStringTextLoader.Instance, designTimeOnly: true, documentServiceProvider: null);
        }
    }

    public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        var notificationParams = new RemoveDynamicFileParams { RazorFiles = [ProtocolConversions.CreateAbsoluteUri(filePath)] };

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
