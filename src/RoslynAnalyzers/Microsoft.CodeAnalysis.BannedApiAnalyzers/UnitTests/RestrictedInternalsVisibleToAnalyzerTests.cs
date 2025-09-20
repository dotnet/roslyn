// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers.CSharpRestrictedInternalsVisibleToAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.BannedApiAnalyzers.BasicRestrictedInternalsVisibleToAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers.UnitTests
{
    public class RestrictedInternalsVisibleToAnalyzerTests
    {
        private static DiagnosticResult GetCSharpResultAt(int markupId, string bannedSymbolName, string restrictedNamespaces) =>
            VerifyCS.Diagnostic()
                .WithLocation(markupId)
                .WithArguments(bannedSymbolName, ApiProviderProjectName, restrictedNamespaces);

        private static DiagnosticResult GetBasicResultAt(int markupId, string bannedSymbolName, string restrictedNamespaces) =>
            VerifyVB.Diagnostic()
                .WithLocation(markupId)
                .WithArguments(bannedSymbolName, ApiProviderProjectName, restrictedNamespaces);

        private static DiagnosticResult GetBasicResultAt(int startLine, int startColumn, int endLine, int endColumn, string bannedSymbolName, string restrictedNamespaces) =>
            VerifyVB.Diagnostic()
                .WithSpan(startLine, startColumn, endLine, endColumn)
                .WithArguments(bannedSymbolName, ApiProviderProjectName, restrictedNamespaces);

        private const string ApiProviderProjectName = nameof(ApiProviderProjectName);
        private const string ApiConsumerProjectName = nameof(ApiConsumerProjectName);

        private const string CSharpApiProviderFileName = "ApiProviderFileName.cs";
        private const string VisualBasicApiProviderFileName = "ApiProviderFileName.vb";

        private const string CSharpRestrictedInternalsVisibleToAttribute = """

            namespace System.Runtime.CompilerServices
            {
                [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
                internal class RestrictedInternalsVisibleToAttribute : System.Attribute
                {
                    public RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] restrictedNamespaces)
                    {
                    }
                }
            }
            """;
        private const string VisualBasicRestrictedInternalsVisibleToAttribute = """

            Namespace System.Runtime.CompilerServices
                <System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple:=True)>
                Friend Class RestrictedInternalsVisibleToAttribute
                    Inherits System.Attribute

                    Public Sub New(ByVal assemblyName As String, ParamArray restrictedNamespaces As String())
                    End Sub
                End Class
            End Namespace
            """;

        private async Task VerifyCSharpAsync(string apiProviderSource, string apiConsumerSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { apiConsumerSource },
                    AdditionalProjects =
                    {
                        [ApiProviderProjectName] =
                        {
                            Sources =
                            {
                                (CSharpApiProviderFileName, apiProviderSource + CSharpRestrictedInternalsVisibleToAttribute),
                            },
                        },
                    },
                    AdditionalProjectReferences = { ApiProviderProjectName },
                },
                SolutionTransforms =
                {
                    ApplySolutionTransforms,
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private async Task VerifyBasicAsync(string apiProviderSource, string apiConsumerSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { apiConsumerSource },
                    AdditionalProjects =
                    {
                        [ApiProviderProjectName] =
                        {
                            Sources =
                            {
                                (VisualBasicApiProviderFileName, apiProviderSource + VisualBasicRestrictedInternalsVisibleToAttribute),
                            },
                        },
                    },
                    AdditionalProjectReferences = { ApiProviderProjectName },
                },
                SolutionTransforms =
                {
                    ApplySolutionTransforms,
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static Solution ApplySolutionTransforms(Solution solution, ProjectId apiConsumerProjectId)
        {
            return solution.WithProjectAssemblyName(apiConsumerProjectId, ApiConsumerProjectName);
        }

        [Fact]
        public Task CSharp_NoIVT_NoRestrictedIVT_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M(N1.{|CS0122:C1|} c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_NoIVT_NoRestrictedIVT_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As {|BC30389:N1.C1|})
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_NoRestrictedIVT_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_NoRestrictedIVT_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_NoIVT_RestrictedIVT_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "NonExistentNamespace")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M(N1.{|CS0122:C1|} c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_NoIVT_RestrictedIVT_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "NonExistentNamespace")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As {|BC30389:N1.C1|})
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_BasicScenario_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_BasicScenario_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_BasicScenario_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M({|#0:N1.C1|} c)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_BasicScenario_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As {|#0:N1.C1|})
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C1", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_MultipleAttributes_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C1 { }
                }

                namespace N2
                {
                    internal class C2 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c1, N2.C2 c2)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_MultipleAttributes_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace

                Namespace N2
                    Friend Class C2
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c1 As N1.C1, c2 As N2.C2)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_MultipleAttributes_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C1 { }
                }

                namespace N2
                {
                    internal class C2 { }
                }

                namespace N3
                {
                    internal class C3 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c1, N2.C2 c2, {|#0:N3.C3|} c3)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N3.C3", "N1, N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_MultipleAttributes_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace

                Namespace N2
                    Friend Class C2
                    End Class
                End Namespace

                Namespace N3
                    Friend Class C3
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c1 As N1.C1, c2 As N2.C2, c3 As {|#0:N3.C3|})
                    End Sub
                End Class
                """,
                GetCSharpResultAt(0, "N3.C3", "N1, N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_ProjectNameMismatch_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("XYZ", "NonExistentNamespace")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_ProjectNameMismatch_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("XYZ", "NonExistentNamespace")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_ProjectNameMismatch_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("XYZ", "N1")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M({|#0:N1.C1|} c)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_ProjectNameMismatch_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("XYZ", "N1")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As {|#0:N1.C1|})
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C1", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_NoRestrictedNamespace_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_NoRestrictedNamespace_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_QualifiedNamespace_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1.N2")]

                namespace N1
                {
                    namespace N2
                    {
                        internal class C1 { }
                    }
                }
                """, """

                class C2
                {
                    void M(N1.N2.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_QualifiedNamespace_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1.N2")>

                Namespace N1
                    Namespace N2
                        Friend Class C1
                        End Class
                    End Namespace
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.N2.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_QualifiedNamespace_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1.N2")]

                namespace N1
                {
                    namespace N2
                    {
                        internal class C1 { }
                    }

                    internal class C3 { }
                }
                """, """

                class C2
                {
                    void M(N1.N2.C1 c1, {|#0:N1.C3|} c3)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C3", "N1.N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_QualifiedNamespace_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1.N2")>

                Namespace N1
                    Namespace N2
                        Friend Class C1
                        End Class
                    End Namespace

                    Friend Class C3
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c1 As N1.N2.C1, c3 As {|#0:N1.C3|})
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C3", "N1.N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_AncestorNamespace_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    namespace N2
                    {
                        internal class C1 { }
                    }
                }
                """, """

                class C2
                {
                    void M(N1.N2.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_AncestorNamespace_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Namespace N2
                        Friend Class C1
                        End Class
                    End Namespace
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.N2.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_AncestorNamespace_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1.N2")]

                namespace N1
                {
                    namespace N2
                    {
                        internal class C1 { }
                    }

                    internal class C2 { }
                }
                """, """

                class C2
                {
                    void M(N1.N2.C1 c1, {|#0:N1.C2|} c2)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C2", "N1.N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_AncestorNamespace_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1.N2")>

                Namespace N1
                    Namespace N2
                        Friend Class C1
                        End Class
                    End Namespace

                    Friend Class C2
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c1 As N1.N2.C1, c2 As {|#0:N1.C2|})
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C2", "N1.N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_QualifiedAndAncestorNamespace_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1", "N1.N2")]

                namespace N1
                {
                    namespace N2
                    {
                        internal class C1 { }
                    }
                }
                """, """

                class C2
                {
                    void M(N1.N2.C1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_QualifiedAndAncestorNamespace_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1", "N1.N2")>

                Namespace N1
                    Namespace N2
                        Friend Class C1
                        End Class
                    End Namespace
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.N2.C1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_NestedType_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1 { internal class Nested { } }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c, N1.C1.Nested nested)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_NestedType_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1
                        Friend Class Nested
                        End Class
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1, nested As N1.C1.Nested)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_NestedType_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1 { internal class Nested { } }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c, {|#0:N1.C1.Nested|} nested)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.Nested", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_NestedType_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                        Friend Class Nested
                        End Class
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1, nested As {|#0:N1.C1.Nested|})
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C1.Nested", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInAttributes_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1 : System.Attribute
                    {
                        public C1(object o) { }
                    }
                }
                """, """

                [N1.C1(typeof(N1.C1))]
                class C2
                {
                    [N1.C1(typeof(N1.C1))]
                    private readonly int field;

                    [N1.C1(typeof(N1.C1))]
                    private int Property { [N1.C1(typeof(N1.C1))] get; }

                    [N1.C1(typeof(N1.C1))]
                    private event System.EventHandler X;

                    [N1.C1(typeof(N1.C1))]
                    [return: N1.C1(typeof(N1.C1))]
                    int M([N1.C1(typeof(N1.C1))]object c)
                    {
                        return 0;
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInAttributes_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1
                        Inherits System.Attribute
                        Public Sub New(obj As Object)
                        End Sub
                    End Class
                End Namespace
                """, """

                <N1.C1(GetType(N1.C1))>
                Class C2
                    <N1.C1(GetType(N1.C1))>
                    Private ReadOnly field As Integer

                    <N1.C1(GetType(N1.C1))>
                    Private ReadOnly Property [Property] As Integer

                    <N1.C1(GetType(N1.C1))>
                    Private Event X As System.EventHandler

                    <N1.C1(GetType(N1.C1))>
                    Private Function M(<N1.C1(GetType(N1.C1))> ByVal c As Object) As <N1.C1(GetType(N1.C1))> Integer
                        Return 0
                    End Function
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInAttributes_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C1 : System.Attribute
                    {
                        public C1(object o) { }
                    }
                }
                """, """

                [{|#16:{|#0:N1.C1|}(typeof({|#1:N1.C1|}))|}]
                class C2
                {
                    [{|#17:{|#2:N1.C1|}(typeof({|#3:N1.C1|}))|}]
                    private readonly int field;

                    [{|#18:{|#4:N1.C1|}(typeof({|#5:N1.C1|}))|}]
                    private int Property { [{|#19:{|#6:N1.C1|}(typeof({|#7:N1.C1|}))|}] get; }

                    [{|#20:{|#8:N1.C1|}(typeof({|#9:N1.C1|}))|}]
                    private event System.EventHandler X;

                    [{|#21:{|#10:N1.C1|}(typeof({|#11:N1.C1|}))|}]
                    [return: {|#22:{|#12:N1.C1|}(typeof({|#13:N1.C1|}))|}]
                    int M([{|#23:{|#14:N1.C1|}(typeof({|#15:N1.C1|}))|}]object c)
                    {
                        return 0;
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1", "N2"),
                GetCSharpResultAt(1, "N1.C1", "N2"),
                GetCSharpResultAt(2, "N1.C1", "N2"),
                GetCSharpResultAt(3, "N1.C1", "N2"),
                GetCSharpResultAt(4, "N1.C1", "N2"),
                GetCSharpResultAt(5, "N1.C1", "N2"),
                GetCSharpResultAt(6, "N1.C1", "N2"),
                GetCSharpResultAt(7, "N1.C1", "N2"),
                GetCSharpResultAt(8, "N1.C1", "N2"),
                GetCSharpResultAt(9, "N1.C1", "N2"),
                GetCSharpResultAt(10, "N1.C1", "N2"),
                GetCSharpResultAt(11, "N1.C1", "N2"),
                GetCSharpResultAt(12, "N1.C1", "N2"),
                GetCSharpResultAt(13, "N1.C1", "N2"),
                GetCSharpResultAt(14, "N1.C1", "N2"),
                GetCSharpResultAt(15, "N1.C1", "N2"),
                GetCSharpResultAt(16, "N1.C1.C1", "N2"),
                GetCSharpResultAt(17, "N1.C1.C1", "N2"),
                GetCSharpResultAt(18, "N1.C1.C1", "N2"),
                GetCSharpResultAt(19, "N1.C1.C1", "N2"),
                GetCSharpResultAt(20, "N1.C1.C1", "N2"),
                GetCSharpResultAt(21, "N1.C1.C1", "N2"),
                GetCSharpResultAt(22, "N1.C1.C1", "N2"),
                GetCSharpResultAt(23, "N1.C1.C1", "N2")
                );

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInAttributes_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C1
                        Inherits System.Attribute
                        Public Sub New(obj As Object)
                        End Sub
                    End Class
                End Namespace
                """, """

                <{|#14:{|#0:N1.C1|}(GetType({|#1:N1.C1|}))|}>
                Class C2
                    <{|#15:{|#2:N1.C1|}(GetType({|#3:N1.C1|}))|}>
                    Private ReadOnly field As Integer

                    <{|#16:{|#4:N1.C1|}(GetType({|#5:N1.C1|}))|}>
                    Private ReadOnly Property [Property] As Integer

                    <{|#17:{|#6:N1.C1|}(GetType({|#7:N1.C1|}))|}>
                    Private Event X As System.EventHandler

                    <{|#18:{|#8:N1.C1|}(GetType({|#9:N1.C1|}))|}>
                    Private Function M(<{|#19:{|#10:N1.C1|}(GetType({|#11:N1.C1|}))|}> ByVal c As Object) As <{|#20:{|#12:N1.C1|}(GetType({|#13:N1.C1|}))|}> Integer
                        Return 0
                    End Function
                End Class
                """,
                GetBasicResultAt(0, "N1.C1", "N2"),
                GetBasicResultAt(1, "N1.C1", "N2"),
                GetBasicResultAt(2, "N1.C1", "N2"),
                GetBasicResultAt(3, "N1.C1", "N2"),
                GetBasicResultAt(4, "N1.C1", "N2"),
                GetBasicResultAt(5, "N1.C1", "N2"),
                GetBasicResultAt(6, "N1.C1", "N2"),
                GetBasicResultAt(7, "N1.C1", "N2"),
                GetBasicResultAt(8, "N1.C1", "N2"),
                GetBasicResultAt(9, "N1.C1", "N2"),
                GetBasicResultAt(10, "N1.C1", "N2"),
                GetBasicResultAt(11, "N1.C1", "N2"),
                GetBasicResultAt(12, "N1.C1", "N2"),
                GetBasicResultAt(13, "N1.C1", "N2"),
                GetBasicResultAt(14, "N1.C1.New", "N2"),
                GetBasicResultAt(15, "N1.C1.New", "N2"),
                GetBasicResultAt(16, "N1.C1.New", "N2"),
                GetBasicResultAt(17, "N1.C1.New", "N2"),
                GetBasicResultAt(18, "N1.C1.New", "N2"),
                GetBasicResultAt(19, "N1.C1.New", "N2"),
                GetBasicResultAt(20, "N1.C1.New", "N2")
                );

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInDeclaration_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """

                class C2 : N1.C1
                {
                    private readonly N1.C1 field;
                    private N1.C1 Property { get; }
                    private N1.C1 this[N1.C1 index] { get => null; }
                    N1.C1 M(N1.C1 c)
                    {
                        return null;
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInDeclaration_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Inherits N1.C1

                    Private ReadOnly field As N1.C1
                    Private ReadOnly Property [Property] As N1.C1

                    Private ReadOnly Property Item(index As N1.C1) As N1.C1
                        Get
                            Return Nothing
                        End Get
                    End Property

                    Private Function M(c As N1.C1) As N1.C1
                        Return Nothing
                    End Function
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInDeclaration_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C1 { }
                    internal class C2 { }
                    internal class C3 { }
                    internal class C4 { }
                    internal class C5 { }
                    internal class C6 { }
                    internal class C7 { }
                }
                """, """

                class B : {|#0:N1.C1|}
                {
                    private readonly {|#1:N1.C2|} field;
                    private {|#2:N1.C3|} Property { get; }
                    private {|#3:N1.C4|} this[{|#4:N1.C5|} index] { get => null; }
                    {|#5:N1.C6|} M({|#6:N1.C7|} c)
                    {
                        return null;
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1", "N2"),
                GetCSharpResultAt(1, "N1.C2", "N2"),
                GetCSharpResultAt(2, "N1.C3", "N2"),
                GetCSharpResultAt(3, "N1.C4", "N2"),
                GetCSharpResultAt(4, "N1.C5", "N2"),
                GetCSharpResultAt(5, "N1.C6", "N2"),
                GetCSharpResultAt(6, "N1.C7", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInDeclaration_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C1
                    End Class
                    Friend Class C2
                    End Class
                    Friend Class C3
                    End Class
                    Friend Class C4
                    End Class
                    Friend Class C5
                    End Class
                    Friend Class C6
                    End Class
                    Friend Class C7
                    End Class
                End Namespace
                """, """

                Class B
                    Inherits {|#0:N1.C1|}

                    Private ReadOnly field As {|#1:N1.C2|}
                    Private ReadOnly Property [Property] As {|#2:N1.C3|}

                    Private ReadOnly Property Item(index As {|#3:N1.C4|}) As {|#4:N1.C5|}
                        Get
                            Return Nothing
                        End Get
                    End Property

                    Private Function M(c As {|#5:N1.C6|}) As {|#6:N1.C7|}
                        Return Nothing
                    End Function
                End Class
                """,
                GetBasicResultAt(0, "N1.C1", "N2"),
                GetBasicResultAt(1, "N1.C2", "N2"),
                GetBasicResultAt(2, "N1.C3", "N2"),
                GetBasicResultAt(3, "N1.C4", "N2"),
                GetBasicResultAt(4, "N1.C5", "N2"),
                GetBasicResultAt(5, "N1.C6", "N2"),
                GetBasicResultAt(6, "N1.C7", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInExecutableCode_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1 : System.Attribute
                    {
                        public C1(object o) { }
                        public C1 GetC() => this;
                        public C1 GetC(C1 c) => c;
                        public object GetObject() => this;
                        public C1 Field;
                        public C1 Property { get; set; }
                    }
                }
                """, """

                class C2
                {
                    // Field initializer
                    private readonly N1.C1 field = new N1.C1(null);

                    // Property initializer
                    private N1.C1 Property { get; } = new N1.C1(null);

                    void M(object c)
                    {
                        var x = new N1.C1(null);    // Object creation
                        N1.C1 y = x.GetC();         // Invocation
                        var z = y.GetC(x);          // Parameter type
                        _ = (N1.C1)z.GetObject();   // Conversion
                        _ = z.Field;                // Field reference
                        _ = z.Property;             // Property reference
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInExecutableCode_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1
                        Inherits System.Attribute

                        Public Sub New(ByVal o As Object)
                        End Sub

                        Public Function GetC() As C1
                            Return Me
                        End Function

                        Public Function GetC(ByVal c As C1) As C1
                            Return c
                        End Function

                        Public Function GetObject() As Object
                            Return Me
                        End Function

                        Public Field As C1
                        Public Property [Property] As C1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private ReadOnly field As N1.C1 = New N1.C1(Nothing)
                    Private ReadOnly Property [Property] As N1.C1 = New N1.C1(Nothing)

                    Private Sub M(ByVal c As Object)
                        Dim x = New N1.C1(Nothing)
                        Dim y As N1.C1 = x.GetC()
                        Dim z = y.GetC(x)
                        Dim unused1 = CType(z.GetObject(), N1.C1)
                        Dim unused2 = z.Field
                        Dim unused3 = z.[Property]
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalMember_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    public class C1
                    {
                        internal int Field;
                    }
                }
                """, """

                class C2
                {
                    int M(N1.C1 c)
                    {
                        return c.Field;
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalMember_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Public Class C1
                        Friend Field As Integer
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Return c.Field
                    End Function
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalField_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1
                    {
                        internal int Field;
                    }
                }
                """, """

                class C2
                {
                    int M(N1.C1 c)
                    {
                        return {|#0:c.Field|};
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.Field", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalField_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                        Friend Field As Integer
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Return {|#0:c.Field|}
                    End Function
                End Class
                """,
                GetBasicResultAt(0, "N1.C1.Field", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalMethod_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1
                    {
                        internal int Method() => 0;
                    }
                }
                """, """

                class C2
                {
                    int M(N1.C1 c)
                    {
                        return {|#0:c.Method()|};
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.Method", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalMethod_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                        Friend Function Method() As Integer
                            Return 0
                        End Function
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Return {|#0:c.Method()|}
                    End Function
                End Class
                """,
                GetBasicResultAt(0, "N1.C1.Method", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    public class C1
                    {
                    }

                    public static class C1Extensions
                    {
                        internal static int Method(this C1 c1) => 0;
                    }
                }
                """, """
                using static N1.C1Extensions;
                class C2
                {
                    int M(N1.C1 c)
                    {
                        return c.Method();
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Public Class C1
                    End Class

                    Public Module C1Extensions
                        <System.Runtime.CompilerServices.ExtensionAttribute>
                        Friend Function Method(c As C1) As Integer
                            Return 0
                        End Function
                    End Module
                End Namespace
                """, """
                Imports N1.C1Extensions
                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Return c.Method()
                    End Function
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1
                    {
                    }

                    public static class C1Extensions
                    {
                        internal static int Method(this C1 c1) => 0;
                    }
                }
                """, """
                using static N1.C1Extensions;
                class C2
                {
                    int M(N1.C1 c)
                    {
                        return {|#0:c.Method()|};
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.Method", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                    End Class

                    Public Module C1Extensions
                        <System.Runtime.CompilerServices.ExtensionAttribute>
                        Friend Function Method(c As C1) As Integer
                            Return 0
                        End Function
                    End Module
                End Namespace
                """, """
                Imports N1.C1Extensions
                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Return {|#0:c.Method()|}
                    End Function
                End Class
                """,
                GetBasicResultAt(0, "N1.C1.Method", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalProperty_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1
                    {
                        internal int Property { get => 0; }
                    }
                }
                """, """

                class C2
                {
                    int M(N1.C1 c)
                    {
                        return {|#0:c.Property|};
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.Property", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalProperty_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                        Friend ReadOnly Property [Property] As Integer
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Return {|#0:c.[Property]|}
                    End Function
                End Class
                """,
                GetBasicResultAt(0, "N1.C1.Property", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalEvent_DiagnosticAsync()
            => VerifyCSharpAsync("""
                using System;

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1
                    {
                        internal event EventHandler Event;
                    }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c)
                    {
                        _ = {|#0:c.{|CS0070:Event|}|};
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.Event", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalEvent_DiagnosticAsync()
            => VerifyBasicAsync("""
                Imports System

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                        Friend Event [Event] As EventHandler
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M(c As N1.C1)
                        Dim unused = {|BC32022:c.[Event]|}
                    End Sub
                End Class
                """,
                GetBasicResultAt(4, 22, 4, 31, "N1.C1.Event", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_PublicTypeInternalConstructor_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    public class C1
                    {
                        internal C1() { }
                    }
                }
                """, """

                class C2
                {
                    void M()
                    {
                        var c = {|#0:new N1.C1()|};
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1.C1", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_PublicTypeInternalConstructor_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Public Class C1
                        Friend Sub New()
                        End Sub
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Sub M()
                        Dim c = {|#0:New N1.C1()|}
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C1.New", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_Conversions_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal interface I1 { }
                    public class C1 : I1 { }
                }
                """, """

                class C2
                {
                    void M(N1.C1 c)
                    {
                        _ = ({|#0:N1.I1|})c;       // Explicit
                        {|#1:N1.I1|} y = c;        // Implicit
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.I1", "N2"),
                GetCSharpResultAt(1, "N1.I1", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_Conversions_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Interface I1
                    End Interface

                    Public Class C1
                        Implements I1
                    End Class
                End Namespace
                """, """

                Class C2
                    Private Function M(c As N1.C1) As Integer
                        Dim x = CType(c, {|#0:N1.I1|})
                        Dim y As {|#1:N1.I1|} = c
                    End Function
                End Class
                """,
                GetCSharpResultAt(0, "N1.I1", "N2"),
                GetCSharpResultAt(1, "N1.I1", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInTypeArgument_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1<T>
                    {
                        public C1<object> GetC1<U>() => null;
                    }

                    internal class C3 { }
                }
                """, """

                using N1;
                class C2 : C1<C3>
                {
                    void M(C1<C3> c1, C1<object> c2)
                    {
                        _ = c2.GetC1<C3>();
                        _ = c2.GetC1<C1<C3>>();
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInTypeArgument_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1(Of T)
                        Public Function GetC1(Of U)() As C1(Of Object)
                            Return Nothing
                        End Function
                    End Class

                    Friend Class C3
                    End Class
                End Namespace
                """, """

                Imports N1

                Class C2
                    Inherits C1(Of C3)

                    Private Sub M(ByVal c1 As C1(Of C3), ByVal c2 As C1(Of Object))
                        Dim unused1 = c2.GetC1(Of C3)()
                        Dim unused2 = c2.GetC1(Of C1(Of C3))()
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsageInTypeArgument_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C3 { }
                    internal class C4 { }
                    internal class C5 { }
                    internal class C6 { }

                }

                namespace N2
                {
                    internal class C1<T>
                    {
                        public C1<object> GetC1<U>() => null;
                    }
                }

                """, """

                using N1;
                using N2;

                class C2 : C1<{|#0:C3|}>
                {
                    void M(C1<{|#1:C4|}> c1, C1<object> c2)
                    {
                        _ = c2.GetC1<{|#2:C5|}>();
                        _ = c2.GetC1<C1<{|#3:C6|}>>();
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C3", "N2"),
                GetCSharpResultAt(1, "N1.C4", "N2"),
                GetCSharpResultAt(2, "N1.C5", "N2"),
                GetCSharpResultAt(3, "N1.C6", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsageInTypeArgument_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C3
                    End Class
                    Friend Class C4
                    End Class
                    Friend Class C5
                    End Class
                    Friend Class C6
                    End Class
                End Namespace

                Namespace N2
                    Friend Class C1(Of T)
                        Public Function GetC1(Of U)() As C1(Of Object)
                            Return Nothing
                        End Function
                    End Class
                End Namespace
                """, """

                Imports N1
                Imports N2

                Class C2
                    Inherits C1(Of {|#0:C3|})

                    Private Sub M(ByVal c1 As C1(Of {|#1:C4|}), ByVal c2 As C1(Of Object))
                        Dim unused1 = c2.GetC1(Of {|#2:C5|})()
                        Dim unused2 = c2.GetC1(Of C1(Of {|#3:C6|}))()
                    End Sub
                End Class
                """,
                GetCSharpResultAt(0, "N1.C3", "N2"),
                GetCSharpResultAt(1, "N1.C4", "N2"),
                GetCSharpResultAt(2, "N1.C5", "N2"),
                GetCSharpResultAt(3, "N1.C6", "N2"));

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsingAlias_NoDiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """
                using ImportedC1 = N1.C1;
                class C2
                {
                    void M(ImportedC1 c)
                    {
                    }
                }
                """);

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsingAlias_NoDiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N1")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """
                Imports ImportedC1 = N1.C1
                Class C2
                    Private Sub M(c As ImportedC1)
                    End Sub
                End Class
                """);

        [Fact]
        public Task CSharp_IVT_RestrictedIVT_UsingAlias_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C1 { }
                }
                """, """
                using ImportedC1 = N1.C1;
                class C2
                {
                    void M({|#0:ImportedC1|} c)
                    {
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C1", "N2"));

        [Fact]
        public Task Basic_IVT_RestrictedIVT_UsingAlias_DiagnosticAsync()
            => VerifyBasicAsync("""

                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")>
                <Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")>

                Namespace N1
                    Friend Class C1
                    End Class
                End Namespace
                """, """
                Imports ImportedC1 = N1.C1
                Class C2
                    Private Sub M(c As {|#0:ImportedC1|})
                    End Sub
                End Class
                """,
                GetBasicResultAt(0, "N1.C1", "N2"));

        [Fact]
        [WorkItem(2655, "https://github.com/dotnet/roslyn-analyzers/issues/2655")]
        public Task CSharp_IVT_RestrictedIVT_InternalOperators_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal class C
                    {
                        public static implicit operator C(int i) => new C();
                        public static explicit operator C(float f) => new C();
                        public static C operator +(C c, int i) => c;
                        public static C operator ++(C c) => c;
                        public static C operator -(C c) => c;
                    }
                }
                """, """

                using N1;
                class C2
                {
                    void M()
                    {
                        {|#0:C|} c = {|#1:0|};        // implicit conversion.
                        c = {|#2:({|#3:C|})1.0f|};    // Explicit conversion.
                        c = {|#4:c + 1|};      // Binary operator.
                        {|#5:c++|};            // Increment or decrement.
                        c = {|#6:-c|};         // Unary operator.
                    }
                }
                """,
                GetCSharpResultAt(0, "N1.C", "N2"),
                GetCSharpResultAt(1, "N1.C.implicit operator N1.C", "N2"),
                GetCSharpResultAt(2, "N1.C.explicit operator N1.C", "N2"),
                GetCSharpResultAt(3, "N1.C", "N2"),
                GetCSharpResultAt(4, "N1.C.operator +", "N2"),
                GetCSharpResultAt(5, "N1.C.operator ++", "N2"),
                GetCSharpResultAt(6, "N1.C.operator -", "N2"));

        [Fact]
        [WorkItem(2655, "https://github.com/dotnet/roslyn-analyzers/issues/2655")]
        public Task CSharp_IVT_RestrictedIVT_TypeArgumentUsage_DiagnosticAsync()
            => VerifyCSharpAsync("""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApiConsumerProjectName")]
                [assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo("ApiConsumerProjectName", "N2")]

                namespace N1
                {
                    internal struct C {}
                }
                """, """

                using N1;
                namespace N2
                {
                    class G<T>
                    {
                        class N<U>
                        { }

                        unsafe void M()
                        {
                            var b = new G<{|#0:C|}>();
                            var c = new G<{|#1:C|}>.N<int>();
                            var d = new G<int>.N<{|#2:C|}>();
                            var e = new G<G<int>.N<{|#3:C|}>>.N<int>();
                            var f = new G<G<{|#4:C|}>.N<int>>.N<int>();
                            var g = new {|#5:C|}[42];
                            var h = new G<{|#6:C|}[]>();
                            fixed ({|#7:C|}* i = &g[0]) { }
                        }
                    }
                }
                """,
                // Test0.cs(12,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(0, "N1.C", "N2"),
                // Test0.cs(13,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(1, "N1.C", "N2"),
                // Test0.cs(14,34): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(2, "N1.C", "N2"),
                // Test0.cs(15,36): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(3, "N1.C", "N2"),
                // Test0.cs(16,29): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(4, "N1.C", "N2"),
                // Test0.cs(17,25): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(5, "N1.C", "N2"),
                // Test0.cs(18,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(6, "N1.C", "N2"),
                // Test0.cs(19,20): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(7, "N1.C", "N2"));
    }
}
