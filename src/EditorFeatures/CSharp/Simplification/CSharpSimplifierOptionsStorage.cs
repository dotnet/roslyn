// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal static class CSharpSimplifierOptionsStorage
{
    public static CSharpSimplifierOptions GetCSharpSimplifierOptions(this IGlobalOptionService globalOptions)
        => new(
            qualifyFieldAccess: globalOptions.GetOption(CodeStyleOptions2.QualifyFieldAccess, LanguageNames.CSharp),
            qualifyPropertyAccess: globalOptions.GetOption(CodeStyleOptions2.QualifyPropertyAccess, LanguageNames.CSharp),
            qualifyMethodAccess: globalOptions.GetOption(CodeStyleOptions2.QualifyMethodAccess, LanguageNames.CSharp),
            qualifyEventAccess: globalOptions.GetOption(CodeStyleOptions2.QualifyEventAccess, LanguageNames.CSharp),
            preferPredefinedTypeKeywordInMemberAccess: globalOptions.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp),
            preferPredefinedTypeKeywordInDeclaration: globalOptions.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.CSharp),
            varForBuiltInTypes: globalOptions.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes),
            varWhenTypeIsApparent: globalOptions.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent),
            varElsewhere: globalOptions.GetOption(CSharpCodeStyleOptions.VarElsewhere),
            preferSimpleDefaultExpression: globalOptions.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression),
            preferBraces: globalOptions.GetOption(CSharpCodeStyleOptions.PreferBraces));
}
