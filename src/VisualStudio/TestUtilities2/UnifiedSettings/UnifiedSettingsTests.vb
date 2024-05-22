' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO.Hashing
Imports System.Text
Imports System.Text.RegularExpressions
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

        ' Override this method to if the option use different default value.
        Friend Overridable Function GetOptionsDefaultValue([option] As IOption2) As Object
            Return [option].DefaultValue
        End Function

        ' Override this method to specify all possible enum values in option page.
        Friend Overridable Function GetEnumOptionValues([option] As IOption2) As Object()
            Dim type = [option].Definition.Type
            Assert.True(type.IsEnum)
            Return [Enum].GetValues(type).Cast(Of Object).AsArray()
        End Function

        Protected Sub TestUnifiedSettingsCategory(registrationJsonObject As JObject, categoryBasePath As String, languageName As String, pkdDefFile As String)
            Dim actualAllSettings = registrationJsonObject.SelectToken($"$.properties").Children.OfType(Of JProperty).
                Where(Function(setting) setting.Name.StartsWith(categoryBasePath)).
                Select(Function(setting) setting.Name).
                OrderBy(Function(name) name).
                ToArray()

            Dim expectedAllSettings = OnboardedOptions.Select(Function(onboardedOption) s_unifiedSettingsStorage(onboardedOption.Definition.ConfigName).GetUnifiedSettingsPath(languageName)).
                OrderBy(Function(name) name).
                ToArray()
            Assert.Equal(expectedAllSettings, actualAllSettings)

            For Each onboardedOption In OnboardedOptions
                Dim optionName = onboardedOption.Definition.ConfigName
                Dim settingStorage As UnifiedSettingsStorage = Nothing
                If s_unifiedSettingsStorage.TryGetValue(optionName, settingStorage) Then
                    Dim unifiedSettingsPath = settingStorage.GetUnifiedSettingsPath(languageName)
                    VerifyType(registrationJsonObject, unifiedSettingsPath, onboardedOption)

                    Dim expectedDefaultValue = GetOptionsDefaultValue(onboardedOption)
                    Dim actualDefaultValue = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingsPath}').default")
                    Assert.Equal(expectedDefaultValue.ToString().ToCamelCase(), actualDefaultValue.ToString().ToCamelCase())

                    If onboardedOption.Type.IsEnum Then
                        ' Enum settings contains special setup.
                        VerifyEnum(registrationJsonObject, unifiedSettingsPath, onboardedOption, languageName)
                    Else
                        VerifySettings(registrationJsonObject, unifiedSettingsPath, onboardedOption, languageName)
                    End If
                Else
                    ' Can't find the option in the storage dictionary
                    Throw ExceptionUtilities.UnexpectedValue(optionName)
                End If
            Next

            Dim registrationFileBytes = ASCIIEncoding.ASCII.GetBytes(registrationJsonObject.ToString())
            Dim hash = XxHash128.Hash(registrationFileBytes)
            Dim tagBytes = hash.Take(8).ToArray()
            Dim expectedCacheTagValue = BitConverter.ToInt64(tagBytes, 0).ToString("X16")

            Dim regexExp = New Regex("""CacheTag""=qword:\w{16}")
            Dim match = regexExp.Match(pkdDefFile, 0).Value
            Dim actual = match.Substring(match.Length - 16)
            ' Please change the CacheTag value in pkddef if you modify the unified settings regirstration file
            Assert.Equal(expectedCacheTagValue, actual)
        End Sub

        Private Shared Sub VerifySettings(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2, languageName As String)
            VerifyMigration(registrationJsonObject, unifiedSettingPath, [option], languageName)
        End Sub

        Private Sub VerifyEnum(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2, languageName As String)
            Dim actualEnumValues = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').enum").Select(Function(token) token.ToString()).OrderBy(Function(value) value)
            Dim expectedEnumValues = GetEnumOptionValues([option]).Select(Function(value) value.ToString().ToCamelCase()).OrderBy(Function(value) value)
            AssertEx.Equal(expectedEnumValues, actualEnumValues)
            VerifyEnumMigration(registrationJsonObject, unifiedSettingPath, [option], languageName)
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

        Private Sub VerifyEnumMigration(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2, languageName As String)
            Dim actualMigration = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration")
            Dim migrationProperty = DirectCast(actualMigration.Children().Single(), JProperty)
            Dim migrationType = migrationProperty.Name
            Assert.Equal("enumIntegerToString", migrationType)

            ' Verify input node and map node
            Dim input = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration.enumIntegerToString.input")
            VerifyInput(input, [option], languageName)
            VerifyEnumToIntegerMappings(registrationJsonObject, unifiedSettingPath, [option])
        End Sub

        Private Shared Sub VerifyMigration(registrationJsonObject As JObject, unifiedSettingPath As String, [option] As IOption2, languageName As String)
            Dim actualMigration = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration")
            ' Get the single property under migration
            Dim migrationProperty = DirectCast(actualMigration.Children().Single(), JProperty)
            Dim migrationType = migrationProperty.Name
            If migrationType = "pass" Then
                ' Verify input node
                Dim input = registrationJsonObject.SelectToken($"$.properties('{unifiedSettingPath}').migration.pass.input")
                VerifyInput(input, [option], languageName)
            Else
                ' Need adding more migration types if new type is added
                Throw ExceptionUtilities.UnexpectedValue(migrationType)
            End If
        End Sub

        ' Verify input property under migration
        Private Shared Sub VerifyInput(input As JToken, [option] As IOption2, languageName As String)
            Dim store = input.SelectToken("store").ToString()
            Dim path = input.SelectToken("path").ToString()
            Dim configName = [option].Definition.ConfigName
            Dim visualStudioStorage = Storages(configName)
            If TypeOf visualStudioStorage Is VisualStudioOptionStorage.RoamingProfileStorage Then
                Dim roamingProfileStorage = DirectCast(visualStudioStorage, VisualStudioOptionStorage.RoamingProfileStorage)
                Assert.Equal("SettingsManager", store)
                Assert.Equal(roamingProfileStorage.Key.Replace("%LANGUAGE%", GetSubstituteLanguage(languageName)), path)
            Else
                ' Not supported yet
                Throw ExceptionUtilities.Unreachable
            End If
        End Sub

        Private Shared Function GetSubstituteLanguage(languageName As String) As String
            Select Case languageName
                Case LanguageNames.CSharp
                    Return "CSharp"
                Case LanguageNames.VisualBasic
                    Return "VisualBasic"
                Case Else
                    Return languageName
            End Select
        End Function

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
                                     Dim actualDefaultValue = GetOptionsDefaultValue([option])
                                     If actualDefaultValue.Equals(enumValue) Then
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
            Dim realDefaultValue = GetOptionsDefaultValue([option])
            Dim indexOfTheRealDefaultMapping = Array.IndexOf(actualMappings, (realDefaultValue.ToString().ToCamelCase(), CInt(realDefaultValue)))
            Assert.NotEqual(-1, indexOfTheRealDefaultMapping)
            Dim indexOfTheDefaultMapping = Array.IndexOf(actualMappings, (realDefaultValue.ToString().ToCamelCase(), CInt([option].DefaultValue)))
            Assert.NotEqual(-1, indexOfTheDefaultMapping)
            Assert.True(indexOfTheRealDefaultMapping < indexOfTheDefaultMapping)
        End Sub
    End Class
End Namespace
