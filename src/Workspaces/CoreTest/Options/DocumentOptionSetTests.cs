// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Options;

public sealed class DocumentOptionSetTests
{
    [Fact]
    public void GetOption()
    {
        var underlyingSet = new TestOptionSet(ImmutableDictionary<OptionKey, object?>.Empty.Add(
            new OptionKey(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp),
            new CodeStyleOption<bool>(true, NotificationOption.Error)));

        var configOptions = StructuredAnalyzerConfigOptions.Create(ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer).Add(
            "dotnet_style_qualification_for_event", "true:warning"));

        var set = new DocumentOptionSet(configOptions, underlyingSet, LanguageNames.CSharp);

        // option stored in analyzer config:
        Assert.Equal(new CodeStyleOption<bool>(true, NotificationOption.Warning), set.GetOption(CodeStyleOptions.QualifyEventAccess, LanguageNames.CSharp));

        // cache hit:
        Assert.Equal(new CodeStyleOption<bool>(true, NotificationOption.Warning), set.GetOption(CodeStyleOptions.QualifyEventAccess, LanguageNames.CSharp));

        // option stored in underlying config:
        Assert.Equal(new CodeStyleOption<bool>(true, NotificationOption.Error), set.GetOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp));

        // cache hit:
        Assert.Equal(new CodeStyleOption<bool>(true, NotificationOption.Error), set.GetOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp));

        // public option that has no editorconfig storage:
        Assert.Equal(RecommendationOptions.FilterOutOfScopeLocals.DefaultValue, set.GetOption(RecommendationOptions.FilterOutOfScopeLocals, LanguageNames.CSharp));
    }

    [Fact]
    public void GetOption_NoConfigOptions()
    {
        var underlyingSet = new TestOptionSet(ImmutableDictionary<OptionKey, object?>.Empty.Add(
            new OptionKey(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp),
            new CodeStyleOption<bool>(true, NotificationOption.Error)));

        var set = new DocumentOptionSet(configOptions: null, underlyingSet, LanguageNames.CSharp);

        // option stored in analyzer config:
        Assert.Equal(CodeStyleOptions.QualifyEventAccess.DefaultValue, set.GetOption(CodeStyleOptions.QualifyEventAccess, LanguageNames.CSharp));

        // cache hit:
        Assert.Equal(CodeStyleOptions.QualifyEventAccess.DefaultValue, set.GetOption(CodeStyleOptions.QualifyEventAccess, LanguageNames.CSharp));

        // option stored in underlying config:
        Assert.Equal(new CodeStyleOption<bool>(true, NotificationOption.Error), set.GetOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp));

        // cache hit:
        Assert.Equal(new CodeStyleOption<bool>(true, NotificationOption.Error), set.GetOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp));
    }
}
