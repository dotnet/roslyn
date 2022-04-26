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

        [DataMember(Order = BaseMemberCount + 0)]
        public readonly CodeStyleOption2<bool> VarForBuiltInTypes;

        [DataMember(Order = BaseMemberCount + 1)]
        public readonly CodeStyleOption2<bool> VarWhenTypeIsApparent;

        [DataMember(Order = BaseMemberCount + 2)]
        public readonly CodeStyleOption2<bool> VarElsewhere;

        [DataMember(Order = BaseMemberCount + 3)]
        public readonly CodeStyleOption2<bool> PreferSimpleDefaultExpression;

        [DataMember(Order = BaseMemberCount + 4)]
        public readonly CodeStyleOption2<bool> PreferParameterNullChecking;

        [DataMember(Order = BaseMemberCount + 5)]
        public readonly CodeStyleOption2<bool> AllowEmbeddedStatementsOnSameLine;

        [DataMember(Order = BaseMemberCount + 6)]
        public readonly CodeStyleOption2<PreferBracesPreference> PreferBraces;

        [DataMember(Order = BaseMemberCount + 7)]
        public readonly CodeStyleOption2<bool> PreferThrowExpression;

        public CSharpSimplifierOptions(
            CommonOptions? common = null,
            CodeStyleOption2<bool>? varForBuiltInTypes = null,
            CodeStyleOption2<bool>? varWhenTypeIsApparent = null,
            CodeStyleOption2<bool>? varElsewhere = null,
            CodeStyleOption2<bool>? preferSimpleDefaultExpression = null,
            CodeStyleOption2<bool>? preferParameterNullChecking = null,
            CodeStyleOption2<bool>? allowEmbeddedStatementsOnSameLine = null,
            CodeStyleOption2<PreferBracesPreference>? preferBraces = null,
            CodeStyleOption2<bool>? preferThrowExpression = null)
            : base(common)
        {
            VarForBuiltInTypes = varForBuiltInTypes ?? CodeStyleOption2<bool>.Default;
            VarWhenTypeIsApparent = varWhenTypeIsApparent ?? CodeStyleOption2<bool>.Default;
            VarElsewhere = varElsewhere ?? CodeStyleOption2<bool>.Default;
            PreferSimpleDefaultExpression = preferSimpleDefaultExpression ?? s_trueWithSuggestionEnforcement;
            PreferParameterNullChecking = preferParameterNullChecking ?? s_trueWithSuggestionEnforcement;
            AllowEmbeddedStatementsOnSameLine = allowEmbeddedStatementsOnSameLine ?? s_trueWithSilentEnforcement;
            PreferBraces = preferBraces ?? s_defaultPreferBraces;
            PreferThrowExpression = preferThrowExpression ?? s_trueWithSuggestionEnforcement;
        }

        public static readonly CSharpSimplifierOptions Default = new();

        internal static CSharpSimplifierOptions Create(AnalyzerConfigOptions options, CSharpSimplifierOptions? fallbackOptions)
        {
            fallbackOptions ??= Default;

            return new(
                CommonOptions.Create(options, fallbackOptions.Common),
                varForBuiltInTypes: options.GetEditorConfigOption(CSharpCodeStyleOptions.VarForBuiltInTypes, fallbackOptions.VarForBuiltInTypes),
                varWhenTypeIsApparent: options.GetEditorConfigOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, fallbackOptions.VarWhenTypeIsApparent),
                varElsewhere: options.GetEditorConfigOption(CSharpCodeStyleOptions.VarElsewhere, fallbackOptions.VarElsewhere),
                preferSimpleDefaultExpression: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, fallbackOptions.PreferSimpleDefaultExpression),
                preferParameterNullChecking: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferParameterNullChecking, fallbackOptions.PreferParameterNullChecking),
                allowEmbeddedStatementsOnSameLine: options.GetEditorConfigOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, fallbackOptions.AllowEmbeddedStatementsOnSameLine),
                preferBraces: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferBraces, fallbackOptions.PreferBraces),
                preferThrowExpression: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferThrowExpression, fallbackOptions.PreferThrowExpression));
        }
    }
}
