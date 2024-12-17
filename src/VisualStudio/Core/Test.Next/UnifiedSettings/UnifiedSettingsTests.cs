// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public class UnifiedSettingsTests
{
    internal static readonly ImmutableDictionary<IOption2, string> s_optionToUnifiedSettingPath = ImmutableDictionary<IOption2, string>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, "textEditor.basic.intellisense.triggerCompletionOnTypingLetters");

    private static readonly ImmutableDictionary<IOption2, UnifiedSettingBase> s_optionToExpectedUnifiedSettings = ImmutableDictionary<IOption2, UnifiedSettingBase>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, UnifiedSettingBase.CreateOption(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            title: "Show completion list after a character is typed",
            order: 0,
            defaultValue: true,
            featureFlagAndExperimentValue: default,
            enableWhenOptionAndValue: default,
            languageName: LanguageNames.VisualBasic));

    [Fact]
    public async Task VisualBasicIntellisenseTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
        using var pkgDefFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var properties = jsonDocument!.Root["properties"];
    }
}
