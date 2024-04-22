' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
Imports Newtonsoft.Json.Linq

Public Class VisualBasicUnifiedSettingsTests
    Inherits UnifiedSettingsTests

    Friend Overrides ReadOnly Property OnboardedOptions As ImmutableArray(Of IOption2)
        Get
            Return ImmutableArray.Create(Of IOption2)(
                CompletionOptionsStorage.TriggerOnTypingLetters,
                CompletionOptionsStorage.TriggerOnDeletion,
                CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
                CompletionViewOptionsStorage.ShowCompletionItemFilters,
                CompletionOptionsStorage.SnippetsBehavior,
                CompletionOptionsStorage.EnterKeyBehavior,
                CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
                CompletionViewOptionsStorage.EnableArgumentCompletionSnippets
                )
        End Get
    End Property

    <Fact>
    Public Async Function IntelliSensePageTests() As Task
        Dim registrationFileStream = GetType(VisualBasicUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("visualBasicSettings.registration.json")
        Using reader = New StreamReader(registrationFileStream)
            Dim registrationFile = Await reader.ReadToEndAsync().ConfigureAwait(False)
            Dim registrationJsonObject = JObject.Parse(registrationFile, New JsonLoadSettings())
            Dim categoriesTitle = registrationJsonObject.SelectToken("$.categories['textEditor.basic'].title")
            Assert.Equal("Visual Basic", categoriesTitle)
            Dim optionPageId = registrationJsonObject.SelectToken("$.categories['textEditor.basic.intellisense'].legacyOptionPageId")
            Assert.Equal(Guids.VisualBasicOptionPageIntelliSenseIdString, optionPageId.ToString())
        End Using
    End Function
End Class
