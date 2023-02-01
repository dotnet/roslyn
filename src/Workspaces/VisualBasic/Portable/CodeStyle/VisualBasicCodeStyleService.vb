' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Composition
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    <ExportLanguageService(GetType(ICodeStyleService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicCodeStyleService
        Implements ICodeStyleService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public ReadOnly Property DefaultOptions As IdeCodeStyleOptions Implements ICodeStyleService.DefaultOptions
            Get
                Return VisualBasicIdeCodeStyleOptions.Default
            End Get
        End Property

        Public Function GetIdeCodeStyleOptions(options As IOptionsReader, fallbackOptions As IdeCodeStyleOptions) As IdeCodeStyleOptions Implements ICodeStyleService.GetIdeCodeStyleOptions
            Return options.GetVisualBasicCodeStyleOptions(DirectCast(fallbackOptions, VisualBasicIdeCodeStyleOptions))
        End Function
    End Class

    Friend Module VisualBasicIdeCodeStyleOptionsProviders
        <Extension>
        Public Function GetVisualBasicCodeStyleOptions(options As IOptionsReader, fallbackOptions As VisualBasicIdeCodeStyleOptions) As VisualBasicIdeCodeStyleOptions
            If fallbackOptions Is Nothing Then
                fallbackOptions = VisualBasicIdeCodeStyleOptions.Default
            End If

            Return New VisualBasicIdeCodeStyleOptions(
                Common:=options.GetCommonCodeStyleOptions(LanguageNames.VisualBasic, fallbackOptions.Common),
                PreferredModifierOrder:=options.GetOption(VisualBasicCodeStyleOptions.PreferredModifierOrder, fallbackOptions.PreferredModifierOrder),
                PreferIsNotExpression:=options.GetOption(VisualBasicCodeStyleOptions.PreferIsNotExpression, fallbackOptions.PreferIsNotExpression),
                PreferSimplifiedObjectCreation:=options.GetOption(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, fallbackOptions.PreferSimplifiedObjectCreation),
                UnusedValueExpressionStatement:=options.GetOption(VisualBasicCodeStyleOptions.UnusedValueExpressionStatement, fallbackOptions.UnusedValueExpressionStatement),
                UnusedValueAssignment:=options.GetOption(VisualBasicCodeStyleOptions.UnusedValueAssignment, fallbackOptions.UnusedValueAssignment))
        End Function
    End Module
End Namespace
