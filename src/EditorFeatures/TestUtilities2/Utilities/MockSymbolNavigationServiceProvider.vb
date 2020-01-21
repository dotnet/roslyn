' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.FindUsages
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

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return _instance
        End Function

        Friend Class MockSymbolNavigationService
            Implements ISymbolNavigationService

            Public TryNavigateToSymbolProvidedSymbol As ISymbol
            Public TryNavigateToSymbolProvidedProject As Project
            Public TryNavigateToSymbolProvidedOptions As OptionSet

            Public TrySymbolNavigationNotifyProvidedSymbol As ISymbol
            Public TrySymbolNavigationNotifyProvidedProject As Project
            Public TrySymbolNavigationNotifyReturnValue As Boolean = False

            Public WouldNavigateToSymbolProvidedDefinitionItem As DefinitionItem
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

            Public Function TrySymbolNavigationNotify(symbol As ISymbol,
                                                      project As Project,
                                                      cancellationToken As CancellationToken) As Boolean Implements ISymbolNavigationService.TrySymbolNavigationNotify
                Me.TrySymbolNavigationNotifyProvidedSymbol = symbol
                Me.TrySymbolNavigationNotifyProvidedProject = project

                Return TrySymbolNavigationNotifyReturnValue
            End Function

            Public Function WouldNavigateToSymbol(definitionItem As DefinitionItem,
                                                  solution As Solution,
                                                  cancellationToken As CancellationToken,
                                                  ByRef filePath As String, ByRef lineNumber As Integer, ByRef charOffset As Integer) As Boolean Implements ISymbolNavigationService.WouldNavigateToSymbol
                Me.WouldNavigateToSymbolProvidedDefinitionItem = definitionItem
                Me.WouldNavigateToSymbolProvidedSolution = solution

                filePath = Me.NavigationFilePathReturnValue
                lineNumber = Me.NavigationLineNumberReturnValue
                charOffset = Me.NavigationCharOffsetReturnValue

                Return WouldNavigateToSymbolReturnValue
            End Function
        End Class
    End Class
End Namespace
