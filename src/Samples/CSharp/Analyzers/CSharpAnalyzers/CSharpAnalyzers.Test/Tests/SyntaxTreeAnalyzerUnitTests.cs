// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class SyntaxTreeAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [TestMethod]
        public void Test1()
        {
            var test = @"
class C
{
    public void M()
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SyntaxTreeAnalyzerRuleId,
                Message = string.Format(Resources.SyntaxTreeAnalyzerMessageFormat, "Test0.cs"),
                Severity = DiagnosticSeverity.Warning
            };

            var parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose);
            VerifyCSharpDiagnostic(test, parseOptions, compilationOptions: null);

            parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None);
            VerifyCSharpDiagnostic(test, parseOptions, compilationOptions: null, expected: expected);

            parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);
            VerifyCSharpDiagnostic(test, parseOptions, compilationOptions: null, expected: expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SyntaxTreeAnalyzer();
        }
    }
}