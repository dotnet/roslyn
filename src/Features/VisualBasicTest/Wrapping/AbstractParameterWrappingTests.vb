' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public MustInherit Class AbstractWrappingTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        ' Wrapping tests need CRLF consistency so that IsEquivalentTo comparisons in the
        ' wrapping code work correctly across platforms.
        Protected Overrides Function NormalizeMarkup(markup As String) As String
            Return markup.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
        End Function

        ' Ensure FormattingOptions2.NewLine is always vbCrLf for wrapping tests. This is
        ' called by CreateWorkspaceFromOptions, ensuring ALL test paths use consistent CRLF.
        Protected Overrides Function SetParameterDefaults(parameters As TestParameters) As TestParameters
            Dim opts = New OptionsCollection(GetLanguage())
            If TypeOf parameters.options Is OptionsCollection Then
                opts.Add(DirectCast(parameters.options, OptionsCollection))
            End If
            opts.Set(FormattingOptions2.NewLine, vbCrLf)
            Return parameters.WithOptions(opts)
        End Function

        Private Protected Function GetIndentionColumn(column As Integer) As TestParameters
            Return New TestParameters(options:=[Option](FormattingOptions2.WrappingColumn, column))
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
