' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Friend Interface ICommitFormatter
        ''' <summary>
        ''' Commits a region by formatting and case correcting it. It is assumed that an ITextUndoTransaction is open
        ''' the underlying text buffer, as multiple edits may be done by this function. Further, if the operation is
        ''' cancelled, the buffer may be left in a partially committed state that must be rolled back by the
        ''' transaction.
        ''' </summary>
        ''' <remarks>
        ''' This method must be called when on the UI thread as it examines the current state of the <see cref="ITextBuffer"/>.
        ''' </remarks>
        Function CommitRegionAsync(
            spanToFormat As SnapshotSpan,
            isExplicitFormat As Boolean,
            useSemantics As Boolean,
            dirtyRegion As SnapshotSpan,
            baseSnapshot As ITextSnapshot,
            baseTree As SyntaxTree,
            blocking As Boolean,
            cancellationToken As CancellationToken) As Task
    End Interface
End Namespace
