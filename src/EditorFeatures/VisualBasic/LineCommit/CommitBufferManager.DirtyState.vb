' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Partial Friend Class CommitBufferManager
        Private Class DirtyState
            Public Sub New(span As SnapshotSpan, baseSnapshot As ITextSnapshot, baseDocument As Document)
                Contract.ThrowIfNull(baseDocument)
                Contract.ThrowIfNull(baseSnapshot)

                DirtyRegion = span.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive)
                Me.BaseSnapshot = baseSnapshot
                Me.BaseDocument = baseDocument
            End Sub

            Public Function WithExpandedDirtySpan(includeSpan As SnapshotSpan) As DirtyState
                Dim oldDirtyRegion = DirtyRegion.GetSpan(includeSpan.Snapshot)
                Dim newDirtyRegion = Span.FromBounds(Math.Min(oldDirtyRegion.Start, includeSpan.Start),
                                                     Math.Max(oldDirtyRegion.End, includeSpan.End))
                Return New DirtyState(New SnapshotSpan(includeSpan.Snapshot, newDirtyRegion),
                                      BaseSnapshot,
                                      BaseDocument)
            End Function

            Public ReadOnly Property DirtyRegion As ITrackingSpan

            Public ReadOnly Property BaseSnapshot As ITextSnapshot

            Public ReadOnly Property BaseDocument As Document
        End Class
    End Class
End Namespace
