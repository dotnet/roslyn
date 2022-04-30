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

        Public Shared ReadOnly [Default] As New VisualBasicSyntaxFormattingOptions()

        Public Shared Shadows Function Create(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicSyntaxFormattingOptions) As VisualBasicSyntaxFormattingOptions
            fallbackOptions = If(fallbackOptions, [Default])

            Return New VisualBasicSyntaxFormattingOptions() With
            {
                .LineFormatting = LineFormattingOptions.Create(options, fallbackOptions.LineFormatting),
                .SeparateImportDirectiveGroups = options.GetEditorConfigOption(GenerationOptions.SeparateImportDirectiveGroups, fallbackOptions.SeparateImportDirectiveGroups),
                .AccessibilityModifiersRequired = options.GetEditorConfigOptionValue(CodeStyleOptions2.RequireAccessibilityModifiers, fallbackOptions.AccessibilityModifiersRequired)
            }
        End Function

        Public Overrides Function [With](lineFormatting As LineFormattingOptions) As SyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions() With
            {
                .LineFormatting = lineFormatting,
                .SeparateImportDirectiveGroups = SeparateImportDirectiveGroups,
                .AccessibilityModifiersRequired = AccessibilityModifiersRequired
            }
        End Function
    End Class
End Namespace
