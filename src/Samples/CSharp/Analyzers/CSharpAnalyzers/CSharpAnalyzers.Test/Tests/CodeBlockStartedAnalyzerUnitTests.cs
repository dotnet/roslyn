// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class CodeBlockStartedAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [TestMethod]
        public void Test1()
        {
            var test = @"
class C
{
    public int M1(int p1, int p2)
    {
        return M2(p1, p1);
    }

    public int M2(int p1, int p2)
    {
        return p1 + p2;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticIds.CodeBlockStartedAnalyzerRuleId,
                Message = string.Format(Resources.CodeBlockStartedAnalyzerMessageFormat, "p2", "M1"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 4, 31)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CodeBlockStartedAnalyzer();
        }
    }
}