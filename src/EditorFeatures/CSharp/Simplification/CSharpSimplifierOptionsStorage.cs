// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal static class CSharpSimplifierOptionsStorage
{
    [ExportLanguageService(typeof(ISimplifierOptionsStorage), LanguageNames.CSharp), Shared]
    internal sealed class Service : ISimplifierOptionsStorage
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Service()
        {
        }

        public SimplifierOptions GetOptions(IGlobalOptionService globalOptions)
            => GetCSharpSimplifierOptions(globalOptions);
    }

    public static CSharpSimplifierOptions GetCSharpSimplifierOptions(this IGlobalOptionService globalOptions)
        => new(
            common: globalOptions.GetCommonSimplifierOptions(LanguageNames.CSharp),
            varForBuiltInTypes: globalOptions.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes),
            varWhenTypeIsApparent: globalOptions.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent),
            varElsewhere: globalOptions.GetOption(CSharpCodeStyleOptions.VarElsewhere),
            preferSimpleDefaultExpression: globalOptions.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression),
            preferParameterNullChecking: globalOptions.GetOption(CSharpCodeStyleOptions.PreferParameterNullChecking),
            allowEmbeddedStatementsOnSameLine: globalOptions.GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine),
            preferBraces: globalOptions.GetOption(CSharpCodeStyleOptions.PreferBraces),
            preferThrowExpression: globalOptions.GetOption(CSharpCodeStyleOptions.PreferThrowExpression));
}
