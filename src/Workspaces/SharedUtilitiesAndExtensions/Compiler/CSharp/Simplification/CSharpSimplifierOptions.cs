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
        private static readonly CodeStyleOption2<PreferBracesPreference> s_defaultPreferBraces = new(PreferBracesPreference.Always, NotificationOption2.Silent);

        [DataMember(Order = BaseMemberCount + 0)]
        public readonly CodeStyleOption2<bool> VarForBuiltInTypes;

        [DataMember(Order = BaseMemberCount + 1)]
        public readonly CodeStyleOption2<bool> VarWhenTypeIsApparent;

        [DataMember(Order = BaseMemberCount + 2)]
        public readonly CodeStyleOption2<bool> VarElsewhere;

        [DataMember(Order = BaseMemberCount + 3)]
        public readonly CodeStyleOption2<bool> PreferSimpleDefaultExpression;

        [DataMember(Order = BaseMemberCount + 4)]
        public readonly CodeStyleOption2<PreferBracesPreference> PreferBraces;

        public CSharpSimplifierOptions(
            CommonOptions? common = null,
            CodeStyleOption2<bool>? varForBuiltInTypes = null,
            CodeStyleOption2<bool>? varWhenTypeIsApparent = null,
            CodeStyleOption2<bool>? varElsewhere = null,
            CodeStyleOption2<bool>? preferSimpleDefaultExpression = null,
            CodeStyleOption2<PreferBracesPreference>? preferBraces = null)
            : base(common)
        {
            VarForBuiltInTypes = varForBuiltInTypes ?? CodeStyleOption2<bool>.Default;
            VarWhenTypeIsApparent = varWhenTypeIsApparent ?? CodeStyleOption2<bool>.Default;
            VarElsewhere = varElsewhere ?? CodeStyleOption2<bool>.Default;
            PreferSimpleDefaultExpression = preferSimpleDefaultExpression ?? CodeStyleOptions2.TrueWithSuggestionEnforcement;
            PreferBraces = preferBraces ?? s_defaultPreferBraces;
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
                preferBraces: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferBraces, fallbackOptions.PreferBraces));
        }
    }
}
