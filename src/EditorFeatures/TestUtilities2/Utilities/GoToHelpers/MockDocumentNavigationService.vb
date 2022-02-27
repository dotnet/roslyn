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

        Public _canNavigateToLineAndOffset As Boolean = True
        Public _canNavigateToPosition As Boolean = True
        Public _canNavigateToSpan As Boolean = True

        Public _triedNavigationToLineAndOffset As Boolean
        Public _triedNavigationToPosition As Boolean
        Public _triedNavigationToSpan As Boolean

        Public _documentId As DocumentId
        Public _options As NavigationOptions
        Public _line As Integer = -1
        Public _offset As Integer = -1
        Public _span As TextSpan = Nothing
        Public _position As Integer = -1
        Public _positionVirtualSpace As Integer = -1

        Public Function CanNavigateToLineAndOffsetAsync(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToLineAndOffsetAsync
            Return If(_canNavigateToLineAndOffset, SpecializedTasks.True, SpecializedTasks.False)
        End Function

        Public Function CanNavigateToPositionAsync(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToPositionAsync
            Return If(_canNavigateToPosition, SpecializedTasks.True, SpecializedTasks.False)
        End Function

        Public Function CanNavigateToSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToSpanAsync
            Return If(_canNavigateToSpan, SpecializedTasks.True, SpecializedTasks.False)
        End Function

        Public Function GetNavigableLocationForLineAndOffsetAsync(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, options As NavigationOptions, cancellationToken As CancellationToken) As Task(Of INavigableDocumentLocation) Implements IDocumentNavigationService.GetNavigableLocationForLineAndOffsetAsync
            _triedNavigationToLineAndOffset = True
            _documentId = documentId
            _options = options
            _line = lineNumber
            _offset = offset

            Return NavigableDocumentLocation.TestAccessor.Create(_canNavigateToLineAndOffset)
        End Function

        Public Function GetNavigableLocationForPositionAsync(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, options As NavigationOptions, cancellationToken As CancellationToken) As Task(Of INavigableDocumentLocation) Implements IDocumentNavigationService.GetNavigableLocationForPositionAsync
            _triedNavigationToPosition = True
            _documentId = documentId
            _options = options
            _position = position
            _positionVirtualSpace = virtualSpace

<<<<<<< HEAD
            Return _canNavigateToPosition
        End Function

        Public Function TryNavigateToPositionAsync(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, options As NavigationOptions, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.TryNavigateToPositionAsync
            Return If(TryNavigateToPosition(workspace, documentId, position, virtualSpace, options, cancellationToken), SpecializedTasks.True, SpecializedTasks.False)
        End Function

        Public Function TryNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, options As NavigationOptions, allowInvalidSpan As Boolean, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.TryNavigateToSpan
            _triedNavigationToSpan = True
            _documentId = documentId
            _options = options
            _span = textSpan

            Return _canNavigateToSpan
        End Function

        Public Function TryNavigateToSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, options As NavigationOptions, allowInvalidSpan As Boolean, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.TryNavigateToSpanAsync
            Return If(TryNavigateToSpan(workspace, documentId, textSpan, options, allowInvalidSpan, cancellationToken), SpecializedTasks.True, SpecializedTasks.False)
=======
            Return NavigableDocumentLocation.TestAccessor.Create(_canNavigateToPosition)
        End Function

        Public Function GetNavigableLocationForSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, options As NavigationOptions, allowInvalidSpan As Boolean, cancellationToken As CancellationToken) As Task(Of INavigableDocumentLocation) Implements IDocumentNavigationService.GetNavigableLocationForSpanAsync
            _triedNavigationToSpan = True
            _documentId = documentId
            _options = options
            _span = textSpan

            Return NavigableDocumentLocation.TestAccessor.Create(_canNavigateToSpan)
>>>>>>> asyncNavigation2
        End Function
    End Class
End Namespace
