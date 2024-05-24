// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.UnifiedSettings
{
    public class CSharpUnifiedSettingsTests : UnifiedSettingsTests
    {
        internal override ImmutableArray<IOption2> OnboardedOptions => ImmutableArray.Create<IOption2>(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            CompletionOptionsStorage.TriggerOnDeletion,
            CompletionOptionsStorage.TriggerInArgumentLists,
            CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
            CompletionViewOptionsStorage.ShowCompletionItemFilters,
            CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon,
            CompletionOptionsStorage.SnippetsBehavior,
            CompletionOptionsStorage.EnterKeyBehavior,
            CompletionOptionsStorage.ShowNameSuggestions,
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            CompletionOptionsStorage.ShowNewSnippetExperienceUserOption
        );

        internal override object[] GetEnumOptionValues(IOption2 option)
        {
            var allValues = Enum.GetValues(option.Type).Cast<object>();
            if (option == CompletionOptionsStorage.SnippetsBehavior)
            {
                // SnippetsRule.Default is used as a stub value, overridden per language at runtime.
                // It is not shown in the option page
                return allValues.Where(value => !value.Equals(SnippetsRule.Default)).ToArray();
            }
            else if (option == CompletionOptionsStorage.EnterKeyBehavior)
            {
                // EnterKeyRule.Default is used as a stub value, overridden per language at runtime.
                // It is not shown in the option page
                return allValues.Where(value => !value.Equals(EnterKeyRule.Default)).ToArray();
            }

            return base.GetEnumOptionValues(option);
        }

        internal override object GetOptionsDefaultValue(IOption2 option)
        {
            // The default values of some options are set at runtime. option.defaultValue is just a dummy value in this case.
            // However, in unified settings we always set the correct value in registration.json.
            if (option == CompletionOptionsStorage.SnippetsBehavior)
            {
                // CompletionOptionsStorage.SnippetsBehavior's default value is SnippetsRule.Default.
                // It's overridden differently per-language at runtime.
                return SnippetsRule.AlwaysInclude;
            }
            else if (option == CompletionOptionsStorage.EnterKeyBehavior)
            {
                // CompletionOptionsStorage.EnterKeyBehavior's default value is EnterKeyBehavior.Default.
                // It's overridden differently per-language at runtime.
                return EnterKeyRule.Never;
            }
            else if (option == CompletionOptionsStorage.TriggerOnDeletion)
            {
                // CompletionOptionsStorage.TriggerOnDeletion's default value is null.
                // It's disabled by default for C#
                return false;
            }
            else if (option == CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces)
            {
                // CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces's default value is null
                // It's enabled by default for C#
                return true;
            }
            else if (option == CompletionViewOptionsStorage.EnableArgumentCompletionSnippets)
            {
                // CompletionViewOptionsStorage.EnableArgumentCompletionSnippets' default value is null
                // It's disabled by default for C#
                return false;
            }
            else if (option == CompletionOptionsStorage.ShowNewSnippetExperienceUserOption)
            {
                // CompletionOptionsStorage.ShowNewSnippetExperienceUserOption's default value is null.
                // It's in experiment, so disabled by default.
                return false;
            }

            return base.GetOptionsDefaultValue(option);
        }

        [Fact]
        public async Task IntelliSensePageTests()
        {
            using var registrationFileStream = typeof(CSharpUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.csharpSettings.registration.json");
            using var reader = new StreamReader(registrationFileStream);
            var registrationFile = await reader.ReadToEndAsync().ConfigureAwait(false);
            var registrationJsonObject = JObject.Parse(registrationFile, new JsonLoadSettings() { CommentHandling = CommentHandling.Ignore });
            var categoriesTitle = registrationJsonObject.SelectToken($"$.categories['textEditor.csharp'].title")!;
            Assert.Equal("C#", actual: categoriesTitle.ToString());
            var optionPageId = registrationJsonObject.SelectToken("$.categories['textEditor.csharp.intellisense'].legacyOptionPageId");
            Assert.Equal(Guids.CSharpOptionPageIntelliSenseIdString, optionPageId!.ToString());
            using var pkgdefFileStream = typeof(CSharpUnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.CSharp.UnitTests.PackageRegistration.pkgdef");
            using var pkgdefReader = new StreamReader(pkgdefFileStream);
            var pkgdefFile = await pkgdefReader.ReadToEndAsync().ConfigureAwait(false);
            TestUnifiedSettingsCategory(registrationJsonObject, categoryBasePath: "textEditor.csharp.intellisense", languageName: LanguageNames.CSharp, pkgdefFile);
        }
    }
}
