' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportArgumentProvider(NameOf(DefaultArgumentProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ContextVariableArgumentProvider))>
    <[Shared]>
    Friend Class DefaultArgumentProvider
        Inherits AbstractDefaultArgumentProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function ProvideArgumentAsync(context As ArgumentContext) As Task
            If context.PreviousValue IsNot Nothing Then
                context.DefaultValue = context.PreviousValue
            Else
                Select Case context.Parameter.Type.SpecialType
                    Case SpecialType.System_Boolean
                        context.DefaultValue = "False"
                    Case SpecialType.System_Char
                        context.DefaultValue = "Chr(0)"
                    Case SpecialType.System_Byte
                        context.DefaultValue = "CByte(0)"
                    Case SpecialType.System_SByte
                        context.DefaultValue = "CSByte(0)"
                    Case SpecialType.System_Int16
                        context.DefaultValue = "0S"
                    Case SpecialType.System_UInt16
                        context.DefaultValue = "0US"
                    Case SpecialType.System_Int32
                        context.DefaultValue = "0"
                    Case SpecialType.System_UInt32
                        context.DefaultValue = "0U"
                    Case SpecialType.System_Int64
                        context.DefaultValue = "0L"
                    Case SpecialType.System_UInt64
                        context.DefaultValue = "0UL"
                    Case SpecialType.System_Decimal
                        context.DefaultValue = "0.0D"
                    Case SpecialType.System_Single
                        context.DefaultValue = "0.0F"
                    Case SpecialType.System_Double
                        context.DefaultValue = "0.0"
                    Case Else
                        context.DefaultValue = "Nothing"
                End Select
            End If

            Return Task.CompletedTask
        End Function
    End Class
End Namespace
