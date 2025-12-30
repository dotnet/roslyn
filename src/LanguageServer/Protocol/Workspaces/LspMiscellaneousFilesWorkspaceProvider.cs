// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Defines a default workspace for opened LSP files that are not found in any
/// workspace registered by the <see cref="LspWorkspaceRegistrationService"/>.
/// If a document added here is subsequently found in a registered workspace, 
/// the document is removed from this workspace.
/// 
/// Future work for this workspace includes supporting basic metadata references (mscorlib, System dlls, etc),
/// but that is dependent on having a x-plat mechanism for retrieving those references from the framework / sdk.
/// </summary>
internal sealed class LspMiscellaneousFilesWorkspaceProvider(ILspServices lspServices, HostServices hostServices)
    : Workspace(hostServices, WorkspaceKind.MiscellaneousFiles), ILspMiscellaneousFilesWorkspaceProvider, ILspWorkspace
{
    public bool SupportsMutation => true;

    public ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument document, CancellationToken cancellationToken)
    {
        // In this case, the only documents ever created live in the Miscellaneous Files workspace (which is this object directly), so we can just compare to 'this'.
        return ValueTask.FromResult(document.Project.Solution.Workspace == this);
    }

    /// <summary>
    /// Takes in a file URI and text and creates a misc project and document for the file.
    /// 
    /// Calls to this method and <see cref="TryRemoveMiscellaneousDocumentAsync(DocumentUri)"/> are made
    /// from LSP text sync request handling which do not run concurrently.
    /// </summary>
    public ValueTask<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
        => ValueTask.FromResult(AddMiscellaneousDocument(uri, documentText, languageId, logger));

    private TextDocument? AddMiscellaneousDocument(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentFilePath = uri.UriString;
        if (uri.ParsedUri is not null)
        {
            documentFilePath = ProtocolConversions.GetDocumentFilePathFromUri(uri.ParsedUri);
        }

        var languageInfoProvider = lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
        {
            // Only log here since throwing here could take down the LSP server.
            logger.LogError($"Could not find language information for {uri} with absolute path {documentFilePath}");
            return null;
        }

        var sourceTextLoader = new SourceTextLoader(documentText, documentFilePath);

        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            this, documentFilePath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, Services.SolutionServices, []);
        OnProjectAdded(projectInfo);

        if (languageInformation.LanguageName == "Razor")
        {
            var docId = projectInfo.AdditionalDocuments.Single().Id;
            return CurrentSolution.GetRequiredAdditionalDocument(docId);
        }

        var id = projectInfo.Documents.Single().Id;
        return CurrentSolution.GetRequiredDocument(id);
    }

    /// <summary>
    /// Removes a document with the matching file path from this workspace.
    /// 
    /// Calls to this method and <see cref="AddMiscellaneousDocument(DocumentUri, SourceText, string, ILspLogger)"/> are made
    /// from LSP text sync request handling which do not run concurrently.
    /// </summary>
    public ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
    {
        // We'll only ever have a single document matching this URI in the misc solution.
        var matchingDocument = CurrentSolution.GetDocumentIds(uri).SingleOrDefault();
        if (matchingDocument != null)
        {
            if (CurrentSolution.ContainsDocument(matchingDocument))
            {
                OnDocumentRemoved(matchingDocument);
            }
            else if (CurrentSolution.ContainsAdditionalDocument(matchingDocument))
            {
                OnAdditionalDocumentRemoved(matchingDocument);
            }

            // Also remove the project - we always create a new project for each misc file we add
            // so it should never have other documents in it.
            var project = CurrentSolution.GetRequiredProject(matchingDocument.ProjectId);
            OnProjectRemoved(project.Id);

            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public ValueTask UpdateTextIfPresentAsync(DocumentId documentId, SourceText sourceText, CancellationToken cancellationToken)
    {
        this.OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity, requireDocumentPresent: false);
        return ValueTask.CompletedTask;
    }

    private sealed class StaticSourceTextContainer(SourceText text) : SourceTextContainer
    {
        public override SourceText CurrentText => text;

        /// <summary>
        /// Text changes are handled by LSP forking the document, we don't need to actually update anything here.
        /// </summary>
        public override event EventHandler<TextChangeEventArgs> TextChanged
        {
            add { }
            remove { }
        }
    }
}
