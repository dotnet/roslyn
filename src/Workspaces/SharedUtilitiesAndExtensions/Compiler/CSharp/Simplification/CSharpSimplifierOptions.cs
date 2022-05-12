// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    [DataContract]
    internal sealed class CSharpSimplifierOptions : SimplifierOptions
    {
        private static readonly CodeStyleOption2<PreferBracesPreference> s_defaultPreferBraces =
            new(PreferBracesPreference.Always, NotificationOption2.Silent);

        private static readonly CodeStyleOption2<bool> s_trueWithSuggestionEnforcement =
            new(value: true, notification: NotificationOption2.Suggestion);

        private static readonly CodeStyleOption2<bool> s_trueWithSilentEnforcement =
            new(value: true, notification: NotificationOption2.Silent);

        public static readonly CSharpSimplifierOptions Default = new();

        [DataMember(Order = BaseMemberCount + 0)]
        public CodeStyleOption2<bool> VarForBuiltInTypes { get; init; } = CodeStyleOption2<bool>.Default;

        [DataMember(Order = BaseMemberCount + 1)]
        public CodeStyleOption2<bool> VarWhenTypeIsApparent { get; init; } = CodeStyleOption2<bool>.Default;

        [DataMember(Order = BaseMemberCount + 2)]
        public CodeStyleOption2<bool> VarElsewhere { get; init; } = CodeStyleOption2<bool>.Default;

        [DataMember(Order = BaseMemberCount + 3)]
        public CodeStyleOption2<bool> PreferSimpleDefaultExpression { get; init; } = s_trueWithSuggestionEnforcement;

        [DataMember(Order = BaseMemberCount + 4)]
        public CodeStyleOption2<bool> PreferParameterNullChecking { get; init; } = s_trueWithSuggestionEnforcement;

        [DataMember(Order = BaseMemberCount + 5)]
        public CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine { get; init; } = s_trueWithSilentEnforcement;

        [DataMember(Order = BaseMemberCount + 6)]
        public CodeStyleOption2<PreferBracesPreference> PreferBraces { get; init; } = s_defaultPreferBraces;

        [DataMember(Order = BaseMemberCount + 7)]
        public CodeStyleOption2<bool> PreferThrowExpression { get; init; } = s_trueWithSuggestionEnforcement;
    }

    internal static class CSharpSimplifierOptionsProviders
    {
        public static CSharpSimplifierOptions GetCSharpSimplifierOptions(this AnalyzerConfigOptions options, CSharpSimplifierOptions? fallbackOptions)
        {
            fallbackOptions ??= CSharpSimplifierOptions.Default;

            return new()
            {
                Common = options.GetCommonSimplifierOptions(fallbackOptions.Common),
                VarForBuiltInTypes = options.GetEditorConfigOption(CSharpCodeStyleOptions.VarForBuiltInTypes, fallbackOptions.VarForBuiltInTypes),
                VarWhenTypeIsApparent = options.GetEditorConfigOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, fallbackOptions.VarWhenTypeIsApparent),
                VarElsewhere = options.GetEditorConfigOption(CSharpCodeStyleOptions.VarElsewhere, fallbackOptions.VarElsewhere),
                PreferSimpleDefaultExpression = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, fallbackOptions.PreferSimpleDefaultExpression),
                PreferParameterNullChecking = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferParameterNullChecking, fallbackOptions.PreferParameterNullChecking),
                AllowEmbeddedStatementsOnSameLine = options.GetEditorConfigOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, fallbackOptions.AllowEmbeddedStatementsOnSameLine),
                PreferBraces = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferBraces, fallbackOptions.PreferBraces),
                PreferThrowExpression = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferThrowExpression, fallbackOptions.PreferThrowExpression)
            };
        }
    }
}
