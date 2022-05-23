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
        Implements IEquatable(Of VisualBasicSyntaxFormattingOptions)

        Public Shared ReadOnly [Default] As New VisualBasicSyntaxFormattingOptions()

        Public Shared Shadows Function Create(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicSyntaxFormattingOptions) As VisualBasicSyntaxFormattingOptions
            fallbackOptions = If(fallbackOptions, [Default])

            Return New VisualBasicSyntaxFormattingOptions() With
            {
                .Common = options.GetCommonSyntaxFormattingOptions(fallbackOptions.Common)
            }
        End Function

        Public Overrides Function [With](lineFormatting As LineFormattingOptions) As SyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions() With
            {
                .Common = New CommonOptions() With
                {
                    .LineFormatting = lineFormatting,
                    .SeparateImportDirectiveGroups = SeparateImportDirectiveGroups,
                    .AccessibilityModifiersRequired = AccessibilityModifiersRequired
                }
            }
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, VisualBasicSyntaxFormattingOptions))
        End Function

        Public Overloads Function Equals(other As VisualBasicSyntaxFormattingOptions) As Boolean Implements IEquatable(Of VisualBasicSyntaxFormattingOptions).Equals
            Return other IsNot Nothing AndAlso
                   Common.Equals(other.Common)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Common.GetHashCode()
        End Function
    End Class
End Namespace
