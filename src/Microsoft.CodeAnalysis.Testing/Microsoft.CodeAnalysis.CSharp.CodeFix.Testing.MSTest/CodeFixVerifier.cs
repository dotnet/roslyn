// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public static class CodeFixVerifier
    {
        public static CodeFixVerifier<TAnalyzer, TCodeFix> Create<TAnalyzer, TCodeFix>()
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : CodeFixProvider, new()
        {
            return new CodeFixVerifier<TAnalyzer, TCodeFix>();
        }
    }
}
