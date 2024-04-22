' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
Imports Newtonsoft.Json.Linq

Namespace Roslyn.VisualStudio.VisualBasic.UnitTests.UnifiedSettings
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

        Friend Overrides Function GetEnumOptionValues([option] As IOption2) As Object()
            Dim allValues = [Enum].GetValues([option].Type).Cast(Of Object)
            If [option].Equals(CompletionOptionsStorage.SnippetsBehavior) Then
                'SnippetsRule.Default is used as a stub value, overridden per language at runtime.
                ' It is not shown in the option page
                Return allValues.Where(Function(value) Not value.Equals(SnippetsRule.Default)).ToArray()
            ElseIf [option].Equals(CompletionOptionsStorage.EnterKeyBehavior) Then
                ' EnterKeyRule.Default is used as a stub value, overridden per language at runtime.
                ' It Is Not shown in the option page
                Return allValues.Where(Function(value) Not value.Equals(EnterKeyRule.Default)).ToArray()
            End If

            Return MyBase.GetEnumOptionValues([option])
        End Function

        Friend Overrides Function GetOptionsDefaultValue([option] As IOption2) As Object
            Return MyBase.GetOptionsDefaultValue([option])
        End Function

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
                TestUnifiedSettingsCategory(registrationJsonObject, categoryBasePath:="textEditor.basic.intellisense", languageName:=LanguageNames.VisualBasic)
            End Using
        End Function
    End Class
End Namespace
