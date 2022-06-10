﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public MustInherit Class AbstractWrappingTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        Private Protected Function GetIndentionColumn(column As Integer) As TestParameters
            Return New TestParameters(globalOptions:=[Option](CodeActionOptionsStorage.WrappingColumn, column))
        End Function

        Protected Function TestAllWrappingCasesAsync(
            input As String,
            ParamArray outputs As String()) As Task

            Return TestAllWrappingCasesAsync(input, parameters:=Nothing, outputs)
        End Function

        Private Protected Function TestAllWrappingCasesAsync(
            input As String,
            parameters As TestParameters,
            ParamArray outputs As String()) As Task

            Return TestAllInRegularAndScriptAsync(input, parameters, outputs)
        End Function
    End Class
End Namespace
