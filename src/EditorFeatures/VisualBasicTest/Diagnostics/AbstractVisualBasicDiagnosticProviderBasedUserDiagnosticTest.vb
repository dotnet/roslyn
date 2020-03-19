' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics

#If CODE_STYLE Then
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
#End If

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

    Public MustInherit Class AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Inherits AbstractDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly _compilationOptions As VisualBasicCompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True).WithParseOptions(New VisualBasicParseOptions(LanguageVersion.Latest))

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(
                initialMarkup,
                parameters.parseOptions,
                If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Friend Overloads Async Function TestAsync(
                initialMarkup As XElement, expected As XElement, Optional index As Integer = 0) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()
            Dim expectedStr = expected.ConvertTestSourceTag()

            Await MyBase.TestAsync(initialMarkupStr, expectedStr,
                                   parseOptions:=_compilationOptions.ParseOptions, compilationOptions:=_compilationOptions,
                                   index:=index)
        End Function

        Protected Overloads Async Function TestMissingAsync(initialMarkup As XElement) As Threading.Tasks.Task
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()

            Await MyBase.TestMissingAsync(initialMarkupStr, New TestParameters(parseOptions:=Nothing, compilationOptions:=_compilationOptions))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

#If CODE_STYLE Then
        Friend Shadows Function SingleOption(Of T)(optionParam As Option2(Of T), enabled As T) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam), enabled)
        End Function

        Friend Shadows Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of T), value As T) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam, Me.GetLanguage()), value)
        End Function

        Friend Shadows Function SingleOption(Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As (OptionKey2, Object)
            Return SingleOption(optionParam, New CodeStyleOption2(Of T)(enabled, notification))
        End Function

        Friend Shadows Function SingleOption(Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam), codeStyle)
        End Function

        Friend Shadows Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As (OptionKey2, Object)
            Return SingleOption(optionParam, New CodeStyleOption2(Of T)(enabled, notification))
        End Function

        Friend Shadows Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As (OptionKey2, Object)
            Return SingleOption(optionParam, codeStyle, language:=GetLanguage())
        End Function

        Friend Shadows Function SingleOption(Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T), language As String) As (OptionKey2, Object)
            Return (New OptionKey2(optionParam, language), codeStyle)
        End Function

        Friend Shadows Function [Option](Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As IOptionsCollection
            Return OptionsSet(SingleOption(optionParam, enabled, notification))
        End Function

        Friend Shadows Function [Option](Of T)(optionParam As Option2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As IOptionsCollection
            Return OptionsSet(SingleOption(optionParam, codeStyle))
        End Function

        Friend Shadows Function [Option](Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), enabled As T, notification As NotificationOption2) As IOptionsCollection
            Return OptionsSet(SingleOption(optionParam, enabled, notification))
        End Function

        Friend Shadows Function [Option](Of T)(optionParam As PerLanguageOption2(Of CodeStyleOption2(Of T)), codeStyle As CodeStyleOption2(Of T)) As IOptionsCollection
            Return OptionsSet(SingleOption(optionParam, codeStyle))
        End Function

        Friend Shadows Function [Option](Of T)(optionParam As Option2(Of T), value As T) As IOptionsCollection
            Return OptionsSet(SingleOption(optionParam, value))
        End Function

        Friend Shadows Function [Option](Of T)(optionParam As PerLanguageOption2(Of T), value As T) As IOptionsCollection
            Return OptionsSet(SingleOption(optionParam, value))
        End Function

        Friend Shared Shadows Function OptionsSet(ParamArray options As (OptionKey2, Object)()) As IOptionsCollection
            Return New OptionsCollection(LanguageNames.VisualBasic, options)
        End Function
#End If
    End Class
End Namespace
