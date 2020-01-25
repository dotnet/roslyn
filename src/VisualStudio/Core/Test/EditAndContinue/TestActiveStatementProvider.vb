' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.EditAndContinue

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
    Friend Class TestActiveStatementProvider
        Implements IActiveStatementProvider

        Public Function GetActiveStatementsAsync(cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ActiveStatementDebugInfo)) Implements IActiveStatementProvider.GetActiveStatementsAsync
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
