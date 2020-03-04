' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editor.visualbasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public MustInherit Class AbstractWrappingTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        Protected Shared Function GetIndentionColumn(column As Integer) As Dictionary(Of OptionKey, Object)
            Return New Dictionary(Of OptionKey, Object) From {
                   {FormattingOptions.PreferredWrappingColumn, column}
               }
        End Function

        Protected Function TestAllWrappingCasesAsync(
            input As String,
            ParamArray outputs As String()) As Task

            Return TestAllWrappingCasesAsync(input, options:=Nothing, outputs)
        End Function

        Protected Function TestAllWrappingCasesAsync(
            input As String,
            options As IDictionary(Of OptionKey, Object),
            ParamArray outputs As String()) As Task

            Dim parameters = New TestParameters(options:=options)
            Return TestAllInRegularAndScriptAsync(input, parameters, outputs)
        End Function
    End Class
End Namespace
