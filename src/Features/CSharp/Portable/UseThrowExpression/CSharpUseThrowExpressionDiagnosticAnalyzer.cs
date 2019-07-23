// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseThrowExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseThrowExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseThrowExpressionDiagnosticAnalyzer : AbstractUseThrowExpressionDiagnosticAnalyzer
    {
        protected override bool IsSupported(ParseOptions options)
        {
            var csOptions = (CSharpParseOptions)options;
            return csOptions.LanguageVersion >= LanguageVersion.CSharp7;
        }

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override ISemanticFactsService GetSemanticFactsService()
            => CSharpSemanticFactsService.Instance;
    }
}
