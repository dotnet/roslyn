// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class SymbolAnalyzerUnitTests : DiagnosticVerifier
    {
        [TestMethod]
        public void Test1()
        {
            var test = @"
class BadOne
{
    public void BadOne() {}
}

class GoodOne
{
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SymbolAnalyzerRuleId,
                Message = string.Format(Resources.SymbolAnalyzerMessageFormat, "BadOne"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 2, 7)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SymbolAnalyzer();
        }
    }
}