// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Test.Utilities
{
    public static partial class VisualBasicSecurityCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>.Test
        {
            public Test()
            {
                // These analyzers run on generated code by default.
                TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;
            }
        }
    }
}
