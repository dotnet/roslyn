' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Wrapping
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping

    Friend NotInheritable Class VisualBasicSyntaxWrappingOptions
        Inherits SyntaxWrappingOptions

        Public Sub New(
            formattingOptions As VisualBasicSyntaxFormattingOptions,
            wrappingColumn As Integer,
            operatorPlacement As OperatorPlacementWhenWrappingPreference)

            MyBase.New(formattingOptions, wrappingColumn, operatorPlacement)
        End Sub

        Public Shared Function Create(options As IOptionsReader, fallbackOptions As CodeActionOptions) As VisualBasicSyntaxWrappingOptions
            Return New VisualBasicSyntaxWrappingOptions(
                formattingOptions:=New VisualBasicSyntaxFormattingOptions(options, DirectCast(fallbackOptions.CleanupOptions.FormattingOptions, VisualBasicSyntaxFormattingOptions)),
                operatorPlacement:=options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping, fallbackOptions.CodeStyleOptions.OperatorPlacementWhenWrapping),
                wrappingColumn:=fallbackOptions.WrappingColumn)
        End Function
    End Class
End Namespace
