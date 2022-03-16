' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.SymbolSearch

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public MustInherit Class AbstractWrappingTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        Private Protected Shared Function GetIndentionColumn(column As Integer) As CodeActionOptions
            Return New CodeActionOptions(
                SymbolSearchOptions.Default,
                ImplementTypeOptions.Default,
                ExtractMethodOptions.Default,
                WrappingColumn:=column)
        End Function

        Protected Function TestAllWrappingCasesAsync(
            input As String,
            ParamArray outputs As String()) As Task

            Return TestAllWrappingCasesAsync(input, options:=CodeActionOptions.Default, outputs)
        End Function

        Private Protected Function TestAllWrappingCasesAsync(
            input As String,
            options As CodeActionOptions,
            ParamArray outputs As String()) As Task

            Dim parameters = New TestParameters(codeActionOptions:=options)
            Return TestAllInRegularAndScriptAsync(input, parameters, outputs)
        End Function

        Private Protected Function TestAllWrappingCasesAsync(
            input As String,
            options As OptionsCollection,
            ParamArray outputs As String()) As Task

            Dim parameters = New TestParameters(options:=options)
            Return TestAllInRegularAndScriptAsync(input, parameters, outputs)
        End Function
    End Class
End Namespace
