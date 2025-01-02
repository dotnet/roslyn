' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
    Partial Public Class UnifiedSettingsTests

        ' Mapping from the config name to its path in Unified Settings registration file
        Private Shared ReadOnly s_unifiedSettingsStorage As New Dictionary(Of String, UnifiedSettingsStorage)() From {
            {"dotnet_trigger_completion_on_typing_letters", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.triggerCompletionOnTypingLetters")},
            {"dotnet_trigger_completion_on_deletion", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.triggerCompletionOnDeletion")},
            {"dotnet_trigger_completion_in_argument_lists", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.triggerCompletionInArgumentLists")},
            {"dotnet_highlight_matching_portions_of_completion_list_items", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.highlightMatchingPortionsOfCompletionListItems")},
            {"dotnet_show_completion_item_filters", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.showCompletionItemFilters")},
            {"csharp_complete_statement_on_semicolon", New UnifiedSettingsStorage("textEditor.csharp.intellisense.completeStatementOnSemicolon")},
            {"dotnet_snippets_behavior", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.snippetsBehavior")},
            {"dotnet_return_key_completion_behavior", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.returnKeyCompletionBehavior")},
            {"dotnet_show_name_completion_suggestions", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.showNameCompletionSuggestions")},
            {"dotnet_show_completion_items_from_unimported_namespaces", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.showCompletionItemsFromUnimportedNamespaces")},
            {"dotnet_enable_argument_completion_snippets", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.enableArgumentCompletionSnippets")},
            {"dotnet_show_new_snippet_experience", New UnifiedSettingsStorage("textEditor.%LANGUAGE%.intellisense.showNewSnippetExperience")}
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
