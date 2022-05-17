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
        => new(
            preferExpressionBodiedMethods: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods),
            preferExpressionBodiedAccessors: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors),
            preferExpressionBodiedProperties: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties),
            preferExpressionBodiedIndexers: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers),
            preferExpressionBodiedConstructors: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors),
            preferExpressionBodiedOperators: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators),
            preferExpressionBodiedLocalFunctions: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions),
            preferExpressionBodiedLambdas: globalOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas),
            preferStaticLocalFunction: globalOptions.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction),
            namespaceDeclarations: globalOptions.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations));
}
