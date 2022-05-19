// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal static class CSharpCodeGenerationOptionsStorage
{
    [ExportLanguageService(typeof(ICodeGenerationOptionsStorage), LanguageNames.CSharp), Shared]
    private sealed class Service : ICodeGenerationOptionsStorage
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Service()
        {
        }

        public CodeGenerationOptions GetOptions(IGlobalOptionService globalOptions)
            => GetCSharpCodeGenerationOptions(globalOptions);
    }

    public static CSharpCodeGenerationOptions GetCSharpCodeGenerationOptions(this IGlobalOptionService globalOptions)
        => new()
        {
            Common = globalOptions.GetCommonCodeGenerationOptions(LanguageNames.CSharp),
            PreferExpressionBodiedMethods = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods),
            PreferExpressionBodiedAccessors = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors),
            PreferExpressionBodiedProperties = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties),
            PreferExpressionBodiedIndexers = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers),
            PreferExpressionBodiedConstructors = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors),
            PreferExpressionBodiedOperators = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators),
            PreferExpressionBodiedLocalFunctions = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions),
            PreferExpressionBodiedLambdas = globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas),
            PreferStaticLocalFunction = globalOptions.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction),
            NamespaceDeclarations = globalOptions.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations)
        };
}
