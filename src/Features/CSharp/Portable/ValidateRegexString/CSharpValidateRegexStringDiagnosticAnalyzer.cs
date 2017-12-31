// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        protected override IParameterSymbol DetermineParameter(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
            => ((ArgumentSyntax)argumentNode).DetermineParameter(semanticModel, allowParams: false, cancellationToken);

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override IVirtualCharService GetVirtualCharService()
            => CSharpVirtualCharService.Instance;
    }
}
