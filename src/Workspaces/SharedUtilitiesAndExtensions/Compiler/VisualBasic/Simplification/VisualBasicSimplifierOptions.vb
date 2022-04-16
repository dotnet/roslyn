' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.CodeStyle
Imports System.Runtime.Serialization

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    <DataContract>
    Friend NotInheritable Class VisualBasicSimplifierOptions
        Inherits SimplifierOptions

        Public Sub New(
            Optional qualifyFieldAccess As CodeStyleOption2(Of Boolean) = Nothing,
            Optional qualifyPropertyAccess As CodeStyleOption2(Of Boolean) = Nothing,
            Optional qualifyMethodAccess As CodeStyleOption2(Of Boolean) = Nothing,
            Optional qualifyEventAccess As CodeStyleOption2(Of Boolean) = Nothing,
            Optional preferPredefinedTypeKeywordInMemberAccess As CodeStyleOption2(Of Boolean) = Nothing,
            Optional preferPredefinedTypeKeywordInDeclaration As CodeStyleOption2(Of Boolean) = Nothing)

            MyBase.New(
                qualifyFieldAccess,
                qualifyPropertyAccess,
                qualifyMethodAccess,
                qualifyEventAccess,
                preferPredefinedTypeKeywordInMemberAccess,
                preferPredefinedTypeKeywordInDeclaration)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicSimplifierOptions()

        Friend Overloads Shared Function Create(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicSimplifierOptions) As VisualBasicSimplifierOptions
            fallbackOptions = If(fallbackOptions, VisualBasicSimplifierOptions.Default)

            Return New VisualBasicSimplifierOptions(
                qualifyFieldAccess:=options.GetEditorConfigOption(CodeStyleOptions2.QualifyFieldAccess, fallbackOptions.QualifyFieldAccess),
                qualifyPropertyAccess:=options.GetEditorConfigOption(CodeStyleOptions2.QualifyPropertyAccess, fallbackOptions.QualifyPropertyAccess),
                qualifyMethodAccess:=options.GetEditorConfigOption(CodeStyleOptions2.QualifyMethodAccess, fallbackOptions.QualifyMethodAccess),
                qualifyEventAccess:=options.GetEditorConfigOption(CodeStyleOptions2.QualifyEventAccess, fallbackOptions.QualifyEventAccess),
                preferPredefinedTypeKeywordInMemberAccess:=options.GetEditorConfigOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, fallbackOptions.PreferPredefinedTypeKeywordInMemberAccess),
                preferPredefinedTypeKeywordInDeclaration:=options.GetEditorConfigOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, fallbackOptions.PreferPredefinedTypeKeywordInDeclaration))
        End Function
    End Class
End Namespace
