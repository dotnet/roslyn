' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockSymbolNavigationService
        Implements ISymbolNavigationService

        Public _triedNavigationToSymbol As Boolean = False
        Public _triedSymbolNavigationNotify As Boolean = False
        Public _wouldNavigateToSymbol As Boolean = False

        Public Function TryNavigateToSymbol(symbol As ISymbol, project As Project, Optional options As OptionSet = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Boolean Implements ISymbolNavigationService.TryNavigateToSymbol
            _triedNavigationToSymbol = True
            Return True
        End Function

        Public Function TrySymbolNavigationNotify(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Boolean Implements ISymbolNavigationService.TrySymbolNavigationNotify
            _triedSymbolNavigationNotify = True
            Return True
        End Function

        Public Function WouldNavigateToSymbol(definitionItem As DefinitionItem, solution As Solution, cancellationToken As CancellationToken, ByRef filePath As String, ByRef lineNumber As Integer, ByRef charOffset As Integer) As Boolean Implements ISymbolNavigationService.WouldNavigateToSymbol
            _wouldNavigateToSymbol = True
            Return True
        End Function
    End Class
End Namespace
