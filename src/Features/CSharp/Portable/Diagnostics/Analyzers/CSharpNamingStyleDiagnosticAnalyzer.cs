// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpNamingStyleDiagnosticAnalyzer : NamingStyleDiagnosticAnalyzerBase
    {
        public CSharpNamingStyleDiagnosticAnalyzer()
        {

        }
    }
}
