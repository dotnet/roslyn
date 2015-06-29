// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class SemanticModelAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [TestMethod]
        public void Test1()
        {
            var test = @"
class C
{
    public async int M()
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SemanticModelAnalyzerRuleId,
                Message = string.Format(Resources.SemanticModelAnalyzerMessageFormat, "Test0.cs", 1),
                Severity = DiagnosticSeverity.Warning
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SemanticModelAnalyzer();
        }
    }
}