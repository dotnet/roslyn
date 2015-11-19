' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockDocumentNavigationService
        Implements IDocumentNavigationService

        Public _canNavigateToLineAndOffset As Boolean = True
        Public _canNavigateToPosition As Boolean = True
        Public _canNavigateToSpan As Boolean = True

        Public _triedNavigationToLineAndOffset As Boolean = False
        Public _triedNavigationToPosition As Boolean = False
        Public _triedNavigationToSpan As Boolean = False

        Public _documentId As DocumentId = Nothing
        Public _options As OptionSet = Nothing
        Public _line As Integer = -1
        Public _offset As Integer = -1
        Public _span As TextSpan = Nothing
        Public _position As Integer = -1
        Public _positionVirtualSpace As Integer = -1

        Public Function CanNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer) As Boolean Implements IDocumentNavigationService.CanNavigateToLineAndOffset
            Return _canNavigateToLineAndOffset
        End Function

        Public Function CanNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, Optional virtualSpace As Integer = 0) As Boolean Implements IDocumentNavigationService.CanNavigateToPosition
            Return _canNavigateToPosition
        End Function

        Public Function CanNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan) As Boolean Implements IDocumentNavigationService.CanNavigateToSpan
            Return _canNavigateToSpan
        End Function

        Public Function TryNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, Optional options As OptionSet = Nothing) As Boolean Implements IDocumentNavigationService.TryNavigateToLineAndOffset
            _triedNavigationToLineAndOffset = True
            _documentId = documentId
            _options = options
            _line = lineNumber
            _offset = offset

            Return _canNavigateToLineAndOffset
        End Function

        Public Function TryNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, Optional virtualSpace As Integer = 0, Optional options As OptionSet = Nothing) As Boolean Implements IDocumentNavigationService.TryNavigateToPosition
            _triedNavigationToPosition = True
            _documentId = documentId
            _options = options
            _position = position
            _positionVirtualSpace = virtualSpace

            Return _canNavigateToPosition
        End Function

        Public Function TryNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, Optional options As OptionSet = Nothing) As Boolean Implements IDocumentNavigationService.TryNavigateToSpan
            _triedNavigationToSpan = True
            _documentId = documentId
            _options = options
            _span = textSpan

            Return _canNavigateToSpan
        End Function
    End Class
End Namespace
