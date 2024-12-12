' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
Imports Newtonsoft.Json
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

        Private Shared ReadOnly Property IntelliSenseOnboardedOptions As ImmutableArray(Of (unifiedSettingsPath As String, roslynOption As IOption2))
            Get
                Return ImmutableArray.Create(Of (String, IOption2))(
                ("textEditor.basic.intellisense.triggerCompletionOnTypingLetters", CompletionOptionsStorage.TriggerOnTypingLetters),
                ("textEditor.basic.intellisense.triggerCompletionOnDeletion", CompletionOptionsStorage.TriggerOnDeletion),
                ("textEditor.basic.intellisense.highlightMatchingPortionsOfCompletionListItems", CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems),
                ("textEditor.basic.intellisense.showCompletionItemFilters", CompletionViewOptionsStorage.ShowCompletionItemFilters),
                ("textEditor.basic.intellisense.snippetsBehavior", CompletionOptionsStorage.SnippetsBehavior),
                ("textEditor.basic.intellisense.returnKeyCompletionBehavior", CompletionOptionsStorage.EnterKeyBehavior),
                ("textEditor.basic.intellisense.showCompletionItemsFromUnimportedNamespaces", CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces),
                ("textEditor.basic.intellisense.enableArgumentCompletionSnippets", CompletionViewOptionsStorage.EnableArgumentCompletionSnippets))
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
            ' The default values of some options are set at runtime. option.defaultValue is just a dummy value in this case.
            ' However, in unified settings we always set the correct value in registration.json.
            If [option].Equals(CompletionOptionsStorage.SnippetsBehavior) Then
                ' CompletionOptionsStorage.SnippetsBehavior's default value is SnippetsRule.Default.
                ' It's overridden differently per-language at runtime.
                Return SnippetsRule.IncludeAfterTypingIdentifierQuestionTab
            ElseIf [option].Equals(CompletionOptionsStorage.EnterKeyBehavior) Then
                ' CompletionOptionsStorage.EnterKeyBehavior's default value is EnterKeyBehavior.Default.
                ' It's overridden differently per-language at runtime.
                Return EnterKeyRule.Always
            ElseIf [option].Equals(CompletionOptionsStorage.TriggerOnDeletion) Then
                ' CompletionOptionsStorage.TriggerOnDeletion's default value is null.
                ' It's enabled by default for Visual Basic
                Return True
            ElseIf [option].Equals(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces) Then
                ' CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces's default value is null
                ' It's enabled by default for Visual Basic
                Return True
            ElseIf [option].Equals(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets) Then
                ' CompletionViewOptionsStorage.EnableArgumentCompletionSnippets' default value is null
                ' It's disabled by default for Visual Basic
                Return False
            End If

            Return MyBase.GetOptionsDefaultValue([option])
        End Function

        <Fact>
        Public Async Function CategoriesTest() As Task
            Using registrationFileStream = GetType(VisualBasicUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("visualBasicSettings.registration.json")
                Using reader = New StreamReader(registrationFileStream)
                    Dim registrationFile = Await reader.ReadToEndAsync()
                    Dim registrationJsonObject = JObject.Parse(registrationFile, New JsonLoadSettings() With {.CommentHandling = CommentHandling.Ignore})
                    Dim categories = registrationJsonObject.Property("categories")
                    Dim nameToCategories = categories.Value.Children(Of JProperty).ToDictionary(
                        Function(jProperty) jProperty.Name,
                        Function(jProperty) jProperty.Value.ToObject(Of UnifiedSettingsCategory))

                    Assert.True(nameToCategories.ContainsKey("textEditor.basic"))
                    Assert.Equal("Visual Basic", nameToCategories("textEditor.basic").Title)

                    Assert.True(nameToCategories.ContainsKey("textEditor.basic.intellisense"))
                    Assert.Equal("IntelliSense", nameToCategories("textEditor.basic.intellisense").Title)
                End Using
            End Using
        End Function

        '<Fact>
        'Public Async Function IntelliSensePageTest() As Task
        '    Using registrationFileStream = GetType(VisualBasicUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("visualBasicSettings.registration.json")
        '        Using pkgDefFileStream = GetType(VisualBasicUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("PackageRegistration.pkgdef")
        '            Using pkgDefFileReader = New StreamReader(pkgDefFileStream)
        '                Dim pkgDefFile = Await pkgDefFileReader.ReadToEndAsync().ConfigureAwait(False)
        '                Dim parseOption = New JsonDocumentOptions() With {
        '                        .CommentHandling = JsonCommentHandling.Skip
        '                        }
        '                Dim registrationDocument = Await JsonDocument.ParseAsync(registrationFileStream, parseOption)
        '                Dim properties = registrationDocument.RootElement.GetProperty("properties")
        '                Assert.NotNull(properties)

        '                Dim expectedGroupPrefix = "textEditor.basic.intellisense"
        '                Dim actualOptions = properties.EnumerateObject.Where(Function([property]) [property].Name.StartsWith(expectedGroupPrefix)).ToImmutableArray()
        '                Assert.Equal(IntelliSenseOnboardedOptions.Length, actualOptions.Length)

        '                For Each optionPair In actualOptions.Zip(IntelliSenseOnboardedOptions, Function(actual, expected) (actual, expected))
        '                    Dim expected = optionPair.expected
        '                    Dim actualName = optionPair.actual.Name

        '                Next
        '            End Using
        '        End Using
        '    End Using
        'End Function

        'Private Sub Helper(expected As (unifiedSettingsPath As String, roslynOption As IOption2), actualProperty As JsonProperty)
        '    Assert.Equal(expected.unifiedSettingsPath, actualProperty.Name)
        '    Dim expectedOption = expected.roslynOption
        '    Dim type = expectedOption.Type
        '    If type = GetType(Boolean) Then
        '        VerifyBooleanOption(expectedOption, actualProperty.Value.Deserialize(Of UnifiedSettingsOption(Of Boolean)))
        '    ElseIf type.IsEnum Then
        '        VerifyEnumOption(expectedOption, actualProperty.Value.Deserialize(Of UnifiedSettingsEnumOption))
        '    Else
        '        Assert.Fail("We only have enum and boolean option now. Add more if needed")
        '    End If
        'End Sub
    End Class
End Namespace
