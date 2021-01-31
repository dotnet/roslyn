﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

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
            Public ProvidedLineNumber As Integer
            Public ProvidedOffset As Integer
            Public ProvidedPosition As Integer
            Public ProvidedVirtualSpace As Integer
            Public ProvidedOptions As OptionSet

            Public CanNavigateToLineAndOffsetReturnValue As Boolean = True
            Public CanNavigateToPositionReturnValue As Boolean = True
            Public CanNavigateToSpanReturnValue As Boolean = True

            Public TryNavigateToLineAndOffsetReturnValue As Boolean = True
            Public TryNavigateToPositionReturnValue As Boolean = True
            Public TryNavigateToSpanReturnValue As Boolean = True

            Public Function CanNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.CanNavigateToLineAndOffset
                Me.ProvidedDocumentId = documentId
                Me.ProvidedLineNumber = lineNumber

                Return CanNavigateToLineAndOffsetReturnValue
            End Function

            Public Function CanNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.CanNavigateToPosition
                Me.ProvidedDocumentId = documentId
                Me.ProvidedPosition = position
                Me.ProvidedVirtualSpace = virtualSpace

                Return CanNavigateToPositionReturnValue
            End Function

            Public Function CanNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.CanNavigateToSpan
                Me.ProvidedDocumentId = documentId
                Me.ProvidedTextSpan = textSpan

                Return CanNavigateToSpanReturnValue
            End Function

            Public Function TryNavigateToLineAndOffset(workspace As Workspace, documentId As DocumentId, lineNumber As Integer, offset As Integer, options As OptionSet, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.TryNavigateToLineAndOffset
                Me.ProvidedDocumentId = documentId
                Me.ProvidedLineNumber = lineNumber
                Me.ProvidedOffset = offset
                Me.ProvidedOptions = options

                Return TryNavigateToLineAndOffsetReturnValue
            End Function

            Public Function TryNavigateToPosition(workspace As Workspace, documentId As DocumentId, position As Integer, virtualSpace As Integer, options As OptionSet, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.TryNavigateToPosition
                Me.ProvidedDocumentId = documentId
                Me.ProvidedPosition = position
                Me.ProvidedVirtualSpace = virtualSpace
                Me.ProvidedOptions = options

                Return TryNavigateToPositionReturnValue
            End Function

            Public Function TryNavigateToSpan(workspace As Workspace, documentId As DocumentId, textSpan As TextSpan, options As OptionSet, cancellationToken As CancellationToken) As Boolean Implements IDocumentNavigationService.TryNavigateToSpan
                Me.ProvidedDocumentId = documentId
                Me.ProvidedTextSpan = textSpan
                Me.ProvidedOptions = options

                Return TryNavigateToSpanReturnValue
            End Function
        End Class
    End Class
End Namespace
