' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy
    ' Note: by default, TestWorkspace produces a composition from all assemblies except EditorServicesTest2.
    ' This type has to be defined here until we get that cleaned up. Otherwise, other tests may import it.
    <ExportWorkspaceServiceFactory(GetType(ISymbolNavigationService), ServiceLayer.Host), [Shared]>
    Public Class MockSymbolNavigationServiceProvider
        Implements IWorkspaceServiceFactory

        Private instance As MockSymbolNavigationService = New MockSymbolNavigationService()

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return instance
        End Function

        Friend Class MockSymbolNavigationService
            Implements ISymbolNavigationService

            Public Symbol As ISymbol
            Public Project As Project


            Public Function TryNavigateToSymbol(symbol As ISymbol, project As Project, Optional usePreviewTab As Boolean = False) As Boolean Implements ISymbolNavigationService.TryNavigateToSymbol
                Me.Symbol = symbol
                Me.Project = project
                Return True
            End Function

            Public Function TrySymbolNavigationNotify(symbol As ISymbol, solution As Solution) As Boolean Implements ISymbolNavigationService.TrySymbolNavigationNotify
                Throw New NotImplementedException()
            End Function

            Public Function WouldNavigateToSymbol(symbol As ISymbol, solution As Solution, ByRef filePath As String, ByRef lineNumber As Integer, ByRef charOffset As Integer) As Boolean Implements ISymbolNavigationService.WouldNavigateToSymbol
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
