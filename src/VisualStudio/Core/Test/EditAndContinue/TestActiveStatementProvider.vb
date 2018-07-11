' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
