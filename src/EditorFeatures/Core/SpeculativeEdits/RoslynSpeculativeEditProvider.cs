// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Internal.Proposals;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.SpeculativeEdits;

// The entire SpeculativeEdit api is marked as obsolete since this is a preview API.  So we do the same here as well.
[Obsolete("This is a preview api and subject to change")]
[Export(typeof(SpeculativeEditProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
internal sealed class RoslynSpeculativeEditProvider : SpeculativeEditProvider
{
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly ITextBufferCloneService _textBufferCloneService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynSpeculativeEditProvider(
        ITextBufferFactoryService3 textBufferFactoryService,
        ITextDocumentFactoryService textDocumentFactoryService,
        ITextBufferCloneService textBufferCloneService)
    {
        this.TextBufferFactoryService = textBufferFactoryService;
        _threadingContext = threadingContext;
        _textDocumentFactoryService = textDocumentFactoryService;
        _textBufferCloneService = textBufferCloneService;
    }

    public override ISpeculativeEditSession? TryStartSpeculativeEditSession(SpeculativeEditOptions options)
    {
        var oldTextSnapshot = options.SourceSnapshot;
        var document = oldTextSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document is null)
            return null;

        var documentId = document.Id;

        // Clone the existing text into a new editor snapshot/buffer that we can fork independently of the original.
        // To do this, we associate a clone of the buffer with a text document with random file path to satisfy
        // extensibility points expecting absolute file path.  We also ensure the new path preserves the same
        // extension as before as that extension is used by LSP to determine the language of the document.
        var clonedTextDocument = _textDocumentFactoryService.CreateTextDocument(
            _textBufferCloneService.Clone(oldTextSnapshot.AsText(), options.DocumentContentType),
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), document.Name));

        // Grab the ITextSnapshot of this cloned buffer before making any changes.  The SpeculativeEdit api needs it
        // as part of the returned set of values.
        var clonedBuffer = clonedTextDocument.TextBuffer;
        var clonedSnapshotBeforeEdits = clonedBuffer.CurrentSnapshot;

        // Now take the cloned buffer and apply the edits to it the caller wants to speculate about.
        ApplyEditsToClonedBuffer(options, clonedBuffer);

        // Now create a forked solution that takes the original document and updates it to point at the current state
        // of the text buffer with the edits applied.  Ensure that this properly updates linked files as well so everything
        // is consistent.
        var newSolution = document.Project.Solution.WithDocumentText(
            document.Project.Solution.GetRelatedDocumentIds(documentId),
            clonedBuffer.AsTextContainer().CurrentText,
            PreservationMode.PreserveIdentity);

        // Now, create a preview workspace with that forked document opened within it so that we can lightup features properly there.
        var previewWorkspace = new PreviewWorkspace(newSolution);
        previewWorkspace.OpenDocument(documentId, clonedBuffer.AsTextContainer());

        // Wrap everything we need into a final ISpeculativeEditSession for the caller.  It owns the lifetime of the data
        // and will dispose it when done. At that point, we can release the allocated preview workspace new 
        return new RoslynSpeculativeEditSession(
            this,
            options,
            clonedSnapshotBeforeEdits,
            previewWorkspace,
            clonedTextDocument);

        static void ApplyEditsToClonedBuffer(SpeculativeEditOptions options, ITextBuffer newBuffer)
        {
            using var bulkEdit = newBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null);
            foreach (var edit in options.Edits)
                bulkEdit.Replace(edit.Span, edit.NewText);

            bulkEdit.Apply();
        }
    }

    private sealed class RoslynSpeculativeEditSession(
        SpeculativeEditOptions options,
        ITextSnapshot clonedSnapshotBeforeEdits,
        PreviewWorkspace previewWorkspace,
        ITextDocument newTextDocument) : ISpeculativeEditSession
    {
        public ITextSnapshot ClonedSnapshot { get; } = clonedSnapshotBeforeEdits;

        public SpeculativeEditOptions CreationOptions { get; } = options;

        public void Dispose()
        {
            previewWorkspace.Dispose();
            newTextDocument.Dispose();
        }
    }
}
