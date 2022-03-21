' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Wrapping
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping

    Friend NotInheritable Class VisualBasicSyntaxWrappingOptions
        Inherits SyntaxWrappingOptions

        Public Sub New(
            useTabs As Boolean,
            tabSize As Integer,
            newLine As String,
            wrappingColumn As Integer,
            operatorPlacement As OperatorPlacementWhenWrappingPreference)

            MyBase.New(useTabs, tabSize, newLine, wrappingColumn, operatorPlacement)
        End Sub

        Public Shared Function Create(options As AnalyzerConfigOptions, ideOptions As CodeActionOptions) As VisualBasicSyntaxWrappingOptions
            Return New VisualBasicSyntaxWrappingOptions(
                useTabs:=options.GetOption(FormattingOptions2.UseTabs),
                tabSize:=options.GetOption(FormattingOptions2.TabSize),
                newLine:=options.GetOption(FormattingOptions2.NewLine),
                operatorPlacement:=options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping),
                wrappingColumn:=ideOptions.WrappingColumn)
        End Function
    End Class
End Namespace
