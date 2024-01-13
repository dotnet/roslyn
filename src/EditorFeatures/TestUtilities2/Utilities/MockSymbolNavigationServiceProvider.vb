' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
    ' Note: by default, TestWorkspace produces a composition from all assemblies except EditorServicesTest2.
    ' This type has to be defined here until we get that cleaned up. Otherwise, other tests may import it.
    <ExportWorkspaceServiceFactory(GetType(ISymbolNavigationService), ServiceLayer.Test), [Shared], PartNotDiscoverable>
    Public Class MockSymbolNavigationServiceProvider
        Implements IWorkspaceServiceFactory

        Private ReadOnly _instance As MockSymbolNavigationService = New MockSymbolNavigationService()

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return _instance
        End Function

        Friend Class MockSymbolNavigationService
            Implements ISymbolNavigationService

            Public TryNavigateToSymbolProvidedSymbol As ISymbol
            Public TryNavigateToSymbolProvidedProject As Project

            Public TrySymbolNavigationNotifyProvidedSymbol As ISymbol
            Public TrySymbolNavigationNotifyProvidedProject As Project
            Public TrySymbolNavigationNotifyReturnValue As Boolean

            Public WouldNavigateToSymbolProvidedDefinitionItem As DefinitionItem
            Public NavigationFilePathReturnValue As String = String.Empty
            Public NavigationLineNumberReturnValue As Integer
            Public NavigationCharOffsetReturnValue As Integer

            Public Function GetNavigableLocationAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of INavigableLocation) Implements ISymbolNavigationService.GetNavigableLocationAsync
                Me.TryNavigateToSymbolProvidedSymbol = symbol
                Me.TryNavigateToSymbolProvidedProject = project
                Return NavigableLocation.TestAccessor.Create(True)
            End Function

            Public Function TrySymbolNavigationNotifyAsync(
                    symbol As ISymbol,
                    project As Project,
                    cancellationToken As CancellationToken) As Task(Of Boolean) Implements ISymbolNavigationService.TrySymbolNavigationNotifyAsync
                Me.TrySymbolNavigationNotifyProvidedSymbol = symbol
                Me.TrySymbolNavigationNotifyProvidedProject = project

                Return Task.FromResult(TrySymbolNavigationNotifyReturnValue)
            End Function

            Public Function GetExternalNavigationSymbolLocationAsync(
                    definitionItem As DefinitionItem,
                    cancellationToken As CancellationToken) As Task(Of (filePath As String, LinePosition As LinePosition)?) Implements ISymbolNavigationService.GetExternalNavigationSymbolLocationAsync
                Me.WouldNavigateToSymbolProvidedDefinitionItem = definitionItem

                Return Task.FromResult(Of (filePath As String, linePosition As LinePosition)?)((Me.NavigationFilePathReturnValue, New LinePosition(Me.NavigationLineNumberReturnValue, Me.NavigationCharOffsetReturnValue)))
            End Function
        End Class
    End Class
End Namespace
