// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.BannedApiAnalyzers;
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
        private static DiagnosticResult GetCSharpResultAt(int line, int column, string bannedSymbolName, string restrictedNamespaces)
        {
            return new DiagnosticResult(CSharpRestrictedInternalsVisibleToAnalyzer.Rule)
                .WithLocation(line, column)
                .WithArguments(bannedSymbolName, ApiProviderProjectName, restrictedNamespaces);
        }

        private static DiagnosticResult GetBasicResultAt(int line, int column, string bannedSymbolName, string restrictedNamespaces)
        {
            return new DiagnosticResult(BasicRestrictedInternalsVisibleToAnalyzer.Rule)
                .WithLocation(line, column)
                .WithArguments(bannedSymbolName, ApiProviderProjectName, restrictedNamespaces);
        }

        private const string ApiProviderProjectName = nameof(ApiProviderProjectName);
        private const string ApiConsumerProjectName = nameof(ApiConsumerProjectName);

        private const string CSharpApiProviderFileName = "ApiProviderFileName.cs";
        private const string VisualBasicApiProviderFileName = "ApiProviderFileName.vb";

        private static readonly CompilationOptions s_csharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        private static readonly CompilationOptions s_visualBasicCompilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        private const string CSharpRestrictedInternalsVisibleToAttribute = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal class RestrictedInternalsVisibleToAttribute : System.Attribute
    {
        public RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] restrictedNamespaces)
        {
        }
    }
}";
        private const string VisualBasicRestrictedInternalsVisibleToAttribute = @"
Namespace System.Runtime.CompilerServices
    <System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple:=True)>
    Friend Class RestrictedInternalsVisibleToAttribute
        Inherits System.Attribute

        Public Sub New(ByVal assemblyName As String, ParamArray restrictedNamespaces As String())
        End Sub
    End Class
End Namespace";

        private async Task VerifyCSharpAsync(string apiProviderSource, string apiConsumerSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { apiConsumerSource },
                },
                SolutionTransforms =
                {
                    (solution, projectId) => ApplySolutionTransforms(solution, projectId, apiProviderSource, LanguageNames.CSharp),
                },
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
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
                },
                SolutionTransforms =
                {
                    (solution, projectId) => ApplySolutionTransforms(solution, projectId, apiProviderSource, LanguageNames.VisualBasic),
                },
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static Solution ApplySolutionTransforms(
            Solution solution,
            ProjectId apiConsumerProjectId,
            string apiProviderSource,
            string language)
        {
            var restrictedInternalsVisibleToAttribute = language == LanguageNames.CSharp
                ? CSharpRestrictedInternalsVisibleToAttribute
                : VisualBasicRestrictedInternalsVisibleToAttribute;
            var compilationOptions = language == LanguageNames.CSharp
                ? s_csharpCompilationOptions
                : s_visualBasicCompilationOptions;
            var fileName = language == LanguageNames.CSharp
                ? CSharpApiProviderFileName
                : VisualBasicApiProviderFileName;

            var metadataReferences = solution.Projects.Single().MetadataReferences;
            solution = solution.WithProjectAssemblyName(apiConsumerProjectId, ApiConsumerProjectName);
            var apiProducerProject = solution
                .AddProject(ApiProviderProjectName, ApiProviderProjectName, language)
                .WithCompilationOptions(compilationOptions)
                .WithMetadataReferences(metadataReferences)
                .AddDocument(fileName, apiProviderSource + restrictedInternalsVisibleToAttribute)
                .Project;
            return apiProducerProject.Solution.AddProjectReference(apiConsumerProjectId, new ProjectReference(apiProducerProject.Id));
        }

        [Fact]
        public async Task CSharp_NoIVT_NoRestrictedIVT_NoDiagnostic()
        {
            var apiProviderSource = @"
namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                DiagnosticResult.CompilerError("CS0122")
                    .WithLocation(4, 15)
                    .WithMessage("'C1' is inaccessible due to its protection level"));
        }

        [Fact]
        public async Task Basic_NoIVT_NoRestrictedIVT_NoDiagnostic()
        {
            var apiProviderSource = @"
Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                DiagnosticResult.CompilerError("BC30389")
                    .WithLocation("Test0.vb", new LinePosition(2, 23), DiagnosticLocationOptions.IgnoreAdditionalLocations)
                    .WithMessage("'N1.C1' is not accessible in this context because it is 'Friend'."));
        }

        [Fact]
        public async Task CSharp_IVT_NoRestrictedIVT_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_NoRestrictedIVT_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_NoIVT_RestrictedIVT_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""NonExistentNamespace"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                DiagnosticResult.CompilerError("CS0122")
                    .WithLocation(4, 15)
                    .WithMessage("'C1' is inaccessible due to its protection level"));
        }

        [Fact]
        public async Task Basic_NoIVT_RestrictedIVT_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""NonExistentNamespace"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                new DiagnosticResult("BC30389", DiagnosticSeverity.Error)
                    .WithLocation("Test0.vb", new LinePosition(2, 23), DiagnosticLocationOptions.IgnoreAdditionalLocations)
                    .WithMessage("'N1.C1' is not accessible in this context because it is 'Friend'."));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_BasicScenario_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_BasicScenario_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_BasicScenario_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 12, "N1.C1", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_BasicScenario_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 24, "N1.C1", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_MultipleAttributes_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}

namespace N2
{
    internal class C2 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c1, N2.C2 c2)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_MultipleAttributes_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace

Namespace N2
    Friend Class C2
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c1 As N1.C1, c2 As N2.C2)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_MultipleAttributes_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

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
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c1, N2.C2 c2, N3.C3 c3)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 32, "N3.C3", "N1, N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_MultipleAttributes_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

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
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c1 As N1.C1, c2 As N2.C2, c3 As N3.C3)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(3, 51, "N3.C3", "N1, N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_ProjectNameMismatch_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""XYZ"", ""NonExistentNamespace"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_ProjectNameMismatch_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""XYZ"", ""NonExistentNamespace"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_ProjectNameMismatch_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""XYZ"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 12, "N1.C1", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_ProjectNameMismatch_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""XYZ"", ""N1"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 24, "N1.C1", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_NoRestrictedNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_NoRestrictedNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_QualifiedNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_QualifiedNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")>

Namespace N1
    Namespace N2
        Friend Class C1
        End Class
    End Namespace
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.N2.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_QualifiedNamespace_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }

    internal class C3 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c1, N1.C3 c3)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 25, "N1.C3", "N1.N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_QualifiedNamespace_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")>

Namespace N1
    Namespace N2
        Friend Class C1
        End Class
    End Namespace

    Friend Class C3
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c1 As N1.N2.C1, c3 As N1.C3)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 41, "N1.C3", "N1.N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_AncestorNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_AncestorNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Namespace N2
        Friend Class C1
        End Class
    End Namespace
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.N2.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_AncestorNamespace_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }

    internal class C2 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c1, N1.C2 c2)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 25, "N1.C2", "N1.N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_AncestorNamespace_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")>

Namespace N1
    Namespace N2
        Friend Class C1
        End Class
    End Namespace

    Friend Class C2
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c1 As N1.N2.C1, c2 As N1.C2)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 41, "N1.C2", "N1.N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_QualifiedAndAncestorNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_QualifiedAndAncestorNamespace_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"", ""N1.N2"")>

Namespace N1
    Namespace N2
        Friend Class C1
        End Class
    End Namespace
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.N2.C1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_NestedType_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { internal class Nested { } }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c, N1.C1.Nested nested)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_NestedType_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Friend Class C1
        Friend Class Nested
        End Class
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1, nested As N1.C1.Nested)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_NestedType_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1 { internal class Nested { } }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c, N1.C1.Nested nested)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 21, "N1.C1.Nested", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_NestedType_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
        Friend Class Nested
        End Class
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1, nested As N1.C1.Nested)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 41, "N1.C1.Nested", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInAttributes_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 : System.Attribute
    {
        public C1(object o) { }
    }
}";

            var apiConsumerSource = @"
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
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInAttributes_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Friend Class C1
        Inherits System.Attribute
        Public Sub New(obj As Object)
        End Sub
    End Class
End Namespace";

            var apiConsumerSource = @"
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
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInAttributes_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 : System.Attribute
    {
        public C1(object o) { }
    }
}";

            var apiConsumerSource = @"
[N1.C1(typeof(N1.C1))]      // 1, 2
class C2
{
    [N1.C1(typeof(N1.C1))]      // 3, 4
    private readonly int field;

    [N1.C1(typeof(N1.C1))]      // 5, 6
    private int Property { [N1.C1(typeof(N1.C1))] get; }    // 7, 8

    [N1.C1(typeof(N1.C1))]      // 9, 10
    private event System.EventHandler X;

    [N1.C1(typeof(N1.C1))]      // 11, 12
    [return: N1.C1(typeof(N1.C1))]      // 13, 14
    int M([N1.C1(typeof(N1.C1))]object c)   // 15, 16
    {
        return 0;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(2, 2, "N1.C1", "N2"),     // 1,
                GetCSharpResultAt(2, 15, "N1.C1", "N2"),    // 2,
                GetCSharpResultAt(5, 6, "N1.C1", "N2"),     // 3,
                GetCSharpResultAt(5, 19, "N1.C1", "N2"),    // 4,
                GetCSharpResultAt(8, 6, "N1.C1", "N2"),     // 5,
                GetCSharpResultAt(8, 19, "N1.C1", "N2"),    // 6,
                GetCSharpResultAt(9, 29, "N1.C1", "N2"),    // 7,
                GetCSharpResultAt(9, 42, "N1.C1", "N2"),    // 8,
                GetCSharpResultAt(11, 6, "N1.C1", "N2"),    // 9,
                GetCSharpResultAt(11, 19, "N1.C1", "N2"),   // 10,
                GetCSharpResultAt(14, 6, "N1.C1", "N2"),    // 11,
                GetCSharpResultAt(14, 19, "N1.C1", "N2"),   // 12,
                GetCSharpResultAt(15, 14, "N1.C1", "N2"),   // 13,
                GetCSharpResultAt(15, 27, "N1.C1", "N2"),   // 14,
                GetCSharpResultAt(16, 12, "N1.C1", "N2"),   // 15,
                GetCSharpResultAt(16, 25, "N1.C1", "N2")    // 16
                );
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInAttributes_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Friend Class C1
        Inherits System.Attribute
        Public Sub New(obj As Object)
        End Sub
    End Class
End Namespace";

            var apiConsumerSource = @"
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
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(2, 2, "N1.C1", "N2"),     // 1,
                GetBasicResultAt(2, 16, "N1.C1", "N2"),    // 2,
                GetBasicResultAt(4, 6, "N1.C1", "N2"),     // 3,
                GetBasicResultAt(4, 20, "N1.C1", "N2"),    // 4,
                GetBasicResultAt(7, 6, "N1.C1", "N2"),     // 5,
                GetBasicResultAt(7, 20, "N1.C1", "N2"),    // 6,
                GetBasicResultAt(10, 6, "N1.C1", "N2"),    // 7,
                GetBasicResultAt(10, 20, "N1.C1", "N2"),   // 8,
                GetBasicResultAt(13, 6, "N1.C1", "N2"),    // 9,
                GetBasicResultAt(13, 20, "N1.C1", "N2"),   // 10,
                GetBasicResultAt(14, 25, "N1.C1", "N2"),   // 11,
                GetBasicResultAt(14, 39, "N1.C1", "N2"),   // 12,
                GetBasicResultAt(14, 71, "N1.C1", "N2"),   // 13,
                GetBasicResultAt(14, 85, "N1.C1", "N2")    // 14
                );
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInDeclaration_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2 : N1.C1
{
    private readonly N1.C1 field;
    private N1.C1 Property { get; }
    private N1.C1 this[N1.C1 index] { get => null; }
    N1.C1 M(N1.C1 c)
    {
        return null;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInDeclaration_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"
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
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInDeclaration_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
    internal class C2 { }
    internal class C3 { }
    internal class C4 { }
    internal class C5 { }
    internal class C6 { }
    internal class C7 { }
}";

            var apiConsumerSource = @"
class B : N1.C1
{
    private readonly N1.C2 field;
    private N1.C3 Property { get; }
    private N1.C4 this[N1.C5 index] { get => null; }
    N1.C6 M(N1.C7 c)
    {
        return null;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(2, 11, "N1.C1", "N2"),
                GetCSharpResultAt(4, 22, "N1.C2", "N2"),
                GetCSharpResultAt(5, 13, "N1.C3", "N2"),
                GetCSharpResultAt(6, 13, "N1.C4", "N2"),
                GetCSharpResultAt(6, 24, "N1.C5", "N2"),
                GetCSharpResultAt(7, 5, "N1.C6", "N2"),
                GetCSharpResultAt(7, 13, "N1.C7", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInDeclaration_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

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
End Namespace";

            var apiConsumerSource = @"
Class B
    Inherits N1.C1

    Private ReadOnly field As N1.C2
    Private ReadOnly Property [Property] As N1.C3

    Private ReadOnly Property Item(index As N1.C4) As N1.C5
        Get
            Return Nothing
        End Get
    End Property

    Private Function M(c As N1.C6) As N1.C7
        Return Nothing
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 14, "N1.C1", "N2"),
                GetBasicResultAt(5, 31, "N1.C2", "N2"),
                GetBasicResultAt(6, 45, "N1.C3", "N2"),
                GetBasicResultAt(8, 45, "N1.C4", "N2"),
                GetBasicResultAt(8, 55, "N1.C5", "N2"),
                GetBasicResultAt(14, 29, "N1.C6", "N2"),
                GetBasicResultAt(14, 39, "N1.C7", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInExecutableCode_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

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
}";

            var apiConsumerSource = @"
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
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInExecutableCode_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

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
End Namespace";

            var apiConsumerSource = @"
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
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalMember_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    public class C1
    {
        internal int Field;
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return c.Field;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalMember_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Public Class C1
        Friend Field As Integer
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Function M(c As N1.C1) As Integer
        Return c.Field
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalField_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal int Field;
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return c.Field;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 16, "N1.C1.Field", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalField_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
        Friend Field As Integer
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Function M(c As N1.C1) As Integer
        Return c.Field
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(4, 16, "N1.C1.Field", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalMethod_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal int Method() => 0;
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return c.Method();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 16, "N1.C1.Method", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalMethod_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
        Friend Function Method() As Integer
            Return 0
        End Function
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Function M(c As N1.C1) As Integer
        Return c.Method()
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(4, 16, "N1.C1.Method", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    public class C1
    {
    }

    public static class C1Extensions
    {
        internal static int Method(this C1 c1) => 0;
    }
}";

            var apiConsumerSource = @"using static N1.C1Extensions;
class C2
{
    int M(N1.C1 c)
    {
        return c.Method();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Public Class C1
    End Class

    Public Module C1Extensions
        <System.Runtime.CompilerServices.ExtensionAttribute>
        Friend Function Method(c As C1) As Integer
            Return 0
        End Function
    End Module
End Namespace";

            var apiConsumerSource = @"Imports N1.C1Extensions
Class C2
    Private Function M(c As N1.C1) As Integer
        Return c.Method()
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
    }

    public static class C1Extensions
    {
        internal static int Method(this C1 c1) => 0;
    }
}";

            var apiConsumerSource = @"using static N1.C1Extensions;
class C2
{
    int M(N1.C1 c)
    {
        return c.Method();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 16, "N1.C1.Method", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
    End Class

    Public Module C1Extensions
        <System.Runtime.CompilerServices.ExtensionAttribute>
        Friend Function Method(c As C1) As Integer
            Return 0
        End Function
    End Module
End Namespace";

            var apiConsumerSource = @"Imports N1.C1Extensions
Class C2
    Private Function M(c As N1.C1) As Integer
        Return c.Method()
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(4, 16, "N1.C1.Method", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalProperty_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal int Property { get => 0; }
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return c.Property;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 16, "N1.C1.Property", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalProperty_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
        Friend ReadOnly Property [Property] As Integer
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Function M(c As N1.C1) As Integer
        Return c.[Property]
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(4, 16, "N1.C1.Property", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalEvent_Diagnostic()
        {
            var apiProviderSource = @"using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal event EventHandler Event;
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
        _ = c.Event;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 13, "N1.C1.Event", "N2"),
                DiagnosticResult.CompilerError("CS0070")
                    .WithLocation(6, 15)
                    .WithMessage("The event 'C1.Event' can only appear on the left hand side of += or -= (except when used from within the type 'C1')"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalEvent_Diagnostic()
        {
            var apiProviderSource = @"Imports System

<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
        Friend Event [Event] As EventHandler
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M(c As N1.C1)
        Dim unused = c.[Event]
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(4, 22, "N1.C1.Event", "N2"),
                DiagnosticResult.CompilerError("BC32022")
                    .WithLocation(4, 22)
                    .WithMessage("'Friend Event [Event] As EventHandler' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event."));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalConstructor_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal C1() { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M()
    {
        var c = new N1.C1();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 17, "N1.C1.C1", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_PublicTypeInternalConstructor_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Public Class C1
        Friend Sub New()
        End Sub
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Sub M()
        Dim c = New N1.C1()
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(4, 17, "N1.C1.New", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_Conversions_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal interface I1 { }
    public class C1 : I1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
        _ = (N1.I1)c;       // Explicit
        N1.I1 y = c;        // Implicit
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 14, "N1.I1", "N2"),
                GetCSharpResultAt(7, 9, "N1.I1", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_Conversions_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Friend Interface I1
    End Interface

    Public Class C1
        Implements I1
    End Class
End Namespace";

            var apiConsumerSource = @"
Class C2
    Private Function M(c As N1.C1) As Integer
        Dim x = CType(c, N1.I1)
        Dim y As N1.I1 = c
    End Function
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 26, "N1.I1", "N2"),
                GetCSharpResultAt(5, 18, "N1.I1", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInTypeArgument_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1<T>
    {
        public C1<object> GetC1<U>() => null;
    }

    internal class C3 { }
}";

            var apiConsumerSource = @"
using N1;
class C2 : C1<C3>
{
    void M(C1<C3> c1, C1<object> c2)
    {
        _ = c2.GetC1<C3>();
        _ = c2.GetC1<C1<C3>>();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInTypeArgument_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Friend Class C1(Of T)
        Public Function GetC1(Of U)() As C1(Of Object)
            Return Nothing
        End Function
    End Class

    Friend Class C3
    End Class
End Namespace";

            var apiConsumerSource = @"
Imports N1

Class C2
    Inherits C1(Of C3)

    Private Sub M(ByVal c1 As C1(Of C3), ByVal c2 As C1(Of Object))
        Dim unused1 = c2.GetC1(Of C3)()
        Dim unused2 = c2.GetC1(Of C1(Of C3))()
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInTypeArgument_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

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
";

            var apiConsumerSource = @"
using N1;
using N2;

class C2 : C1<C3>
{
    void M(C1<C4> c1, C1<object> c2)
    {
        _ = c2.GetC1<C5>();
        _ = c2.GetC1<C1<C6>>();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(5, 15, "N1.C3", "N2"),
                GetCSharpResultAt(7, 15, "N1.C4", "N2"),
                GetCSharpResultAt(9, 22, "N1.C5", "N2"),
                GetCSharpResultAt(10, 25, "N1.C6", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsageInTypeArgument_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

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
End Namespace";

            var apiConsumerSource = @"
Imports N1
Imports N2

Class C2
    Inherits C1(Of C3)

    Private Sub M(ByVal c1 As C1(Of C4), ByVal c2 As C1(Of Object))
        Dim unused1 = c2.GetC1(Of C5)()
        Dim unused2 = c2.GetC1(Of C1(Of C6))()
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(6, 20, "N1.C3", "N2"),
                GetCSharpResultAt(8, 37, "N1.C4", "N2"),
                GetCSharpResultAt(9, 35, "N1.C5", "N2"),
                GetCSharpResultAt(10, 41, "N1.C6", "N2"));
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsingAlias_NoDiagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"using ImportedC1 = N1.C1;
class C2
{
    void M(ImportedC1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsingAlias_NoDiagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"Imports ImportedC1 = N1.C1
Class C2
    Private Sub M(c As ImportedC1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource);
        }

        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsingAlias_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"using ImportedC1 = N1.C1;
class C2
{
    void M(ImportedC1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(4, 12, "N1.C1", "N2"));
        }

        [Fact]
        public async Task Basic_IVT_RestrictedIVT_UsingAlias_Diagnostic()
        {
            var apiProviderSource = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")>
<Assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")>

Namespace N1
    Friend Class C1
    End Class
End Namespace";

            var apiConsumerSource = @"Imports ImportedC1 = N1.C1
Class C2
    Private Sub M(c As ImportedC1)
    End Sub
End Class";

            await VerifyBasicAsync(apiProviderSource, apiConsumerSource,
                GetBasicResultAt(3, 24, "N1.C1", "N2"));
        }

        [Fact]
        [WorkItem(2655, "https://github.com/dotnet/roslyn-analyzers/issues/2655")]
        public async Task CSharp_IVT_RestrictedIVT_InternalOperators_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

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
}";

            var apiConsumerSource = @"
using N1;
class C2
{
    void M()
    {
        C c = 0;        // implicit conversion.
        c = (C)1.0f;    // Explicit conversion.
        c = c + 1;      // Binary operator.
        c++;            // Increment or decrement.
        c = -c;         // Unary operator.
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(7, 9, "N1.C", "N2"),
                GetCSharpResultAt(7, 15, "N1.C.implicit operator N1.C", "N2"),
                GetCSharpResultAt(8, 13, "N1.C.explicit operator N1.C", "N2"),
                GetCSharpResultAt(8, 14, "N1.C", "N2"),
                GetCSharpResultAt(9, 13, "N1.C.operator +", "N2"),
                GetCSharpResultAt(10, 9, "N1.C.operator ++", "N2"),
                GetCSharpResultAt(11, 13, "N1.C.operator -", "N2"));
        }

        [Fact]
        [WorkItem(2655, "https://github.com/dotnet/roslyn-analyzers/issues/2655")]
        public async Task CSharp_IVT_RestrictedIVT_TypeArgumentUsage_Diagnostic()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal struct C {}
}";

            var apiConsumerSource = @"
using N1;
namespace N2
{
    class G<T>
    {
        class N<U>
        { }

        unsafe void M()
        {
            var b = new G<C>();
            var c = new G<C>.N<int>();
            var d = new G<int>.N<C>();
            var e = new G<G<int>.N<C>>.N<int>();
            var f = new G<G<C>.N<int>>.N<int>();
            var g = new C[42];
            var h = new G<C[]>();
            fixed (C* i = &g[0]) { }
        }
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                // Test0.cs(12,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(12, 27, "N1.C", "N2"),
                // Test0.cs(13,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(13, 27, "N1.C", "N2"),
                // Test0.cs(14,34): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(14, 34, "N1.C", "N2"),
                // Test0.cs(15,36): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(15, 36, "N1.C", "N2"),
                // Test0.cs(16,29): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(16, 29, "N1.C", "N2"),
                // Test0.cs(17,25): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(17, 25, "N1.C", "N2"),
                // Test0.cs(18,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(18, 27, "N1.C", "N2"),
                // Test0.cs(19,20): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(19, 20, "N1.C", "N2"));
        }
    }
}
