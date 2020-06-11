' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Partial Friend Class CommitBufferManager
        Private Class DirtyState
            Private ReadOnly _dirtyRegion As ITrackingSpan
            Private ReadOnly _baseSnapshot As ITextSnapshot
            Private ReadOnly _baseDocument As Document

            Public Sub New(span As SnapshotSpan, baseSnapshot As ITextSnapshot, baseDocument As Document)
                Contract.ThrowIfNull(baseDocument)
                Contract.ThrowIfNull(baseSnapshot)

                _dirtyRegion = span.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive)
                _baseSnapshot = baseSnapshot
                _baseDocument = baseDocument
            End Sub

            Public Function WithExpandedDirtySpan(includeSpan As SnapshotSpan) As DirtyState
                Dim oldDirtyRegion = _dirtyRegion.GetSpan(includeSpan.Snapshot)
                Dim newDirtyRegion = Span.FromBounds(Math.Min(oldDirtyRegion.Start, includeSpan.Start),
                                                     Math.Max(oldDirtyRegion.End, includeSpan.End))
                Return New DirtyState(New SnapshotSpan(includeSpan.Snapshot, newDirtyRegion),
                                      _baseSnapshot,
                                      _baseDocument)
            End Function

            Public ReadOnly Property DirtyRegion As ITrackingSpan
                Get
                    Return _dirtyRegion
                End Get
            End Property

            Public ReadOnly Property BaseSnapshot As ITextSnapshot
                Get
                    Return _baseSnapshot
                End Get
            End Property

            Public ReadOnly Property BaseDocument As Document
                Get
                    Return _baseDocument
                End Get
            End Property
        End Class
    End Class
End Namespace
