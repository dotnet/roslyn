// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.ImmutableObjectMethodAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.ImmutableObjectMethodAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class UseReturnValueFromImmutableObjectMethodTests
    {
        [Fact]
        public async Task CSharpVerifyDiagnosticsAsync()
        {
            DiagnosticResult documentExpected = GetCSharpExpectedDiagnostic(0, "Document", "WithText");
            DiagnosticResult projectExpected = GetCSharpExpectedDiagnostic(1, "Project", "AddDocument");
            DiagnosticResult solutionExpected = GetCSharpExpectedDiagnostic(2, "Solution", "AddProject");
            DiagnosticResult compilationExpected = GetCSharpExpectedDiagnostic(3, "Compilation", "RemoveAllSyntaxTrees");

            await VerifyCS.VerifyAnalyzerAsync("""
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Text;

                class TestSimple
                {
                    void M()
                    {
                        Document document = default(Document);
                        {|#0:document.WithText(default(SourceText))|};

                        Project project = default(Project);
                        {|#1:project.AddDocument("Sample.cs", default(SourceText))|};

                        Solution solution = default(Solution);
                        {|#2:solution.AddProject("Sample", "Sample", "CSharp")|};

                        Compilation compilation = default(Compilation);
                        {|#3:compilation.RemoveAllSyntaxTrees()|};
                    }
                }
                """, documentExpected, projectExpected, solutionExpected, compilationExpected);
        }

        [Fact]
        public async Task VisualBasicVerifyDiagnosticsAsync()
        {
            DiagnosticResult documentExpected = GetVisualBasicExpectedDiagnostic(0, "Document", "WithText");
            DiagnosticResult projectExpected = GetVisualBasicExpectedDiagnostic(1, "Project", "AddDocument");
            DiagnosticResult solutionExpected = GetVisualBasicExpectedDiagnostic(2, "Solution", "AddProject");
            DiagnosticResult compilationExpected = GetVisualBasicExpectedDiagnostic(3, "Compilation", "RemoveAllSyntaxTrees");

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Text

                Class TestSimple
                    Sub M()
                        Dim document As Document = Nothing
                        {|#0:document.WithText(Nothing)|}

                        Dim project As Project = Nothing
                        {|#1:project.AddDocument("Sample.cs", CType(Nothing, SourceText))|}

                        Dim solution As Solution = Nothing
                        {|#2:solution.AddProject("Sample", "Sample", "CSharp")|}

                        Dim compilation As Compilation = Nothing
                        {|#3:compilation.RemoveAllSyntaxTrees()|}
                    End Sub
                End Class
                """, documentExpected, projectExpected, solutionExpected, compilationExpected);
        }

        [Fact]
        public async Task CSharp_VerifyDiagnosticOnExtensionMethodAsync()
        {
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(0, "SyntaxNode", "WithLeadingTrivia");
            await VerifyCS.VerifyAnalyzerAsync("""
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Text;

                class TestExtensionMethodTrivia
                {
                    void M()
                    {
                        SyntaxNode node = default(SyntaxNode);
                        {|#0:node.WithLeadingTrivia<SyntaxNode>()|};
                    }
                }
                """, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnosticOnExtensionMethodAsync()
        {
            DiagnosticResult expected = GetVisualBasicExpectedDiagnostic(0, "SyntaxNode", "WithLeadingTrivia");
            await VerifyVB.VerifyAnalyzerAsync("""
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Text

                Class TestExtensionMethodTrivia
                    Sub M()
                        Dim node As SyntaxNode = Nothing
                        {|#0:node.WithLeadingTrivia()|}
                    End Sub
                End Class
                """, expected);
        }

        [Fact]
        public Task CSharp_VerifyNoDiagnosticAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
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
                }
                """);

        [Fact]
        public Task VisualBasic_VerifyNoDiagnosticAsync()
            => VerifyVB.VerifyAnalyzerAsync("""
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Text

                Namespace ConsoleApplication1
                    Class TestNoDiagnostic
                        Public Function M() As Document
                            Dim document As Document = Nothing
                            Dim newDocument = document.WithText(Nothing)
                            document = document.WithText(Nothing)

                            OtherMethod(document.WithText(Nothing))
                            Return document.WithText(Nothing)
                        End Function

                        Public Sub OtherMethod(document As Document)
                        End Sub
                    End Class
                End Namespace
                """);

        [Fact]
        public async Task CSharp_ReturnsVoid()
        {
            var source = """
                namespace Microsoft.CodeAnalysis
                {
                    public class Compilation
                    {
                        internal void AddSomething()
                        {
                        }

                        internal void M() => AddSomething();
                    }
                }
                """;
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task VisualBasic_ReturnsVoid()
        {
            var source = """
                Namespace Microsoft.CodeAnalysis
                    Public Class Compilation
                        Friend Sub AddSomething()
                        End Sub

                        Friend Sub M()
                            AddSomething()
                        End Sub
                    End Class
                End Namespace
                """;
            await VerifyVB.VerifyCodeFixAsync(source, source);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int markupKey, string objectName, string methodName) =>
            VerifyCS.Diagnostic().WithLocation(markupKey).WithArguments(objectName, methodName);

        private static DiagnosticResult GetVisualBasicExpectedDiagnostic(int markupKey, string objectName, string methodName) =>
            VerifyVB.Diagnostic().WithLocation(markupKey).WithArguments(objectName, methodName);
    }
}
