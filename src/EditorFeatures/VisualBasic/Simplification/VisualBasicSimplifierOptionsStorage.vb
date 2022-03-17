' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend Module VisualBasicSimplifierOptionsStorage
        <Extension>
        Public Function GetVisualBasicSimplifierOptions(globalOptions As IGlobalOptionService) As VisualBasicSimplifierOptions
            Return New VisualBasicSimplifierOptions(
                qualifyFieldAccess:=globalOptions.GetOption(CodeStyleOptions2.QualifyFieldAccess, LanguageNames.VisualBasic),
                qualifyPropertyAccess:=globalOptions.GetOption(CodeStyleOptions2.QualifyPropertyAccess, LanguageNames.VisualBasic),
                qualifyMethodAccess:=globalOptions.GetOption(CodeStyleOptions2.QualifyMethodAccess, LanguageNames.VisualBasic),
                qualifyEventAccess:=globalOptions.GetOption(CodeStyleOptions2.QualifyEventAccess, LanguageNames.VisualBasic),
                preferPredefinedTypeKeywordInMemberAccess:=globalOptions.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic),
                preferPredefinedTypeKeywordInDeclaration:=globalOptions.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic))
        End Function
    End Module
End Namespace
