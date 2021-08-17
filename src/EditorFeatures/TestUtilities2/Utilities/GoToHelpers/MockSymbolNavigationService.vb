' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockSymbolNavigationService
        Implements ISymbolNavigationService

        Public _triedNavigationToSymbol As Boolean
        Public _triedSymbolNavigationNotify As Boolean
        Public _wouldNavigateToSymbol As Boolean

        Public Function TryNavigateToSymbol(symbol As ISymbol, project As Project, Optional options As OptionSet = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Boolean Implements ISymbolNavigationService.TryNavigateToSymbol
            _triedNavigationToSymbol = True
            Return True
        End Function

        Public Function TrySymbolNavigationNotifyAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of Boolean) Implements ISymbolNavigationService.TrySymbolNavigationNotifyAsync
            _triedSymbolNavigationNotify = True
            Return SpecializedTasks.True
        End Function

        Public Function WouldNavigateToSymbolAsync(definitionItem As DefinitionItem, cancellationToken As CancellationToken) As Task(Of (filePath As String, lineNumber As Integer, charOffset As Integer)?) Implements ISymbolNavigationService.WouldNavigateToSymbolAsync
            _wouldNavigateToSymbol = True
            Return Task.FromResult(Of (filePath As String, lineNumber As Integer, charOffset As Integer)?)(Nothing)
        End Function
    End Class
End Namespace
