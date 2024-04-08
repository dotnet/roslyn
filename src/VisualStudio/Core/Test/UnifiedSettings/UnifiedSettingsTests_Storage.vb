' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
    Partial Public Class UnifiedSettingsTests
        Private Shared ReadOnly s_onboardedOptions As ImmutableArray(Of IOption2) = ImmutableArray.Create(Of IOption2)(
                CompletionOptionsStorage.TriggerOnTypingLetters,
                CompletionOptionsStorage.TriggerOnDeletion,
                CompletionOptionsStorage.SnippetsBehavior)

        ' Summary:
        ' The default value of some enum options is overridden at runtime. It uses different default value for C# and VB.
        ' But in unified settings we always use the correct value for language.
        ' Use this dictionary to indicate that value in unit test.
        Private Shared ReadOnly s_optionsToDefaultValue As ImmutableDictionary(Of IOption2, Object) = ImmutableDictionary(Of IOption2, Object).Empty.
                Add(CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude)

        ' Summary:
        ' The default value of some enum options is overridden at runtime. And it's not shown in the unified settings page.
        ' Use this dictionary to list all the possible enum values in settings page.
        Private Shared ReadOnly s_enumOptionsToValues As ImmutableDictionary(Of IOption2, ImmutableArray(Of Object)) = ImmutableDictionary(Of IOption2, ImmutableArray(Of Object)).Empty.
                Add(CompletionOptionsStorage.SnippetsBehavior, ImmutableArray.Create(Of Object)(SnippetsRule.NeverInclude, SnippetsRule.AlwaysInclude, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab))

        Private Shared ReadOnly s_unifiedSettingsStorage As New Dictionary(Of String, UnifiedSettingsStorage)() From {
            {"dotnet_trigger_completion_on_typing_letters", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.triggerCompletionOnTypingLetters")},
            {"dotnet_trigger_completion_on_deletion", New UnifiedSettingsStorage("textEditor.%LANGUAGE%..intellisense.triggerCompletionOnDeletion")},
            {"dotnet_snippets_behavior", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.triggerCompletionOnTypingLetters")}
        }

        Friend NotInheritable Class UnifiedSettingsStorage
            Private Const LanguagePlaceholder As String = "%LANGUAGE%"

            ' C# name used in Unified Settings path.
            Private Const csharpKey As String = "csharp"

            ' Visual Basic name used in Unified Settings path.
            Private Const visualBasicKey As String = "basic"

            ' Unified settings base path, might contains %LANGAUGE% if it maps to two per-language different setting.
            Public Property UnifiedSettingsBasePath As String

            Public Sub New(unifiedSettingsPath As String)
                UnifiedSettingsBasePath = unifiedSettingsPath
            End Sub

            Public Function GetUnifiedSettingsPath(language As String) As String
                If Not UnifiedSettingsBasePath.Contains(LanguagePlaceholder) Then
                    Return UnifiedSettingsBasePath
                End If

                Select Case language
                    Case LanguageNames.CSharp
                        Return UnifiedSettingsBasePath.Replace(LanguagePlaceholder, csharpKey)
                    Case LanguageNames.VisualBasic
                        Return UnifiedSettingsBasePath.Replace(LanguagePlaceholder, visualBasicKey)
                    Case Else
                        Throw New Exception("Unexpected language value")
                End Select
            End Function
        End Class
    End Class
End Namespace
