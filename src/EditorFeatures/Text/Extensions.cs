// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Text;

public static partial class Extensions
{
    extension(ITextBuffer buffer)
    {
        public SourceTextContainer AsTextContainer()
        => TextBufferContainer.From(buffer);

        /// <summary>
        /// Gets the workspace corresponding to the text buffer.
        /// </summary>
        public Workspace? GetWorkspace()
        {
            var container = buffer.AsTextContainer();
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                return workspace;
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="Document"/>s from the corresponding <see cref="Workspace.CurrentSolution"/> that are associated with the <see cref="ITextBuffer"/>.
        /// There may be multiple <see cref="Document"/>s associated with the buffer if it is linked into multiple projects or is part of a Shared Project. 
        /// </summary>
        public IEnumerable<Document> GetRelatedDocuments()
            => buffer.AsTextContainer().GetRelatedDocuments();

        internal bool CanApplyChangeDocumentToWorkspace()
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out var workspace) &&
               workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);
    }

    extension(SourceTextContainer textContainer)
    {
        public ITextBuffer GetTextBuffer()
        => TryGetTextBuffer(textContainer) ?? throw new ArgumentException(TextEditorResources.textContainer_is_not_a_SourceTextContainer_that_was_created_from_an_ITextBuffer, nameof(textContainer));
    }

    extension(SourceTextContainer? textContainer)
    {
        public ITextBuffer? TryGetTextBuffer()
        => (textContainer as TextBufferContainer)?.TryFindEditorTextBuffer();
    }

    extension(SourceText? text)
    {
        /// <summary>
        /// Returns the <see cref="ITextSnapshot"/> behind this <see cref="SourceText"/>, or null if it wasn't created from one.
        /// 
        /// Note that multiple <see cref="ITextSnapshot"/>s may map to the same <see cref="SourceText"/> instance if it's
        /// <see cref="ITextVersion.ReiteratedVersionNumber" /> doesn't change.
        /// </summary>
        /// <returns>The underlying ITextSnapshot.</returns>
        public ITextSnapshot? FindCorrespondingEditorTextSnapshot()
            => (text as SnapshotSourceText)?.TryFindEditorSnapshot();

        internal ITextImage? TryFindCorrespondingEditorTextImage()
            => (text as SnapshotSourceText)?.TextImage;
    }

    extension(ITextSnapshotLine line)
    {
        internal TextLine AsTextLine()
        => line.Snapshot.AsText().Lines[line.LineNumber];
    }

    extension(ITextSnapshot textSnapshot)
    {
        public SourceText AsText()
        {
            textSnapshot.TextBuffer.Properties.TryGetProperty<ITextBufferCloneService>(typeof(ITextBufferCloneService), out var textBufferCloneServiceOpt);
            return SnapshotSourceText.From(textBufferCloneServiceOpt, textSnapshot);
        }

        internal SourceText AsRoslynText(ITextBufferCloneService textBufferCloneServiceOpt, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm)
            => new SnapshotSourceText.ClosedSnapshotSourceText(textBufferCloneServiceOpt, ((ITextSnapshot2)textSnapshot).TextImage, encoding, checksumAlgorithm);
    }

    extension(ITextSnapshot text)
    {
        /// <summary>
        /// Gets the <see cref="Document"/>s from the corresponding <see cref="Workspace.CurrentSolution"/> that are associated with the <see cref="ITextSnapshot"/>'s buffer,
        /// updated to contain the same text as the snapshot if necessary. There may be multiple <see cref="Document"/>s associated with the buffer
        /// if the file is linked into multiple projects or is part of a Shared Project.
        /// </summary>
        public IEnumerable<Document> GetRelatedDocumentsWithChanges()
            => text.AsText().GetRelatedDocumentsWithChanges();

        /// <summary>
        /// Gets the <see cref="Document"/> from the corresponding <see cref="Workspace.CurrentSolution"/> that is associated with the <see cref="ITextSnapshot"/>'s buffer
        /// in its current project context, updated to contain the same text as the snapshot if necessary. There may be multiple <see cref="Document"/>s
        /// associated with the buffer if it is linked into multiple projects or is part of a Shared Project. In this case, the <see cref="Workspace"/>
        /// is responsible for keeping track of which of these <see cref="Document"/>s is in the current project context.
        /// </summary>
        public Document? GetOpenDocumentInCurrentContextWithChanges()
            => text.AsText().GetOpenDocumentInCurrentContextWithChanges();

        internal TextDocument? GetOpenTextDocumentInCurrentContextWithChanges()
            => text.AsText().GetOpenTextDocumentInCurrentContextWithChanges();
    }

    extension(ITextBuffer textBuffer)
    {
        /// <summary>
        /// Get the encoding used to load this <see cref="ITextBuffer"/> if possible.
        /// <para>
        /// Note that this will return <see cref="Encoding.UTF8"/> if the <see cref="ITextBuffer"/>
        /// didn't come from an <see cref="ITextDocument"/>, or if the <see cref="ITextDocument"/>
        /// is already closed.
        /// </para>
        /// </summary>
        internal Encoding GetEncodingOrUTF8()
            => textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument)
                ? textDocument.Encoding
                : Encoding.UTF8;
    }
}
