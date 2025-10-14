// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.ClassIsNotDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.ClassIsNotDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class ClassIsNotDiagnosticAnalyzerTests
    {
        [Fact]
        public async Task ClassNotDiagnosticAnalyzer_DiagnosticAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                namespace RoslynSandbox
                {
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    [DiagnosticAnalyzer(LanguageNames.CSharp)]
                    internal class NotAnAnalyzer
                    {
                    }
                }
                """,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic().WithLocation(7, 20).WithArguments("NotAnAnalyzer"));
#pragma warning restore RS0030 // Do not use banned APIs

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Namespace RoslynSandbox
                    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                    Friend Class NotAnAnalyzer
                    End Class
                End Namespace
                """,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic().WithLocation(6, 18).WithArguments("NotAnAnalyzer"));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task StaticClassNotDiagnosticAnalyzer_DiagnosticAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                namespace RoslynSandbox
                {
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    [DiagnosticAnalyzer(LanguageNames.CSharp)]
                    internal static class NotAnAnalyzer
                    {
                    }
                }
                """,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic().WithLocation(7, 27).WithArguments("NotAnAnalyzer"));
#pragma warning restore RS0030 // Do not use banned APIs

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Namespace RoslynSandbox
                    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                    Friend Module NotAnAnalyzer
                    End Module
                End Namespace
                """);
        }

        [Fact]
        public async Task ClassDiagnosticAnalyzer_NoDiagnosticAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                namespace RoslynSandbox
                {
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    [DiagnosticAnalyzer(LanguageNames.CSharp)]
                    internal class SomeAnalyzer : DiagnosticAnalyzer
                    {
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new System.NotImplementedException();

                        public override void Initialize(AnalysisContext context) => throw new System.NotImplementedException();
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Namespace RoslynSandbox
                    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                    Friend Class SomeAnalyzer
                        Inherits DiagnosticAnalyzer

                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                            Get
                                Throw New System.NotImplementedException()
                            End Get
                        End Property

                        Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                            Throw New System.NotImplementedException()
                        End Sub
                    End Class
                End Namespace
                """);
        }

        [Fact]
        public async Task ClassInheritsClassDiagnosticAnalyzer_NoDiagnosticAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                namespace RoslynSandbox
                {
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    internal abstract class SomeAnalyzerBase : DiagnosticAnalyzer
                    {
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new System.NotImplementedException();

                        public override void Initialize(AnalysisContext context) => throw new System.NotImplementedException();
                    }

                    [DiagnosticAnalyzer(LanguageNames.CSharp)]
                    internal class SomeAnalyzer : SomeAnalyzerBase
                    {
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Namespace RoslynSandbox
                    Friend MustInherit Class SomeAnalyzerBase
                        Inherits DiagnosticAnalyzer

                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                            Get
                                Throw New System.NotImplementedException()
                            End Get
                        End Property

                        Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                            Throw New System.NotImplementedException()
                        End Sub
                    End Class


                    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                    Friend Class SomeAnalyzer
                        Inherits SomeAnalyzerBase
                    End Class
                End Namespace
                """);
        }
    }
}
