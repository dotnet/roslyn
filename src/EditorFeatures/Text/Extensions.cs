// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Text
{
    public static partial class Extensions
    {
        public static SourceTextContainer AsTextContainer(this ITextBuffer buffer)
            => TextBufferContainer.From(buffer);

        public static ITextBuffer GetTextBuffer(this SourceTextContainer textContainer)
            => TryGetTextBuffer(textContainer) ?? throw new ArgumentException(TextEditorResources.textContainer_is_not_a_SourceTextContainer_that_was_created_from_an_ITextBuffer, nameof(textContainer));

        public static ITextBuffer? TryGetTextBuffer(this SourceTextContainer? textContainer)
            => (textContainer as TextBufferContainer)?.TryFindEditorTextBuffer();

        /// <summary>
        /// Returns the <see cref="ITextSnapshot"/> behind this <see cref="SourceText"/>, or null if it wasn't created from one.
        /// 
        /// Note that multiple <see cref="ITextSnapshot"/>s may map to the same <see cref="SourceText"/> instance if it's
        /// <see cref="ITextVersion.ReiteratedVersionNumber" /> doesn't change.
        /// </summary>
        /// <returns>The underlying ITextSnapshot.</returns>
        public static ITextSnapshot? FindCorrespondingEditorTextSnapshot(this SourceText? text)
            => (text as SnapshotSourceText)?.TryFindEditorSnapshot();

        internal static ITextImage? TryFindCorrespondingEditorTextImage(this SourceText? text)
            => (text as SnapshotSourceText)?.TextImage;

        internal static TextLine AsTextLine(this ITextSnapshotLine line)
            => line.Snapshot.AsText().Lines[line.LineNumber];

        public static SourceText AsText(this ITextSnapshot textSnapshot)
        {
            textSnapshot.TextBuffer.Properties.TryGetProperty<ITextBufferCloneService>(typeof(ITextBufferCloneService), out var textBufferCloneServiceOpt);
            return SnapshotSourceText.From(textBufferCloneServiceOpt, textSnapshot);
        }

        internal static SourceText AsRoslynText(this ITextSnapshot textSnapshot, ITextBufferCloneService textBufferCloneServiceOpt, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm)
            => new SnapshotSourceText.ClosedSnapshotSourceText(textBufferCloneServiceOpt, ((ITextSnapshot2)textSnapshot).TextImage, encoding, checksumAlgorithm);

        /// <summary>
        /// Gets the workspace corresponding to the text buffer.
        /// </summary>
        public static Workspace? GetWorkspace(this ITextBuffer buffer)
        {
            var container = buffer.AsTextContainer();
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                return workspace;
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="Document"/>s from the corresponding <see cref="Workspace.CurrentSolution"/> that are associated with the <see cref="ITextSnapshot"/>'s buffer,
        /// updated to contain the same text as the snapshot if necessary. There may be multiple <see cref="Document"/>s associated with the buffer
        /// if the file is linked into multiple projects or is part of a Shared Project.
        /// </summary>
        public static IEnumerable<Document> GetRelatedDocumentsWithChanges(this ITextSnapshot text)
            => text.AsText().GetRelatedDocumentsWithChanges();

        /// <summary>
        /// Gets the <see cref="Document"/> from the corresponding <see cref="Workspace.CurrentSolution"/> that is associated with the <see cref="ITextSnapshot"/>'s buffer
        /// in its current project context, updated to contain the same text as the snapshot if necessary. There may be multiple <see cref="Document"/>s
        /// associated with the buffer if it is linked into multiple projects or is part of a Shared Project. In this case, the <see cref="Workspace"/>
        /// is responsible for keeping track of which of these <see cref="Document"/>s is in the current project context.
        /// </summary>
        public static Document? GetOpenDocumentInCurrentContextWithChanges(this ITextSnapshot text)
            => text.AsText().GetOpenDocumentInCurrentContextWithChanges();

        internal static TextDocument? GetOpenTextDocumentInCurrentContextWithChanges(this ITextSnapshot text)
            => text.AsText().GetOpenTextDocumentInCurrentContextWithChanges();

        /// <summary>
        /// Gets the <see cref="Document"/>s from the corresponding <see cref="Workspace.CurrentSolution"/> that are associated with the <see cref="ITextBuffer"/>.
        /// There may be multiple <see cref="Document"/>s associated with the buffer if it is linked into multiple projects or is part of a Shared Project. 
        /// </summary>
        public static IEnumerable<Document> GetRelatedDocuments(this ITextBuffer buffer)
            => buffer.AsTextContainer().GetRelatedDocuments();

        internal static bool CanApplyChangeDocumentToWorkspace(this ITextBuffer buffer)
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out var workspace) &&
               workspace.CanApplyChange(ApplyChangesKind.ChangeDocument);

        /// <summary>
        /// Get the encoding used to load this <see cref="ITextBuffer"/> if possible.
        /// <para>
        /// Note that this will return <see cref="Encoding.UTF8"/> if the <see cref="ITextBuffer"/>
        /// didn't come from an <see cref="ITextDocument"/>, or if the <see cref="ITextDocument"/>
        /// is already closed.
        /// </para>
        /// </summary>
        internal static Encoding GetEncodingOrUTF8(this ITextBuffer textBuffer)
            => textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument)
                ? textDocument.Encoding
                : Encoding.UTF8;
    }
}
