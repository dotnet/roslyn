// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class UseReturnValueFromImmutableObjectMethodTests : DiagnosticAnalyzerTestBase
    {
        [Fact]
        public void CSharpVerifyDiagnostics()
        {
            var source = @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

class TestSimple
{
    void M()
    {
        Document document = default(Document);
        document.WithText(default(SourceText));

        Project project = default(Project);
        project.AddDocument(""Sample.cs"", default(SourceText));

        Solution solution = default(Solution);
        solution.AddProject(""Sample"", ""Sample"", ""CSharp"");

        Compilation compilation = default(Compilation);
        compilation.RemoveAllSyntaxTrees();
    }
}
";
            DiagnosticResult documentExpected = GetCSharpExpectedDiagnostic(10, 9, "Document", "WithText");
            DiagnosticResult projectExpected = GetCSharpExpectedDiagnostic(13, 9, "Project", "AddDocument");
            DiagnosticResult solutionExpected = GetCSharpExpectedDiagnostic(16, 9, "Solution", "AddProject");
            DiagnosticResult compilationExpected = GetCSharpExpectedDiagnostic(19, 9, "Compilation", "RemoveAllSyntaxTrees");

            VerifyCSharp(source, documentExpected, projectExpected, solutionExpected, compilationExpected);
        }

        [Fact]
        public void CSharp_VerifyDiagnosticOnExtensionMethod()
        {
            var source = @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

class TestExtensionMethodTrivia
{
    void M()
    {
        SyntaxNode node = default(SyntaxNode);
        node.WithLeadingTrivia<SyntaxNode>();
    }
}";
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(10, 9, "SyntaxNode", "WithLeadingTrivia");
            VerifyCSharp(source, expected);
        }

        [Fact]
        public void CSharp_VerifyNoDiagnostic()
        {
            var source = @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleApplication1
{
    class TestNoDiagnostic
    {
        public Document M()
        {
            Document document = default(Document);
            var newDocument = document.WithText(default(SourceText));
            document = document.WithText(default(SourceText));

            OtherMethod(document.WithText(default(SourceText)));
            return document.WithText(default(SourceText));
        }

        public void OtherMethod(Document document)
        {
        }
    }
}";
            VerifyCSharp(source);
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpImmutableObjectMethodAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string objectName, string methodName)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, objectName, methodName);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string objectName, string methodName)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocation, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationMessage)
                .WithArguments(objectName, methodName);
        }
    }
}
