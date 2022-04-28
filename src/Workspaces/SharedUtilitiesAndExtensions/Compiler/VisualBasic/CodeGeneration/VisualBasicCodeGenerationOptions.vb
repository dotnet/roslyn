' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <DataContract>
    Friend NotInheritable Class VisualBasicCodeGenerationOptions
        Inherits CodeGenerationOptions

        Public Sub New(
                Optional addNullChecksToConstructorsGeneratedFromMembers As Boolean = DefaultAddNullChecksToConstructorsGeneratedFromMembers,
                Optional namingStyle As NamingStylePreferences = Nothing)
            MyBase.New(
                namingStyle,
                addNullChecksToConstructorsGeneratedFromMembers)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicCodeGenerationOptions()

#If Not CODE_STYLE Then
        Public Overloads Shared Function Create(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicCodeGenerationOptions) As VisualBasicCodeGenerationOptions
            ' Not stored in editorconfig
            Return New VisualBasicCodeGenerationOptions(
                addNullChecksToConstructorsGeneratedFromMembers:=fallbackOptions.AddNullChecksToConstructorsGeneratedFromMembers,
                namingStyle:=options.GetEditorConfigOption(NamingStyleOptions.NamingPreferences, fallbackOptions.NamingStyle))
        End Function

        Public Overrides Function GetInfo(context As CodeGenerationContext, parseOptions As ParseOptions) As CodeGenerationContextInfo
            Return New VisualBasicCodeGenerationContextInfo(context, Me)
        End Function
#End If
    End Class
End Namespace
