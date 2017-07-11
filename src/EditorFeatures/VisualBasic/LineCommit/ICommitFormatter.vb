' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Friend Interface ICommitFormatter
        ''' <summary>
        ''' Commits a region by formatting and case correcting it. It is assumed that an
        ''' ITextUndoTransaction is open the underlying text buffer, as multiple edits may be done
        ''' by this function. Further, if the operation is cancelled, the buffer may be left in a
        ''' partially committed state that must be rolled back by the transaction.
        ''' </summary>
        Sub CommitRegion(
            spanToFormat As SnapshotSpan,
            isExplicitFormat As Boolean,
            useSemantics As Boolean,
            dirtyRegion As SnapshotSpan,
            baseSnapshot As ITextSnapshot,
            baseTree As SyntaxTree,
            cancellationToken As CancellationToken)
    End Interface
End Namespace
