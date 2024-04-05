// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Snippets;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.CallHierarchy.Package.Definitions;
using Microsoft.VisualStudio.LanguageServices.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.UnifiedSettings
{
    public class CSharpUnifiedSettingsTests
    {
        private readonly static ImmutableArray<IOption2> s_onboardedOptions = ImmutableArray.Create<IOption2>(CompletionOptionsStorage.TriggerOnTypingLetters);

        [Fact]
        public async Task CSharpIntellisensePageTest()
        {
            var registrationFileStream = typeof(CSharpUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.csharpSettings.registration.json");
            using var reader = new StreamReader(registrationFileStream!);
            var registrationFile = await reader.ReadToEndAsync().ConfigureAwait(false);
            var registrationJsonObject = JObject.Parse(registrationFile, new JsonLoadSettings() { CommentHandling = CommentHandling.Ignore });

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
                    VerifyDefaultValue(registrationJsonObject, unifiedSettingsPath, option);
                }
                else
                {
                    // Can't find the option in the storage dictionary
                    throw ExceptionUtilities.UnexpectedValue(optionName);
                }
            }
        }

        private static void VerifyType(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option)
        {
            var actualType = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].type")!;
            var expectedType = ConvertTypeNameToJsonType(option.Definition.Type.Name);
            Assert.Equal(expectedType, actualType.ToString());

            static string ConvertTypeNameToJsonType(string typeName)
                => typeName switch
                {
                    "Boolean" => "boolean",
                    _ => throw ExceptionUtilities.Unreachable()
                };
        }

        private static void VerifyDefaultValue(JObject registrationJsonObject, string unifiedSettingPath, IOption2 option, string? alternateDefaultOnNull = null)
        {
            var actualDefaultValue = registrationJsonObject.SelectToken($"$.properties['{unifiedSettingPath}'].default")!;
            var expectedDefaultValue = option.Definition.DefaultValue?.ToString() ?? alternateDefaultOnNull;
            Assert.Equal(actualDefaultValue, expectedDefaultValue);
        }

        private static void VerifyMigration()
        {

        }
    }
}
