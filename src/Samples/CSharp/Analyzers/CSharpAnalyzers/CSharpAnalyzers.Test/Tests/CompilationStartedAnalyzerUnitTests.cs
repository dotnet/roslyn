// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace CSharpAnalyzers.Test
{
    [TestClass]
    public class CompilationStartedAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [TestMethod]
        public void Test1()
        {
            var test = @"
namespace MyInterfaces
{
    public interface Interface {}

    class MyInterfaceImpl : Interface
    {
    }

    class MyInterfaceImpl2 : Interface
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticIds.CompilationStartedAnalyzerRuleId,
                Message = string.Format(Resources.CompilationStartedAnalyzerMessageFormat, "MyInterfaceImpl2", CompilationStartedAnalyzer.DontInheritInterfaceTypeName),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 10, 11)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CompilationStartedAnalyzer();
        }
    }
}