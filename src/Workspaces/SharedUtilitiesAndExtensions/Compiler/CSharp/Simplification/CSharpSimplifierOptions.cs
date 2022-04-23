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
                qualifyFieldAccess: qualifyFieldAccess ?? Default.QualifyFieldAccess,
                qualifyPropertyAccess: qualifyPropertyAccess ?? Default.QualifyPropertyAccess,
                qualifyMethodAccess: qualifyMethodAccess ?? Default.QualifyMethodAccess,
                qualifyEventAccess: qualifyEventAccess ?? Default.QualifyEventAccess,
                preferPredefinedTypeKeywordInMemberAccess: preferPredefinedTypeKeywordInMemberAccess ?? Default.PreferPredefinedTypeKeywordInMemberAccess,
                preferPredefinedTypeKeywordInDeclaration: preferPredefinedTypeKeywordInDeclaration ?? Default.PreferPredefinedTypeKeywordInDeclaration)
        {
            VarForBuiltInTypes = varForBuiltInTypes ?? Default.VarForBuiltInTypes;
            VarWhenTypeIsApparent = varWhenTypeIsApparent ?? Default.VarWhenTypeIsApparent;
            VarElsewhere = varElsewhere ?? Default.VarElsewhere;
            PreferSimpleDefaultExpression = preferSimpleDefaultExpression ?? Default.PreferSimpleDefaultExpression;
            PreferBraces = preferBraces ?? Default.PreferBraces;
        }

        public static readonly CSharpSimplifierOptions Default = new(
            qualifyFieldAccess: CodeStyleOptions2.QualifyFieldAccess.DefaultValue,
            qualifyPropertyAccess: CodeStyleOptions2.QualifyPropertyAccess.DefaultValue,
            qualifyMethodAccess: CodeStyleOptions2.QualifyMethodAccess.DefaultValue,
            qualifyEventAccess: CodeStyleOptions2.QualifyEventAccess.DefaultValue,
            preferPredefinedTypeKeywordInMemberAccess: CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.DefaultValue,
            preferPredefinedTypeKeywordInDeclaration: CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration.DefaultValue,
            varForBuiltInTypes: CSharpCodeStyleOptions.VarForBuiltInTypes.DefaultValue,
            varWhenTypeIsApparent: CSharpCodeStyleOptions.VarWhenTypeIsApparent.DefaultValue,
            varElsewhere: CSharpCodeStyleOptions.VarElsewhere.DefaultValue,
            preferSimpleDefaultExpression: CSharpCodeStyleOptions.PreferSimpleDefaultExpression.DefaultValue,
            preferBraces: CSharpCodeStyleOptions.PreferBraces.DefaultValue);

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
