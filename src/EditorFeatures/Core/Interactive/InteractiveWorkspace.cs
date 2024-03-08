// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive;

internal partial class InteractiveWorkspace : Workspace
{
    private SourceTextContainer? _openTextContainer;
    private DocumentId? _openDocumentId;

    internal InteractiveWorkspace(HostServices hostServices)
        : base(hostServices, WorkspaceKind.Interactive)
    {
    }

    public override bool CanOpenDocuments
        => true;

    public override bool CanApplyChange(ApplyChangesKind feature)
        => feature == ApplyChangesKind.ChangeDocument;

    public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
    {
        _openTextContainer = textContainer;
        _openDocumentId = documentId;
        OnDocumentOpened(documentId, textContainer);
    }

    protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
    {
        if (_openDocumentId != document)
        {
            return;
        }

        Contract.ThrowIfNull(_openTextContainer);

        ITextSnapshot appliedText;
        using (var edit = _openTextContainer.GetTextBuffer().CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
        {
            var oldText = _openTextContainer.CurrentText;
            var changes = newText.GetTextChanges(oldText);

            foreach (var change in changes)
            {
                edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
            }

            appliedText = edit.Apply();
        }

        OnDocumentTextChanged(document, appliedText.AsText(), PreservationMode.PreserveIdentity);
    }

    /// <summary>
    /// Closes all open documents and empties the solution but keeps all solution-level analyzers.
    /// </summary>
    public void ResetSolution()
    {
        ClearOpenDocuments();

        var emptySolution = CreateSolution(SolutionId.CreateNewId("InteractiveSolution"));
        SetCurrentSolution(solution => emptySolution.WithAnalyzerReferences(solution.AnalyzerReferences), WorkspaceChangeKind.SolutionCleared);
    }
}
