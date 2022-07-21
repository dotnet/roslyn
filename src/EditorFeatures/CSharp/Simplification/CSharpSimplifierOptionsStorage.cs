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
        => new()
        {
            Common = globalOptions.GetCommonSimplifierOptions(LanguageNames.CSharp),
            VarForBuiltInTypes = globalOptions.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes),
            VarWhenTypeIsApparent = globalOptions.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent),
            VarElsewhere = globalOptions.GetOption(CSharpCodeStyleOptions.VarElsewhere),
            PreferSimpleDefaultExpression = globalOptions.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression),
            AllowEmbeddedStatementsOnSameLine = globalOptions.GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine),
            PreferBraces = globalOptions.GetOption(CSharpCodeStyleOptions.PreferBraces),
            PreferThrowExpression = globalOptions.GetOption(CSharpCodeStyleOptions.PreferThrowExpression),
        };
}
