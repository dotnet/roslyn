' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend Module VisualBasicCodeStyleOptionsStorage
        <ExportLanguageService(GetType(ICodeStyleOptionsStorage), LanguageNames.VisualBasic), [Shared]>
        Private NotInheritable Class Service
            Implements ICodeStyleOptionsStorage

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetOptions(globalOptions As IGlobalOptionService) As IdeCodeStyleOptions Implements ICodeStyleOptionsStorage.GetOptions
                Return GetVisualBasicCodeStyleOptions(globalOptions)
            End Function
        End Class

        <Extension>
        Public Function GetVisualBasicCodeStyleOptions(globalOptions As IGlobalOptionService) As VisualBasicIdeCodeStyleOptions
            Return New VisualBasicIdeCodeStyleOptions(
                Common:=globalOptions.GetCommonCodeStyleOptions(LanguageNames.VisualBasic),
                PreferredModifierOrder:=globalOptions.GetOption(VisualBasicCodeStyleOptions.PreferredModifierOrder),
                PreferIsNotExpression:=globalOptions.GetOption(VisualBasicCodeStyleOptions.PreferIsNotExpression),
                PreferSimplifiedObjectCreation:=globalOptions.GetOption(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation),
                UnusedValueExpressionStatement:=globalOptions.GetOption(VisualBasicCodeStyleOptions.UnusedValueExpressionStatement),
                UnusedValueAssignment:=globalOptions.GetOption(VisualBasicCodeStyleOptions.UnusedValueAssignment))
        End Function
    End Module
End Namespace
