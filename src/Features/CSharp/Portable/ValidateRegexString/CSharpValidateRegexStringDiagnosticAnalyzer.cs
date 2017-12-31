// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;
using Microsoft.CodeAnalysis.ValidateRegexString;

namespace Microsoft.CodeAnalysis.CSharp.ValidateRegexString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpValidateRegexStringDiagnosticAnalyzer : AbstractValidateRegexStringDiagnosticAnalyzer<SyntaxKind>
    {
        public CSharpValidateRegexStringDiagnosticAnalyzer() 
            : base((int)SyntaxKind.StringLiteralToken)
        {
        }

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override ISemanticFactsService GetSemanticFactsService()
            => CSharpSemanticFactsService.Instance;

        protected override IVirtualCharService GetVirtualCharService()
            => CSharpVirtualCharService.Instance;
    }
}
