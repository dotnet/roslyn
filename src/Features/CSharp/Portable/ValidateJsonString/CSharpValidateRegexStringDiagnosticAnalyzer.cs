// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.VirtualChars;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ValidateJsonString;

namespace Microsoft.CodeAnalysis.CSharp.ValidateJsonString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpValidateJsonStringDiagnosticAnalyzer : AbstractValidateJsonStringDiagnosticAnalyzer
    {
        public CSharpValidateJsonStringDiagnosticAnalyzer() 
            : base((int)SyntaxKind.StringLiteralToken, 
                   CSharpSyntaxFactsService.Instance,
                   CSharpSemanticFactsService.Instance,
                   CSharpVirtualCharService.Instance)
        {
        }
    }
}
