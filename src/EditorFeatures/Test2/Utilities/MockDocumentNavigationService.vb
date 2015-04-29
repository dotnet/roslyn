' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities

    Public Class MockDocumentNavigationService
        Implements IDocumentNavigationService

        Public TriedNavigationToLineAndOffset As Boolean
        Public TriedNavigationToPosition As Boolean
        Public TriedNavigationToSpan As Boolean

        Public ProvidedDocumentId As DocumentId
        Public ProvidedTextSpan As TextSpan
        Public ProvidedLineNumber As Integer
        Public ProvidedOffset As Integer
        Public ProvidedPosition As Integer
        Public ProvidedVirtualSpace As Integer
        Public ProvidedReopenDocument As Boolean
        Public ProvidedUsePreviewTab As Boolean

        Public CanNavigateToLineAndOffsetReturnValue As Boolean = True
        Public CanNavigateToPositionReturnValue As Boolean = True
        Public CanNavigateToSpanReturnValue As Boolean = True

        Public TryNavigateToLineAndOffsetReturnValue As Boolean = True
        Public TryNavigateToPositionReturnValue As Boolean = True
        Public TryNavigateToSpanReturnValue As Boolean = True

        Public Function CanNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer) As Boolean Implements IDocumentNavigationService.CanNavigateToLineAndOffset
            Me.ProvidedDocumentId = documentId
            Me.ProvidedLineNumber = lineNumber

            Return CanNavigateToLineAndOffsetReturnValue
        End Function

        Public Function CanNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, Optional virtualSpace As Integer = 0) As Boolean Implements IDocumentNavigationService.CanNavigateToPosition
            Me.ProvidedDocumentId = documentId
            Me.ProvidedPosition = position
            Me.ProvidedVirtualSpace = virtualSpace

            Return CanNavigateToPositionReturnValue
        End Function

        Public Function CanNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan) As Boolean Implements IDocumentNavigationService.CanNavigateToSpan
            Me.ProvidedDocumentId = documentId
            Me.ProvidedTextSpan = textSpan

            Return CanNavigateToSpanReturnValue
        End Function

        Public Function TryNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, Optional reopenDocument As Boolean = False, Optional usePreviewTab As Boolean = False) As Boolean Implements IDocumentNavigationService.TryNavigateToLineAndOffset
            Me.TriedNavigationToLineAndOffset = True
            Me.ProvidedDocumentId = documentId
            Me.ProvidedLineNumber = lineNumber
            Me.ProvidedOffset = offset
            Me.ProvidedReopenDocument = reopenDocument
            Me.ProvidedUsePreviewTab = usePreviewTab

            Return TryNavigateToLineAndOffsetReturnValue
        End Function

        Public Function TryNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, Optional virtualSpace As Integer = 0, Optional reopenDocument As Boolean = False, Optional usePreviewTab As Boolean = False) As Boolean Implements IDocumentNavigationService.TryNavigateToPosition
            Me.TriedNavigationToPosition = True
            Me.ProvidedDocumentId = documentId
            Me.ProvidedPosition = position
            Me.ProvidedVirtualSpace = virtualSpace
            Me.ProvidedReopenDocument = reopenDocument
            Me.ProvidedUsePreviewTab = usePreviewTab

            Return TryNavigateToPositionReturnValue
        End Function

        Public Function TryNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, Optional reopenDocument As Boolean = False, Optional usePreviewTab As Boolean = False) As Boolean Implements IDocumentNavigationService.TryNavigateToSpan
            Me.TriedNavigationToSpan = True
            Me.ProvidedDocumentId = documentId
            Me.ProvidedTextSpan = textSpan
            Me.ProvidedReopenDocument = reopenDocument
            Me.ProvidedUsePreviewTab = usePreviewTab

            Return TryNavigateToSpanReturnValue
        End Function
    End Class
End Namespace