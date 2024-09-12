' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
    ' Note: by default, TestWorkspace produces a composition from all assemblies except EditorServicesTest2.
    ' This type has to be defined here until we get that cleaned up. Otherwise, other tests may import it.
    <ExportWorkspaceServiceFactory(GetType(IDocumentNavigationService), ServiceLayer.Host), [Shared]>
    Public Class MockDocumentNavigationServiceProvider
        Implements IWorkspaceServiceFactory

        Private ReadOnly _instance As MockDocumentNavigationService = New MockDocumentNavigationService()

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return _instance
        End Function

        Friend Class MockDocumentNavigationService
            Implements IDocumentNavigationService

            Public ProvidedDocumentId As DocumentId
            Public ProvidedTextSpan As TextSpan
            Public ProvidedPosition As Integer
            Public ProvidedVirtualSpace As Integer

            Public CanNavigateToPositionReturnValue As Boolean = True
            Public CanNavigateToSpanReturnValue As Boolean = True

            Public TryNavigateToPositionReturnValue As Boolean = True
            Public TryNavigateToSpanReturnValue As Boolean = True

            Public Function CanNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToPositionAsync
                Me.ProvidedDocumentId = documentId
                Me.ProvidedPosition = position
                Me.ProvidedVirtualSpace = virtualSpace

                Return If(CanNavigateToPositionReturnValue, SpecializedTasks.True, SpecializedTasks.False)
            End Function

            Public Function CanNavigateToSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, allowInvalidSpan As Boolean, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDocumentNavigationService.CanNavigateToSpanAsync
                Me.ProvidedDocumentId = documentId
                Me.ProvidedTextSpan = textSpan

                Return If(CanNavigateToSpanReturnValue, SpecializedTasks.True, SpecializedTasks.False)
            End Function

            Public Function GetLocationForPositionAsync(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements IDocumentNavigationService.GetLocationForPositionAsync
                Me.ProvidedDocumentId = documentId
                Me.ProvidedPosition = position
                Me.ProvidedVirtualSpace = virtualSpace

                Return NavigableLocation.TestAccessor.Create(TryNavigateToPositionReturnValue)
            End Function

            Public Function GetLocationForSpanAsync(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, allowInvalidSpans As Boolean, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements IDocumentNavigationService.GetLocationForSpanAsync
                Me.ProvidedDocumentId = documentId
                Me.ProvidedTextSpan = textSpan

                Return NavigableLocation.TestAccessor.Create(TryNavigateToSpanReturnValue)
            End Function
        End Class
    End Class
End Namespace
