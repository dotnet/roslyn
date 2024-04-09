' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Options
Imports Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage
Imports Newtonsoft.Json.Linq
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
    Partial Public MustInherit Class UnifiedSettingsTests
        ' Onboarded options in Unified Settings registration file
        Friend MustOverride ReadOnly Property OnboardedOptions As ImmutableArray(Of IOption2)

        ' The default value of some enum options is overridden at runtime.
        ' It could be
        ' 1: Option has different default value for C# and VB.
        ' 2: Option is in experiment, so it is set to null and override to a value.
        ' But in unified settings we always use the correct value for language.
        ' Use this dictionary to indicate that value in unit test.
        Friend MustOverride ReadOnly Property OptionsToDefaultValue As ImmutableDictionary(Of IOption2, Object)

        ' The default value of some enum options is overridden at runtime. And it's not shown in the unified settings page.
        ' Use this dictionary to list all the possible enum values in settings page.
        Friend MustOverride ReadOnly Property EnumOptionsToValues As ImmutableDictionary(Of IOption2, ImmutableArray(Of Object))

        Protected Sub TestIntelliSensePageSettings(registrationJsonObject As JObject)
            For Each onboardedOption In OnboardedOptions
                Dim optionName = onboardedOption.Definition.ConfigName
                Dim settingStorage As UnifiedSettingsStorage = Nothing
                If s_unifiedSettingsStorage.TryGetValue(optionName, settingStorage) Then
                    Dim unifiedSettingsPath = settingStorage.GetUnifiedSettingsPath(LanguageNames.CSharp)
                    VerifyType(registrationJsonObject, unifiedSettingsPath, onboardedOption)
                    If onboardedOption.Type.IsEnum Then
                        ' Enum settings contains special setup.
                        VerifyEnum(registrationJsonObject, unifiedSettingsPath, onboardedOption)
                    Else
                        VerifySettings(registrationJsonObject, unifiedSettingsPath, onboardedOption)
                    End If
                Else
                    ' Can't find the option in the storage dictionary
                    Throw ExceptionUtilities.UnexpectedValue(optionName)
                End If
            Next
        End Sub

        Private Sub VerifySettings(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2)
            ' Verify default value
            Dim actualDefaultValue = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').default")
            Dim perLangDefaultValue As Object = Nothing
            Assert.Equal(If(OptionsToDefaultValue.TryGetValue([option], perLangDefaultValue),
                            perLangDefaultValue.ToString().ToCamelCase(),
                            [option].Definition.DefaultValue?.ToString().ToCamelCase()), actualDefaultValue.ToString().ToCamelCase())

            VerifyMigration(registrationJsonObject, unifiedSettingPath, [option])
        End Sub

        Private Sub VerifyEnum(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2)
            Dim actualDefaultValue = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').default")
            Dim perLangDefaultValue As Object = Nothing
            Assert.Equal(If(OptionsToDefaultValue.TryGetValue([option], perLangDefaultValue),
                            perLangDefaultValue.ToString().ToCamelCase(),
                            [option].Definition.DefaultValue?.ToString()), actualDefaultValue.ToString().ToCamelCase())

            Dim actualEnumValues = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').enum").SelectAsArray(Function(token) token.ToString())
            Dim possibleEnumValues As ImmutableArray(Of Object) = ImmutableArray(Of Object).Empty
            Dim expectedEnumValues = If(EnumOptionsToValues.TryGetValue([option], possibleEnumValues),
                                         possibleEnumValues.SelectAsArray(Function(value) value.ToString().ToCamelCase()),
                                         [option].Type.GetEnumValues().Cast(Of String).SelectAsArray(Function(value) value.ToCamelCase()))
            AssertEx.Equal(expectedEnumValues, actualEnumValues)
            VerifyEnumMigration(registrationJsonObject, unifiedSettingPath, [option])
        End Sub

        Private Shared Sub VerifyType(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2)
            Dim actualType = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].type")
            Dim expectedType = [option].Definition.Type
            If expectedType.IsEnum Then
                ' Enum is string in json
                Assert.Equal("string", actualType.ToString())
            Else
                Dim expectedTypeName = ConvertTypeNameToJsonType([option].Definition.Type)
                Assert.Equal(expectedTypeName, actualType.ToString())
            End If
        End Sub

        Private Shared Function ConvertTypeNameToJsonType(optionType As Type) As String
            Dim underlyingType = Nullable.GetUnderlyingType(optionType)
            ' If the type is Nullable type, its mapping type in unified setting page would be the normal type
            ' These options would need to change to non-nullable form
            ' See https://github.com/dotnet/roslyn/issues/69367
            If underlyingType Is Nothing Then
                Return optionType.Name.ToCamelCase()
            Else
                Return underlyingType.Name.ToCamelCase()
            End If
        End Function

        Private Sub VerifyEnumMigration(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2)
            Dim actualMigration = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration")
            Dim migrationProperty = DirectCast(actualMigration.Children().Single(), JProperty)
            Dim migrationType = migrationProperty.Name
            Assert.Equal("enumIntegerToString", migrationType)

            ' Verify input node and map node
            Dim input = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration.enumIntegerToString.input")
            VerifyInput(input, [option])
            VerifyEnumToIntegerMappings(registrationJsonObject, unifiedSettingPath, [option])
        End Sub

        Private Shared Sub VerifyMigration(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2)
            Dim actualMigration = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration")
            ' Get the single property under migration
            Dim migrationProperty = DirectCast(actualMigration.Children().Single(), JProperty)
            Dim migrationType = migrationProperty.Name
            If migrationType = "pass" Then
                ' Verify input node
                Dim input = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration.pass.input")
                VerifyInput(input, [option])
            Else
                ' Need adding more migration types if new type is added
                Throw ExceptionUtilities.UnexpectedValue(migrationType)
            End If
        End Sub

        ' Verify input property under migration
        Private Shared Sub VerifyInput(input As JToken, [option] As IOption2)
            Dim store = input.SelectToken("store").ToString()
            Dim path = input.SelectToken("path").ToString()
            Dim configName = [option].Definition.ConfigName
            Dim visualStudioStorage = Storages(configName)
            If TypeOf visualStudioStorage Is VisualStudioOptionStorage.RoamingProfileStorage Then
                Dim roamingProfileStorage = DirectCast(visualStudioStorage, VisualStudioOptionStorage.RoamingProfileStorage)
                Assert.Equal("SettingsManager", store)
                Assert.Equal(roamingProfileStorage.Key.Replace("%LANGUAGE%", "CSharp"), path)
            Else
                ' Not supported yet
                Throw ExceptionUtilities.Unreachable
            End If
        End Sub

        Private Sub VerifyEnumToIntegerMappings(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2)
            ' Here we are going to verify a structure like this:
            ' "map": [
            ' {
            '     "result": "neverInclude",
            '     "match": 1
            ' },
            ' // '0' matches to SnippetsRule.Default. Means the behavior is decided by language.
            ' // '2' matches to SnippetsRule.AlwaysInclude. It's the default behavior for C#
            ' // Put both mapping here, so it's possible for unified setting to load '0' from the storage.
            ' // Put '2' in front, so unified settings would persist '2' to storage when 'alwaysInclude' is selected.
            ' {
            '     "result": "alwaysInclude",
            '     "match": 2
            ' },
            ' {
            '     "result": "alwaysInclude",
            '     "match": 0
            ' },
            ' {
            '     "result": "includeAfterTypingIdentifierQuestionTab",
            '     "match": 3
            ' }
            ' ]
            Dim actualMappings = CType(registrationJsonObject.SelectToken(String.Format("$.properties['{0}'].migration.enumIntegerToString.map", unifiedSettingPath)), JArray).Select(Function(mapping) (mapping("result").ToString(), Integer.Parse(mapping("match").ToString()))).ToArray()

            Dim enumValues = [option].Type.GetEnumValues().Cast(Of Object).ToDictionary(
                keySelector:=Function(enumValue) enumValue.ToString().ToCamelCase(),
                elementSelector:=Function(enumValue)
                                     Dim actualDefaultValue As Object = Nothing
                                     If OptionsToDefaultValue.TryGetValue([option], actualDefaultValue) AndAlso actualDefaultValue.Equals(enumValue) Then
                                         ' This value is the real default value at runtime.
                                         ' So map it to both default value and its own value.
                                         ' Like 'alwaysInclude' in the above example, it would map to both 0 and 2.
                                         Return New Integer() {CInt(enumValue), CInt([option].DefaultValue)}
                                     End If

                                     Return New Integer() {CInt(enumValue)}
                                 End Function
                )

            For Each tuple In actualMappings
                Dim result = tuple.Item1
                Dim match = tuple.Item2
                Dim acceptableValues = enumValues(result)
                Assert.Contains(match, acceptableValues)
            Next

            ' If the default value of the enum is a stub value, verify the real value mapping is put in font of the default value mapping.
            ' It makes sure the default value would be converted to the real value by unified settings engine.
            Dim realDefaultValue As Object = Nothing
            If OptionsToDefaultValue.TryGetValue([option], realDefaultValue) Then
                Dim indexOfTheRealDefaultMapping = Array.IndexOf(actualMappings, (realDefaultValue.ToString().ToCamelCase(), CInt(realDefaultValue)))
                Assert.NotEqual(-1, indexOfTheRealDefaultMapping)
                Dim indexOfTheDefaultMapping = Array.IndexOf(actualMappings, (realDefaultValue.ToString().ToCamelCase(), CInt([option].DefaultValue)))
                Assert.NotEqual(-1, indexOfTheDefaultMapping)
                Assert.True(indexOfTheRealDefaultMapping < indexOfTheDefaultMapping)
            End If
        End Sub
    End Class
End Namespace
