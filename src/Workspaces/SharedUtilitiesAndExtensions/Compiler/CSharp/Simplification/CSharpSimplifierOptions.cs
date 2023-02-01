// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    [DataContract]
    internal sealed record class CSharpSimplifierOptions : SimplifierOptions, IEquatable<CSharpSimplifierOptions>
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

        public CSharpSimplifierOptions()
        {
        }

        public CSharpSimplifierOptions(IOptionsReader options, CSharpSimplifierOptions fallbackOptions)
            : base(options, LanguageNames.CSharp, fallbackOptions)
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
}
