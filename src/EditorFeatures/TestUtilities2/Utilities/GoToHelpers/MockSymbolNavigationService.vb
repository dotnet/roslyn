' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockSymbolNavigationService
        Implements ISymbolNavigationService

        Public _triedNavigationToSymbol As Boolean
        Public _triedSymbolNavigationNotify As Boolean
        Public _wouldNavigateToSymbol As Boolean

        Public Function TryNavigateToSymbolAsync(symbol As ISymbol, project As Project, options As NavigationOptions, cancellationToken As CancellationToken) As Task(Of Boolean) Implements ISymbolNavigationService.TryNavigateToSymbolAsync
            _triedNavigationToSymbol = True
            Return SpecializedTasks.True
        End Function

        Public Function TrySymbolNavigationNotifyAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of Boolean) Implements ISymbolNavigationService.TrySymbolNavigationNotifyAsync
            _triedSymbolNavigationNotify = True
            Return SpecializedTasks.True
        End Function

        Public Function GetExternalNavigationSymbolLocationAsync(definitionItem As DefinitionItem, cancellationToken As CancellationToken) As Task(Of (filePath As String, linePosition As LinePosition)?) Implements ISymbolNavigationService.GetExternalNavigationSymbolLocationAsync
            _wouldNavigateToSymbol = True
            Return Task.FromResult(Of (filePath As String, linePosition As LinePosition)?)(Nothing)
        End Function
    End Class
End Namespace
