' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockDocumentNavigationService
        Implements IDocumentNavigationService

        Public _canNavigateToPosition As Boolean = True
        Public _canNavigateToSpan As Boolean = True

        Public _triedNavigationToPosition As Boolean
        Public _triedNavigationToSpan As Boolean

        Public _documentId As DocumentId
        Public _line As Integer = -1
        Public _offset As Integer = -1
        Public _span As TextSpan = Nothing
        Public _position As Integer = -1
        Public _positionVirtualSpace As Integer = -1

        Public Function CanNavigateToPositionAsync(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToPositionAsync
            Return If(_canNavigateToPosition, SpecializedTasks.True, SpecializedTasks.False)
        End Function

        Public Function CanNavigateToSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, allowInvalidSpan As Boolean, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToSpanAsync
            Return If(_canNavigateToSpan, SpecializedTasks.True, SpecializedTasks.False)
        End Function

        Public Function GetLocationForPositionAsync(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements IDocumentNavigationService.GetLocationForPositionAsync
            Return Task.FromResult(Of INavigableLocation)(New NavigableLocation(
                Function(o, c)
                    _triedNavigationToPosition = True
                    _documentId = documentId
                    _position = position
                    _positionVirtualSpace = virtualSpace
                    Return SpecializedTasks.True
                End Function))
        End Function

        Public Function GetLocationForSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, allowInvalidSpan As Boolean, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements IDocumentNavigationService.GetLocationForSpanAsync
            Return Task.FromResult(Of INavigableLocation)(New NavigableLocation(
                Function(o, c)
                    _triedNavigationToSpan = True
                    _documentId = documentId
                    _span = textSpan
                    Return SpecializedTasks.True
                End Function))
        End Function
    End Class
End Namespace
