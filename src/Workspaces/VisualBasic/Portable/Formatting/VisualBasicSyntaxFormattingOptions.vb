' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <DataContract>
    Friend NotInheritable Class VisualBasicSyntaxFormattingOptions
        Inherits SyntaxFormattingOptions

        Public Sub New(Optional lineFormatting As LineFormattingOptions = Nothing,
                       Optional separateImportDirectiveGroups As Boolean = DefaultSeparateImportDirectiveGroups,
                       Optional accessibilityModifiersRequired As AccessibilityModifiersRequired = DefaultAccessibilityModifiersRequired)

            MyBase.New(lineFormatting,
                       separateImportDirectiveGroups,
                       accessibilityModifiersRequired)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicSyntaxFormattingOptions()

        Public Shared Shadows Function Create(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicSyntaxFormattingOptions) As VisualBasicSyntaxFormattingOptions
            fallbackOptions = If(fallbackOptions, [Default])

            Return New VisualBasicSyntaxFormattingOptions(
                LineFormattingOptions.Create(options, fallbackOptions.LineFormatting),
                options.GetEditorConfigOption(GenerationOptions.SeparateImportDirectiveGroups, fallbackOptions.SeparateImportDirectiveGroups),
                options.GetEditorConfigOptionValue(CodeStyleOptions2.RequireAccessibilityModifiers, fallbackOptions.AccessibilityModifiersRequired))
        End Function

        Public Overrides Function [With](lineFormatting As LineFormattingOptions) As SyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(
                lineFormatting,
                SeparateImportDirectiveGroups,
                AccessibilityModifiersRequired)
        End Function
    End Class
End Namespace
