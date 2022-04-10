' Licensed to the .NET Foundation under one or more agreements.
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

        Public Sub New(useTabs As Boolean,
                       tabSize As Integer,
                       indentationSize As Integer,
                       newLine As String,
                       separateImportDirectiveGroups As Boolean)

            MyBase.New(useTabs,
                       tabSize,
                       indentationSize,
                       newLine,
                       separateImportDirectiveGroups)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicSyntaxFormattingOptions(
            useTabs:=FormattingOptions2.UseTabs.DefaultValue,
            tabSize:=FormattingOptions2.TabSize.DefaultValue,
            indentationSize:=FormattingOptions2.IndentationSize.DefaultValue,
            newLine:=FormattingOptions2.NewLine.DefaultValue,
            separateImportDirectiveGroups:=GenerationOptions.SeparateImportDirectiveGroups.DefaultValue)

        Public Shared Shadows Function Create(options As AnalyzerConfigOptions) As VisualBasicSyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(
                useTabs:=options.GetOption(FormattingOptions2.UseTabs),
                tabSize:=options.GetOption(FormattingOptions2.TabSize),
                indentationSize:=options.GetOption(FormattingOptions2.IndentationSize),
                newLine:=options.GetOption(FormattingOptions2.NewLine),
                separateImportDirectiveGroups:=options.GetOption(GenerationOptions.SeparateImportDirectiveGroups))
        End Function

        Public Overrides Function [With](useTabs As Boolean, tabSize As Integer, indentationSize As Integer) As SyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(
                useTabs:=useTabs,
                tabSize:=tabSize,
                indentationSize:=indentationSize,
                newLine:=NewLine,
                separateImportDirectiveGroups:=SeparateImportDirectiveGroups)
        End Function
    End Class
End Namespace
