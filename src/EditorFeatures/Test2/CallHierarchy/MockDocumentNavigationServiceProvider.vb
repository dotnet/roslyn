' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy
    ' Note: by default, TestWorkspace produces a composition from all assemblies except EditorServicesTest2.
    ' This type has to be defined here until we get that cleaned up. Otherwise, other tests may import it.
    <ExportWorkspaceServiceFactory(GetType(IDocumentNavigationService), ServiceLayer.Host), [Shared]>
    Public Class MockDocumentNavigationServiceProvider
        Implements IWorkspaceServiceFactory

        Private instance As MockDocumentNavigationService = New MockDocumentNavigationService()

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return instance
        End Function

        Friend Class MockDocumentNavigationService
            Implements IDocumentNavigationService

            Public DocumentId As DocumentId
            Public TextSpan As TextSpan

            Public Function CanNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer) As Boolean Implements IDocumentNavigationService.CanNavigateToLineAndOffset
                Throw New NotImplementedException()
            End Function

            Public Function CanNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, Optional virtualSpace As Integer = 0) As Boolean Implements IDocumentNavigationService.CanNavigateToPosition
                Throw New NotImplementedException()
            End Function

            Public Function CanNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan) As Boolean Implements IDocumentNavigationService.CanNavigateToSpan
                Throw New NotImplementedException()
            End Function

            Public Function TryNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, Optional usePreviewTab As Boolean = False) As Boolean Implements IDocumentNavigationService.TryNavigateToLineAndOffset
                Throw New NotImplementedException()
            End Function

            Public Function TryNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, Optional virtualSpace As Integer = 0, Optional usePreviewTab As Boolean = False) As Boolean Implements IDocumentNavigationService.TryNavigateToPosition
                Throw New NotImplementedException()
            End Function

            Public Function TryNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, Optional usePreviewTab As Boolean = False) As Boolean Implements IDocumentNavigationService.TryNavigateToSpan
                Me.DocumentId = documentId
                Me.TextSpan = textSpan
                Return True
            End Function
        End Class
    End Class
End Namespace
