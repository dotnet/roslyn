// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class SyntaxNodeAnalyzerUnitTests : DiagnosticVerifier
    {
        [TestMethod]
        public void Test1()
        {
            var test = @"
class C
{
    public void M()
    {
        var implicitTypedLocal = 0;
        int explicitTypedLocal = 1;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SyntaxNodeAnalyzerRuleId,
                Message = string.Format(Resources.SyntaxNodeAnalyzerMessageFormat, "implicitTypedLocal"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 6, 13)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SyntaxNodeAnalyzer();
        }
    }
}