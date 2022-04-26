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
            CodeStyleOption2<bool>? qualifyFieldAccess = null,
            CodeStyleOption2<bool>? qualifyPropertyAccess = null,
            CodeStyleOption2<bool>? qualifyMethodAccess = null,
            CodeStyleOption2<bool>? qualifyEventAccess = null,
            CodeStyleOption2<bool>? preferPredefinedTypeKeywordInMemberAccess = null,
            CodeStyleOption2<bool>? preferPredefinedTypeKeywordInDeclaration = null,
            CodeStyleOption2<bool>? varForBuiltInTypes = null,
            CodeStyleOption2<bool>? varWhenTypeIsApparent = null,
            CodeStyleOption2<bool>? varElsewhere = null,
            CodeStyleOption2<bool>? preferSimpleDefaultExpression = null,
            CodeStyleOption2<PreferBracesPreference>? preferBraces = null)
            : base(
                qualifyFieldAccess: qualifyFieldAccess,
                qualifyPropertyAccess: qualifyPropertyAccess,
                qualifyMethodAccess: qualifyMethodAccess,
                qualifyEventAccess: qualifyEventAccess,
                preferPredefinedTypeKeywordInMemberAccess,
                preferPredefinedTypeKeywordInDeclaration)
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
                qualifyFieldAccess: options.GetEditorConfigOption(CodeStyleOptions2.QualifyFieldAccess, fallbackOptions.QualifyFieldAccess),
                qualifyPropertyAccess: options.GetEditorConfigOption(CodeStyleOptions2.QualifyPropertyAccess, fallbackOptions.QualifyPropertyAccess),
                qualifyMethodAccess: options.GetEditorConfigOption(CodeStyleOptions2.QualifyMethodAccess, fallbackOptions.QualifyMethodAccess),
                qualifyEventAccess: options.GetEditorConfigOption(CodeStyleOptions2.QualifyEventAccess, fallbackOptions.QualifyEventAccess),
                preferPredefinedTypeKeywordInMemberAccess: options.GetEditorConfigOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, fallbackOptions.PreferPredefinedTypeKeywordInMemberAccess),
                preferPredefinedTypeKeywordInDeclaration: options.GetEditorConfigOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, fallbackOptions.PreferPredefinedTypeKeywordInDeclaration),
                varForBuiltInTypes: options.GetEditorConfigOption(CSharpCodeStyleOptions.VarForBuiltInTypes, fallbackOptions.VarForBuiltInTypes),
                varWhenTypeIsApparent: options.GetEditorConfigOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, fallbackOptions.VarWhenTypeIsApparent),
                varElsewhere: options.GetEditorConfigOption(CSharpCodeStyleOptions.VarElsewhere, fallbackOptions.VarElsewhere),
                preferSimpleDefaultExpression: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, fallbackOptions.PreferSimpleDefaultExpression),
                preferBraces: options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferBraces, fallbackOptions.PreferBraces));
        }
    }
}
