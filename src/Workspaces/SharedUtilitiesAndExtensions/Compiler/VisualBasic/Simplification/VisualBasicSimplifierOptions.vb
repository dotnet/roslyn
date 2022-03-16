' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.CodeStyle

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend NotInheritable Class VisualBasicSimplifierOptions
        Inherits SimplifierOptions

        Public Sub New(
            qualifyFieldAccess As CodeStyleOption2(Of Boolean),
            qualifyPropertyAccess As CodeStyleOption2(Of Boolean),
            qualifyMethodAccess As CodeStyleOption2(Of Boolean),
            qualifyEventAccess As CodeStyleOption2(Of Boolean),
            preferPredefinedTypeKeywordInMemberAccess As CodeStyleOption2(Of Boolean),
            preferPredefinedTypeKeywordInDeclaration As CodeStyleOption2(Of Boolean))

            MyBase.New(
                qualifyFieldAccess,
                qualifyPropertyAccess,
                qualifyMethodAccess,
                qualifyEventAccess,
                preferPredefinedTypeKeywordInMemberAccess,
                preferPredefinedTypeKeywordInDeclaration)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicSimplifierOptions(
            qualifyFieldAccess:=CodeStyleOptions2.QualifyFieldAccess.DefaultValue,
            qualifyPropertyAccess:=CodeStyleOptions2.QualifyPropertyAccess.DefaultValue,
            qualifyMethodAccess:=CodeStyleOptions2.QualifyMethodAccess.DefaultValue,
            qualifyEventAccess:=CodeStyleOptions2.QualifyEventAccess.DefaultValue,
            preferPredefinedTypeKeywordInMemberAccess:=CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.DefaultValue,
            preferPredefinedTypeKeywordInDeclaration:=CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration.DefaultValue)

        Friend Overloads Shared Function Create(options As AnalyzerConfigOptions) As VisualBasicSimplifierOptions
            Return New VisualBasicSimplifierOptions(
                qualifyFieldAccess:=options.GetOption(CodeStyleOptions2.QualifyFieldAccess),
                qualifyPropertyAccess:=options.GetOption(CodeStyleOptions2.QualifyPropertyAccess),
                qualifyMethodAccess:=options.GetOption(CodeStyleOptions2.QualifyMethodAccess),
                qualifyEventAccess:=options.GetOption(CodeStyleOptions2.QualifyEventAccess),
                preferPredefinedTypeKeywordInMemberAccess:=options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
                preferPredefinedTypeKeywordInDeclaration:=options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration))
        End Function
    End Class
End Namespace
