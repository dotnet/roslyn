﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <DataContract>
    Friend NotInheritable Class VisualBasicSyntaxFormattingOptions
        Inherits SyntaxFormattingOptions

        Public Sub New(lineFormatting As LineFormattingOptions,
                       separateImportDirectiveGroups As Boolean)

            MyBase.New(lineFormatting, separateImportDirectiveGroups)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicSyntaxFormattingOptions(
            lineFormatting:=LineFormattingOptions.Default,
            separateImportDirectiveGroups:=GenerationOptions.SeparateImportDirectiveGroups.DefaultValue)

        Public Shared Shadows Function Create(options As AnalyzerConfigOptions) As VisualBasicSyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(
                lineFormatting:=LineFormattingOptions.Create(options),
                separateImportDirectiveGroups:=options.GetOption(GenerationOptions.SeparateImportDirectiveGroups))
        End Function

        Public Overrides Function [With](lineFormatting As LineFormattingOptions) As SyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(
                lineFormatting:=lineFormatting,
                separateImportDirectiveGroups:=SeparateImportDirectiveGroups)
        End Function
    End Class
End Namespace
