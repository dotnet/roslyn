' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
    ' Note: by default, TestWorkspace produces a composition from all assemblies except EditorServicesTest2.
    ' This type has to be defined here until we get that cleaned up. Otherwise, other tests may import it.
    <ExportWorkspaceServiceFactory(GetType(ISymbolNavigationService), ServiceLayer.Host), [Shared]>
    Public Class MockSymbolNavigationServiceProvider
        Implements IWorkspaceServiceFactory

        Private _instance As MockSymbolNavigationService = New MockSymbolNavigationService()

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return _instance
        End Function

        Friend Class MockSymbolNavigationService
            Implements ISymbolNavigationService

            Public TryNavigateToSymbolProvidedSymbol As ISymbol
            Public TryNavigateToSymbolProvidedProject As Project
            Public TryNavigateToSymbolProvidedOptions As OptionSet

            Public TrySymbolNavigationNotifyProvidedSymbol As ISymbol
            Public TrySymbolNavigationNotifyProvidedSolution As Solution
            Public TrySymbolNavigationNotifyReturnValue As Boolean = False

            Public WouldNavigateToSymbolProvidedSymbol As ISymbol
            Public WouldNavigateToSymbolProvidedSolution As Solution
            Public WouldNavigateToSymbolReturnValue As Boolean = False
            Public NavigationFilePathReturnValue As String = String.Empty
            Public NavigationLineNumberReturnValue As Integer = 0
            Public NavigationCharOffsetReturnValue As Integer = 0

            Public Function TryNavigateToSymbol(symbol As ISymbol, project As Project, Optional options As OptionSet = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Boolean Implements ISymbolNavigationService.TryNavigateToSymbol
                Me.TryNavigateToSymbolProvidedSymbol = symbol
                Me.TryNavigateToSymbolProvidedProject = project
                Me.TryNavigateToSymbolProvidedOptions = options
                Return True
            End Function

            Public Function TrySymbolNavigationNotify(symbol As ISymbol, solution As Solution) As Boolean Implements ISymbolNavigationService.TrySymbolNavigationNotify
                Me.TrySymbolNavigationNotifyProvidedSymbol = symbol
                Me.TrySymbolNavigationNotifyProvidedSolution = solution

                Return TrySymbolNavigationNotifyReturnValue
            End Function

            Public Function WouldNavigateToSymbol(symbol As ISymbol, solution As Solution, ByRef filePath As String, ByRef lineNumber As Integer, ByRef charOffset As Integer) As Boolean Implements ISymbolNavigationService.WouldNavigateToSymbol
                Me.WouldNavigateToSymbolProvidedSymbol = symbol
                Me.WouldNavigateToSymbolProvidedSolution = solution

                filePath = Me.NavigationFilePathReturnValue
                lineNumber = Me.NavigationLineNumberReturnValue
                charOffset = Me.NavigationCharOffsetReturnValue

                Return WouldNavigateToSymbolReturnValue
            End Function
        End Class
    End Class
End Namespace
