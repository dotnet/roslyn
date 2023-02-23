// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

public interface ICSharpSimplifierOptions : ISimplifierOptions
{
    bool VarForBuiltInTypes { get; }
    bool VarWhenTypeIsApparent { get; }
    bool VarElsewhere { get; }
    bool PreferSimpleDefaultExpression { get; }
    bool PreferParameterNullChecking { get; }
    bool AllowEmbeddedStatementsOnSameLine { get; }
    BracePlacementPreferences BracePlacement { get; }
    bool PreferThrowExpression { get; }
}

[DataContract]
public record class CSharpSimplifierOptions : SimplifierOptions, ICSharpSimplifierOptions
{
    public static readonly CSharpSimplifierOptions Default = new();

    [DataMember] public bool VarForBuiltInTypes { get; init; } = CSharpSimplifierStyleOptions.Default.VarForBuiltInTypes.Value;
    [DataMember] public bool VarWhenTypeIsApparent { get; init; } = CSharpSimplifierStyleOptions.Default.VarWhenTypeIsApparent.Value;
    [DataMember] public bool VarElsewhere { get; init; } = CSharpSimplifierStyleOptions.Default.VarElsewhere.Value;
    [DataMember] public bool PreferSimpleDefaultExpression { get; init; } = CSharpSimplifierStyleOptions.Default.PreferSimpleDefaultExpression.Value;
    [DataMember] public bool PreferParameterNullChecking { get; init; } = CSharpSimplifierStyleOptions.Default.PreferParameterNullChecking.Value;
    [DataMember] public bool AllowEmbeddedStatementsOnSameLine { get; init; } = CSharpSimplifierStyleOptions.Default.AllowEmbeddedStatementsOnSameLine.Value;
    [DataMember] public BracePlacementPreferences BracePlacement { get; init; } = (BracePlacementPreferences)CSharpSimplifierStyleOptions.Default.PreferBraces.Value;
    [DataMember] public bool PreferThrowExpression { get; init; } = CSharpSimplifierStyleOptions.Default.PreferThrowExpression.Value;

    public CSharpSimplifierOptions()
    {
    }
}

[DataContract]
internal sealed record class CSharpSimplifierStyleOptions : SimplifierStyleOptions, ICSharpSimplifierOptions
{
    private static readonly CodeStyleOption2<PreferBracesPreference> s_defaultPreferBraces =
        new(PreferBracesPreference.Always, NotificationOption2.Silent);

    public static readonly CSharpSimplifierStyleOptions Default = new();

    [DataMember] public CodeStyleOption2<bool> VarForBuiltInTypes { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> VarWhenTypeIsApparent { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> VarElsewhere { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferSimpleDefaultExpression { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferParameterNullChecking { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;
    [DataMember] public CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<PreferBracesPreference> PreferBraces { get; init; } = s_defaultPreferBraces;
    [DataMember] public CodeStyleOption2<bool> PreferThrowExpression { get; init; } = CodeStyleOption2.TrueWithSuggestionEnforcement;

    bool ICSharpSimplifierOptions.VarForBuiltInTypes => VarForBuiltInTypes.Value;
    bool ICSharpSimplifierOptions.VarWhenTypeIsApparent => VarWhenTypeIsApparent.Value;
    bool ICSharpSimplifierOptions.VarElsewhere => VarElsewhere.Value;
    bool ICSharpSimplifierOptions.PreferSimpleDefaultExpression => PreferSimpleDefaultExpression.Value;
    bool ICSharpSimplifierOptions.PreferParameterNullChecking => PreferParameterNullChecking.Value;
    bool ICSharpSimplifierOptions.AllowEmbeddedStatementsOnSameLine => AllowEmbeddedStatementsOnSameLine.Value;
    BracePlacementPreferences ICSharpSimplifierOptions.BracePlacement => (BracePlacementPreferences)PreferBraces.Value;
    bool ICSharpSimplifierOptions.PreferThrowExpression => PreferThrowExpression.Value;

    public CSharpSimplifierStyleOptions()
    {
    }

    public CSharpSimplifierStyleOptions(IOptionsReader options, CSharpSimplifierStyleOptions? fallbackOptions)
        : base(options, fallbackOptions ??= Default, LanguageNames.CSharp)
    {
        VarForBuiltInTypes = options.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes, fallbackOptions.VarForBuiltInTypes);
        VarWhenTypeIsApparent = options.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, fallbackOptions.VarWhenTypeIsApparent);
        VarElsewhere = options.GetOption(CSharpCodeStyleOptions.VarElsewhere, fallbackOptions.VarElsewhere);
        PreferSimpleDefaultExpression = options.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, fallbackOptions.PreferSimpleDefaultExpression);
        AllowEmbeddedStatementsOnSameLine = options.GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, fallbackOptions.AllowEmbeddedStatementsOnSameLine);
        PreferBraces = options.GetOption(CSharpCodeStyleOptions.PreferBraces, fallbackOptions.PreferBraces);
        PreferThrowExpression = options.GetOption(CSharpCodeStyleOptions.PreferThrowExpression, fallbackOptions.PreferThrowExpression);
    }
}

internal static partial class Extensions
{
    public static UseVarPreference GetUseVarPreference(this ICSharpSimplifierOptions options)
    {
        var result = UseVarPreference.None;

        if (options.VarForBuiltInTypes)
            result |= UseVarPreference.ForBuiltInTypes;

        if (options.VarWhenTypeIsApparent)
            result |= UseVarPreference.WhenTypeIsApparent;

        if (options.VarElsewhere)
            result |= UseVarPreference.Elsewhere;

        return result;
    }
}
