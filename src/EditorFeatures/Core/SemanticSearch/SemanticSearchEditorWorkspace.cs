// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class SemanticSearchEditorWorkspace(
    HostServices services,
    SemanticSearchProjectConfiguration config,
    IThreadingContext threadingContext,
    IAsynchronousOperationListenerProvider listenerProvider)
    : SemanticSearchWorkspace(services, config)
{
    private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.SemanticSearch);

    private ITextBuffer? _queryTextBuffer;
    private DocumentId? _queryDocumentId;

    public async Task OpenQueryDocumentAsync(ITextBuffer buffer, CancellationToken cancellationToken)
    {
        _queryTextBuffer = buffer;

        // initialize solution with default query, unless it has already been initialized:
        var queryDocument = await UpdateQueryDocumentAsync(query: null, cancellationToken).ConfigureAwait(false);

        _queryDocumentId = queryDocument.Id;

        OnDocumentOpened(queryDocument.Id, buffer.AsTextContainer());
    }

    /// <summary>
    /// Used by code actions through <see cref="Workspace.TryApplyChanges(Solution)"/>.
    /// </summary>
    protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
    {
        if (documentId == _queryDocumentId)
        {
            ApplyQueryDocumentTextChanged(newText);
        }
    }

    protected override void ApplyQueryDocumentTextChanged(SourceText newText)
    {
        Contract.ThrowIfNull(_queryTextBuffer);

        // update the buffer on UI thread:

        var completionToken = _asyncListener.BeginAsyncOperation(nameof(SemanticSearchEditorWorkspace) + "." + nameof(ApplyQueryDocumentTextChanged));
        _ = UpdateTextAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(completionToken);

        async Task UpdateTextAsync()
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
            TextEditApplication.UpdateText(newText, _queryTextBuffer, EditOptions.DefaultMinimalChange);
        }
    }
}
