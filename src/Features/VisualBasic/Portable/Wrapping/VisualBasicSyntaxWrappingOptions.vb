' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.Wrapping

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping

    Friend NotInheritable Class VisualBasicSyntaxWrappingOptions
        Inherits SyntaxWrappingOptions

        Public Sub New(
            formattingOptions As VisualBasicSyntaxFormattingOptions,
            operatorPlacement As OperatorPlacementWhenWrappingPreference)

            MyBase.New(formattingOptions, operatorPlacement)
        End Sub

        Public Shared Function Create(options As IOptionsReader) As VisualBasicSyntaxWrappingOptions
            Return New VisualBasicSyntaxWrappingOptions(
                formattingOptions:=New VisualBasicSyntaxFormattingOptions(options),
                operatorPlacement:=options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping))
        End Function
    End Class
End Namespace
