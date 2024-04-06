// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.UnifiedSettings
{
    public class CSharpUnifiedSettingsTests
    {
        private static readonly ImmutableArray<IOption2> s_onboardedOptions = ImmutableArray.Create<IOption2>(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            CompletionOptionsStorage.SnippetsBehavior);

        /// <summary>
        /// Some options use different default value for C# and VB. The default value in OptionConfig is just a stub value.
        /// The real value is set at runtime. But in unified settings we always use the correct value for language.
        /// Use this dictionary to indicate that in unit test.
        /// </summary>
        private static readonly ImmutableDictionary<IOption2, object> s_optionsToDefaultValue = ImmutableDictionary<IOption2, object>.Empty
                .Add(CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude.ToString().ToCamelCase());

        [Fact]
        public async Task IntellisensePageSettingsTest()
        {
            var registrationFileStream = typeof(CSharpUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.csharpSettings.registration.json");
            using var reader = new StreamReader(registrationFileStream!);
            var registrationFile = await reader.ReadToEndAsync().ConfigureAwait(false);
            var registrationJsonObject = JObject.Parse(registrationFile, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore });

            var categoriesTitle = registrationJsonObject.SelectToken("$.categories['textEditor.csharp'].title")!;
            Assert.Equal("C#", categoriesTitle.ToString());
            var optionPageId = registrationJsonObject.SelectToken("$.categories['textEditor.csharp.intellisense'].legacyOptionPageId")!;
            Assert.Equal(Microsoft.VisualStudio.LanguageServices.Guids.CSharpOptionPageIntelliSenseIdString, optionPageId.ToString());

            foreach (var option in s_onboardedOptions)
            {
                var optionName = option.Definition.ConfigName;
                if (VisualStudioOptionStorage.UnifiedSettingsStorages.TryGetValue(optionName, out var unifiedSettingsStorage))
                {
                    var unifiedSettingsPath = unifiedSettingsStorage.GetUnifiedSettingsPath(LanguageNames.CSharp);
                    VerifyType(registrationJsonObject, unifiedSettingsPath, option);
                    if (option.Type.IsEnum)
                    {
                        // VerifyEnum(registrationJsonObject, unifiedSettingsPath, option);
                    }
                    else
                    {
                        VerifySettings(registrationJsonObject, unifiedSettingsPath, option);
                    }
                }
                else
                {
                    // Can't find the option in the storage dictionary
                    throw ExceptionUtilities.UnexpectedValue(optionName);
                }
            }
        }

        private static void VerifySettings(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option)
        {
            // Verify default value
            var actualDefaultValue = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].default")!;
            var expectedDefaultValue = option.Definition.DefaultValue?.ToString();
            Assert.Equal(actualDefaultValue.ToString(), expectedDefaultValue);
            VerifyMigration(registrationJsonObject, unifiedSettingPath, option);
        }

        private static void VerifyEnum(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option)
        {
            var enumArray = registrationJsonObject.SelectTokens($"$.properties['{unifiedSettingPath}'].enum");

        }

        private static void VerifyType(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option)
        {
            var actualType = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].type")!;
            var expectedType = option.Definition.Type;
            if (expectedType.IsEnum)
            {
                // Enum is string in json
                Assert.Equal("string", actualType.ToString());
            }
            else
            {
                var expectedTypeName = ConvertTypeNameToJsonType(option.Definition.Type.Name);
                Assert.Equal(expectedTypeName, actualType.ToString());
            }

            static string ConvertTypeNameToJsonType(string typeName)
                => typeName switch
                {
                    "Boolean" => "boolean",
                    _ => typeName
                };
        }

        private static void VerifyDefaultValue(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option)
        {
            var actualDefaultValue = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].default")!;
            if (s_optionsToDefaultValue.TryGetValue(option, out var perLangDefaultValue))
            {
                Assert.Equal(actualDefaultValue.ToString(), perLangDefaultValue.ToString());
            }
            else
            {
                var expectedDefaultValue = option.Definition.DefaultValue?.ToString();
                Assert.Equal(actualDefaultValue.ToString(), expectedDefaultValue);
            }
        }

        private static void VerifyMigration(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option)
        {
            var actualMigration = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].migration")!;
            // Get the single property under migration
            var migrationProperty = (JProperty)actualMigration.Children().Single();
            var migrationType = migrationProperty.Name;
            if (migrationType is "pass")
            {
                // "migration": {
                //   "pass": {
                //     "input": {
                //      }
                //   }
                // }
                // Verify input node
                var input = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].migration.pass.input")!;
                VerifyInput(input, option);
            }
            else if (migrationType is "enumIntegerToString")
            {
                // "migration": {
                //   "enumIntegerToString": {
                //     "input": {
                //        "store": xxxx,
                //        "path": yyyy
                //      },
                //     "map": {
                //      // Enum mappings
                //     }
                //   }
                // }
                // Verify input node and map node
                var input = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].migration.enumIntegerToString.input")!;
                VerifyInput(input, option);
                VerifyEnumIntegerToStringMigration(registrationJsonObject, unifiedSettingPath, option);
            }
            else
            {
                // Need adding more migration types if new type is added
                throw ExceptionUtilities.UnexpectedValue(migrationType);
            }

            // Verify input property under migration
            // "input": {
            //    "store": "xxxx",
            //    "path": "yyyy"
            // }
            static void VerifyInput(JToken input, IOption2 option)
            {
                var store = input.SelectToken("store")!.ToString();
                var path = input.SelectToken("path")!.ToString();
                var configName = option.Definition.ConfigName;
                var visualStudioStorage = VisualStudioOptionStorage.Storages[configName];
                if (visualStudioStorage is VisualStudioOptionStorage.RoamingProfileStorage roamingProfileStorage)
                {
                    Assert.Equal("SettingsManager", store);
                    Assert.Equal(roamingProfileStorage.Key.Replace("%LANGUAGE%", "CSharp"), path);
                }
                else
                {
                    // Not supported yet
                    throw ExceptionUtilities.Unreachable();
                }
            }

            static void VerifyEnumIntegerToStringMigration(JToken registrationJsonObject, string unifiedSettingPath, IOption2 option)
            {
                var input = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].migration.pass.input")!;
            }
        }
    }
}
