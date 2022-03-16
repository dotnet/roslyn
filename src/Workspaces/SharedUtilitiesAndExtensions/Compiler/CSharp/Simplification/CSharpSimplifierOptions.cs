// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal sealed class CSharpSimplifierOptions : SimplifierOptions
    {
        public readonly CodeStyleOption2<bool> VarForBuiltInTypes;
        public readonly CodeStyleOption2<bool> VarWhenTypeIsApparent;
        public readonly CodeStyleOption2<bool> VarElsewhere;
        public readonly CodeStyleOption2<bool> PreferSimpleDefaultExpression;
        public readonly CodeStyleOption2<PreferBracesPreference> PreferBraces;

        public CSharpSimplifierOptions(
            CodeStyleOption2<bool> qualifyFieldAccess,
            CodeStyleOption2<bool> qualifyPropertyAccess,
            CodeStyleOption2<bool> qualifyMethodAccess,
            CodeStyleOption2<bool> qualifyEventAccess,
            CodeStyleOption2<bool> varForBuiltInTypes,
            CodeStyleOption2<bool> varWhenTypeIsApparent,
            CodeStyleOption2<bool> varElsewhere,
            CodeStyleOption2<bool> preferPredefinedTypeKeywordInMemberAccess,
            CodeStyleOption2<bool> preferPredefinedTypeKeywordInDeclaration,
            CodeStyleOption2<bool> preferSimpleDefaultExpression,
            CodeStyleOption2<PreferBracesPreference> preferBraces)
            : base(
                qualifyFieldAccess: qualifyFieldAccess,
                qualifyPropertyAccess: qualifyPropertyAccess,
                qualifyMethodAccess: qualifyMethodAccess,
                qualifyEventAccess: qualifyEventAccess,
                preferPredefinedTypeKeywordInMemberAccess: preferPredefinedTypeKeywordInMemberAccess,
                preferPredefinedTypeKeywordInDeclaration: preferPredefinedTypeKeywordInDeclaration)
        {
            VarForBuiltInTypes = varForBuiltInTypes;
            VarWhenTypeIsApparent = varWhenTypeIsApparent;
            VarElsewhere = varElsewhere;
            PreferSimpleDefaultExpression = preferSimpleDefaultExpression;
            PreferBraces = preferBraces;
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

        internal static CSharpSimplifierOptions Create(AnalyzerConfigOptions options)
            => new(
                qualifyFieldAccess: options.GetOption(CodeStyleOptions2.QualifyFieldAccess),
                qualifyPropertyAccess: options.GetOption(CodeStyleOptions2.QualifyPropertyAccess),
                qualifyMethodAccess: options.GetOption(CodeStyleOptions2.QualifyMethodAccess),
                qualifyEventAccess: options.GetOption(CodeStyleOptions2.QualifyEventAccess),
                preferPredefinedTypeKeywordInMemberAccess: options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
                preferPredefinedTypeKeywordInDeclaration: options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration),
                varForBuiltInTypes: options.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes),
                varWhenTypeIsApparent: options.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent),
                varElsewhere: options.GetOption(CSharpCodeStyleOptions.VarElsewhere),
                preferSimpleDefaultExpression: options.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression),
                preferBraces: options.GetOption(CSharpCodeStyleOptions.PreferBraces));
    }
}
