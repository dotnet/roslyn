// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
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
            new OptionKey(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp),
            new CodeStyleOption2<bool>(true, NotificationOption2.Error)));

        var configOptions = StructuredAnalyzerConfigOptions.Create(ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer).Add(
            "dotnet_style_qualification_for_event", "true:warning"));

        var set = new DocumentOptionSet(configOptions, underlyingSet, LanguageNames.CSharp);

        // option stored in analyzer config:
        var internalCodeStyleOption = new CodeStyleOption2<bool>(true, NotificationOption2.Warning.WithIsExplicitlySpecified(true));
        Assert.Equal(new CodeStyleOption<bool>(internalCodeStyleOption), set.GetOption(CodeStyleOptions.QualifyEventAccess, LanguageNames.CSharp));

        // cache hit:
        Assert.Equal(new CodeStyleOption<bool>(internalCodeStyleOption), set.GetOption(CodeStyleOptions.QualifyEventAccess, LanguageNames.CSharp));

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
            new OptionKey(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp),
            new CodeStyleOption2<bool>(true, NotificationOption2.Error)));

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

    [Fact]
    public void InternalPublicValueMapping()
    {
        var underlyingSet = new TestOptionSet(ImmutableDictionary<OptionKey, object?>.Empty);
        var set = new DocumentOptionSet(configOptions: null, underlyingSet, LanguageNames.CSharp);

        var newValue = new CodeStyleOption<bool>(true, NotificationOption.Error);
        var optionKey = new OptionKey(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp);
        var updatedSet = set.WithChangedOption(optionKey, newValue);

        Assert.IsType<CodeStyleOption2<bool>>(updatedSet.GetInternalOptionValue(optionKey));

        var actualValue = updatedSet.GetOption(optionKey);
        Assert.IsType<CodeStyleOption<bool>>(actualValue);
        Assert.Equal(newValue, actualValue);
    }

    [Fact]
    public void InternalStorageMapping_NewLineBeforeOpenBrace()
    {
        var underlyingSet = new TestOptionSet(ImmutableDictionary<OptionKey, object?>.Empty);
        var set = new DocumentOptionSet(configOptions: null, underlyingSet, LanguageNames.CSharp);

        var option = CSharpFormattingOptions2.NewLineBeforeOpenBrace;

        foreach (var (legacyOption, flag) in new[]
        {
            (CSharpFormattingOptions.NewLinesForBracesInTypes, NewLineBeforeOpenBracePlacement.Types),
            (CSharpFormattingOptions.NewLinesForBracesInMethods, NewLineBeforeOpenBracePlacement.Methods),
            (CSharpFormattingOptions.NewLinesForBracesInProperties, NewLineBeforeOpenBracePlacement.Properties),
            (CSharpFormattingOptions.NewLinesForBracesInAccessors, NewLineBeforeOpenBracePlacement.Accessors),
            (CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, NewLineBeforeOpenBracePlacement.AnonymousMethods),
            (CSharpFormattingOptions.NewLinesForBracesInControlBlocks, NewLineBeforeOpenBracePlacement.ControlBlocks),
            (CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, NewLineBeforeOpenBracePlacement.AnonymousTypes),
            (CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers),
            (CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, NewLineBeforeOpenBracePlacement.LambdaExpressionBody),
        })
        {
            var newValue = !legacyOption.DefaultValue;
            var optionKey = new OptionKey(legacyOption);
            var updatedSet = set.WithChangedOption(optionKey, newValue);

            var newInternalValue = option.DefaultValue.WithFlagValue(flag, newValue);

            Assert.Equal(newInternalValue, updatedSet.GetInternalOptionValue(new OptionKey(option)));
            Assert.Equal(newValue, updatedSet.GetOption(optionKey));
        }
    }

    [Fact]
    public void InternalStorageMapping_NewLineBeforeOpenBrace_Config()
    {
        var underlyingSet = new TestOptionSet(ImmutableDictionary<OptionKey, object?>.Empty
            .Add(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInTypes), false)
            .Add(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInAccessors), true)
            .Add(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods), false));

        var configOptions = StructuredAnalyzerConfigOptions.Create(ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer).Add(
            "csharp_new_line_before_open_brace", "types,methods"));

        var set = new DocumentOptionSet(configOptions, underlyingSet, LanguageNames.CSharp);

        // editor config value overrides all values of the enum stored in the underlying set:
        Assert.True((bool?)set.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInTypes)));
        Assert.True((bool?)set.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInMethods)));
        Assert.False((bool?)set.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInAccessors)));
        Assert.False((bool?)set.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods)));
        Assert.False((bool?)set.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInProperties)));

        var updatedSet = set.WithChangedOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInProperties), true);

        Assert.True((bool?)updatedSet.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInTypes)));
        Assert.True((bool?)updatedSet.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInMethods)));
        Assert.False((bool?)updatedSet.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInAccessors)));
        Assert.False((bool?)updatedSet.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods)));
        Assert.True((bool?)updatedSet.GetOption(new OptionKey(CSharpFormattingOptions.NewLinesForBracesInProperties)));
    }

    [Fact]
    public void InternalStorageMapping_SpaceBetweenParentheses()
    {
        var underlyingSet = new TestOptionSet(ImmutableDictionary<OptionKey, object?>.Empty);
        var set = new DocumentOptionSet(configOptions: null, underlyingSet, LanguageNames.CSharp);

        var option = CSharpFormattingOptions2.SpaceBetweenParentheses;

        foreach (var (legacyOption, flag) in new[]
        {
            (CSharpFormattingOptions.SpaceWithinExpressionParentheses, SpacePlacementWithinParentheses.Expressions),
            (CSharpFormattingOptions.SpaceWithinCastParentheses, SpacePlacementWithinParentheses.TypeCasts),
            (CSharpFormattingOptions.SpaceWithinOtherParentheses, SpacePlacementWithinParentheses.ControlFlowStatements),
        })
        {
            var newValue = !legacyOption.DefaultValue;
            var optionKey = new OptionKey(legacyOption);
            var updatedSet = set.WithChangedOption(optionKey, newValue);

            var newInternalValue = option.DefaultValue.WithFlagValue(flag, newValue);

            Assert.Equal(newInternalValue, updatedSet.GetInternalOptionValue(new OptionKey(option)));
            Assert.Equal(newValue, updatedSet.GetOption(optionKey));
        }
    }
}
