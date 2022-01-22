' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

    Partial Public MustInherit Class AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Friend Shared Function SingleOption(Of T)(optionParam As Option2(Of T), enabled As T) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam), enabled)
        End Function

        Friend Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of T), value As T) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam, Me.GetLanguage()), value)
        End Function

        Friend Shared Function SingleOption(Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As (OptionKey2, Object)
            Return SingleOption(optionParam, New CodeStyleOption2(Of T)(enabled, notification))
        End Function

        Friend Shared Function SingleOption(Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam), codeStyle)
        End Function

        Friend Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As (OptionKey2, Object)
            Return SingleOption(optionParam, New CodeStyleOption2(Of T)(enabled, notification))
        End Function

        Friend Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As (OptionKey2, Object)
            Return SingleOption(optionParam, codeStyle, language:=GetLanguage())
        End Function

        Friend Shared Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T), language As String) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam, language), codeStyle)
        End Function

        Friend Function [Option](Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As OptionsCollection
            Return New OptionsCollection(GetLanguage()) From {{optionParam, enabled, notification}}
        End Function

        Friend Function [Option](Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As OptionsCollection
            Return New OptionsCollection(GetLanguage()) From {{optionParam, codeStyle}}
        End Function

        Friend Function [Option](Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As OptionsCollection
            Return New OptionsCollection(GetLanguage()) From {{optionParam, enabled, notification}}
        End Function

        Friend Function [Option](Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As OptionsCollection
            Return New OptionsCollection(GetLanguage()) From {{optionParam, codeStyle}}
        End Function

        Friend Function [Option](Of T)(optionParam As Option2(Of T), value As T) As OptionsCollection
            Return New OptionsCollection(GetLanguage()) From {{optionParam, value}}
        End Function

        Friend Function [Option](Of T)(optionParam As PerLanguageOption2(Of T), value As T) As OptionsCollection
            Return New OptionsCollection(GetLanguage()) From {{optionParam, value}}
        End Function
    End Class
End Namespace
