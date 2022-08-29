// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    [DataContract]
    internal sealed class CSharpSimplifierOptions : SimplifierOptions, IEquatable<CSharpSimplifierOptions>
    {
        private static readonly CodeStyleOption2<PreferBracesPreference> s_defaultPreferBraces =
            new(PreferBracesPreference.Always, NotificationOption2.Silent);

        private static readonly CodeStyleOption2<bool> s_trueWithSuggestionEnforcement =
            new(value: true, notification: NotificationOption2.Suggestion);

        private static readonly CodeStyleOption2<bool> s_trueWithSilentEnforcement =
            new(value: true, notification: NotificationOption2.Silent);

        public static readonly CSharpSimplifierOptions Default = new();

        [DataMember] public CodeStyleOption2<bool> VarForBuiltInTypes { get; init; } = CodeStyleOption2<bool>.Default;
        [DataMember] public CodeStyleOption2<bool> VarWhenTypeIsApparent { get; init; } = CodeStyleOption2<bool>.Default;
        [DataMember] public CodeStyleOption2<bool> VarElsewhere { get; init; } = CodeStyleOption2<bool>.Default;
        [DataMember] public CodeStyleOption2<bool> PreferSimpleDefaultExpression { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember] public CodeStyleOption2<bool> PreferParameterNullChecking { get; init; } = s_trueWithSuggestionEnforcement;
        [DataMember] public CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine { get; init; } = s_trueWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<PreferBracesPreference> PreferBraces { get; init; } = s_defaultPreferBraces;
        [DataMember] public CodeStyleOption2<bool> PreferThrowExpression { get; init; } = s_trueWithSuggestionEnforcement;

        public override bool Equals(object? obj)
            => Equals(obj as CSharpSimplifierOptions);

        public bool Equals([AllowNull] CSharpSimplifierOptions other)
            => other is not null &&
               Common.Equals(other.Common) &&
               VarForBuiltInTypes.Equals(other.VarForBuiltInTypes) &&
               VarWhenTypeIsApparent.Equals(other.VarWhenTypeIsApparent) &&
               VarElsewhere.Equals(other.VarElsewhere) &&
               PreferSimpleDefaultExpression.Equals(other.PreferSimpleDefaultExpression) &&
               PreferParameterNullChecking.Equals(other.PreferParameterNullChecking) &&
               AllowEmbeddedStatementsOnSameLine.Equals(other.AllowEmbeddedStatementsOnSameLine) &&
               PreferBraces.Equals(other.PreferBraces) &&
               PreferThrowExpression.Equals(other.PreferThrowExpression);

        public override int GetHashCode()
            => Hash.Combine(VarForBuiltInTypes,
               Hash.Combine(VarWhenTypeIsApparent,
               Hash.Combine(VarElsewhere,
               Hash.Combine(PreferSimpleDefaultExpression,
               Hash.Combine(PreferParameterNullChecking,
               Hash.Combine(AllowEmbeddedStatementsOnSameLine,
               Hash.Combine(PreferBraces,
               Hash.Combine(PreferThrowExpression, 0))))))));
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
                AllowEmbeddedStatementsOnSameLine = options.GetEditorConfigOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, fallbackOptions.AllowEmbeddedStatementsOnSameLine),
                PreferBraces = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferBraces, fallbackOptions.PreferBraces),
                PreferThrowExpression = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferThrowExpression, fallbackOptions.PreferThrowExpression)
            };
        }
    }
}
