' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Module VisualBasicSyntaxFormattingOptionsStorage
        <ExportLanguageService(GetType(ISyntaxFormattingOptionsStorage), LanguageNames.VisualBasic), [Shared]>
        Private NotInheritable Class Service
            Implements ISyntaxFormattingOptionsStorage

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetOptions(globalOptions As IGlobalOptionService) As SyntaxFormattingOptions Implements ISyntaxFormattingOptionsStorage.GetOptions
                Return GetVisualBasicSyntaxFormattingOptions(globalOptions)
            End Function
        End Class

        <Extension>
        Public Function GetVisualBasicSyntaxFormattingOptions(globalOptions As IGlobalOptionService) As VisualBasicSyntaxFormattingOptions
            Return New VisualBasicSyntaxFormattingOptions(
                lineFormatting:=globalOptions.GetLineFormattingOptions(LanguageNames.VisualBasic),
                separateImportDirectiveGroups:=globalOptions.GetOption(GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.VisualBasic))
        End Function
    End Module
End Namespace
