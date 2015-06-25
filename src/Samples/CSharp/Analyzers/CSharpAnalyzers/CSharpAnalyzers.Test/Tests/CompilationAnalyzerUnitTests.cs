// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class CompilationAnalyzerUnitTests
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
                Id = DiagnosticIds.CompilationAnalyzerRuleId,
                Message = string.Format(Resources.CompilationAnalyzerMessageFormat, DiagnosticIds.SymbolAnalyzerRuleId),
                Severity = DiagnosticSeverity.Warning
            };

            var specificOption = new KeyValuePair<string, ReportDiagnostic>(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Error);

            var compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                specificDiagnosticOptions: new[]{ specificOption });
            VerifyCSharpDiagnostic(test, parseOptions: null, compilationOptions: compilationOptions);

            specificOption = new KeyValuePair<string, ReportDiagnostic>(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Suppress);
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(new[] { specificOption });
            VerifyCSharpDiagnostic(test, parseOptions: null, compilationOptions: compilationOptions, expected: expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CompilationAnalyzer();
        }
    }
}