// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseThrowExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseThrowExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseThrowExpressionDiagnosticAnalyzer : AbstractUseThrowExpressionDiagnosticAnalyzer
    {
        public CSharpUseThrowExpressionDiagnosticAnalyzer()
            : base(CSharpCodeStyleOptions.PreferThrowExpression, LanguageNames.CSharp)
        {
        }

        protected override bool IsSupported(ParseOptions options)
        {
            var csOptions = (CSharpParseOptions)options;
            return csOptions.LanguageVersion >= LanguageVersion.CSharp7;
        }

        protected override ISemanticFactsService GetSemanticFactsService()
            => CSharpSemanticFactsService.Instance;
    }
}
