// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.VirtualChars;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ValidateRegexString;

namespace Microsoft.CodeAnalysis.CSharp.JsonStringDetector
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpJsonStringDetectorDiagnosticAnalyzer : AbstractJsonStringDetectorDiagnosticAnalyzer
    {
        public CSharpJsonStringDetectorDiagnosticAnalyzer()
            : base((int)SyntaxKind.StringLiteralToken,
                   CSharpSyntaxFactsService.Instance,
                   CSharpSemanticFactsService.Instance,
                   CSharpVirtualCharService.Instance)
        {
        }
    }
}
