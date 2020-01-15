// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<Microsoft.CodeAnalysis.Analyzers.UnitTests.UpgradeMSBuildWorkspaceAnalyzerTests.TestCSharpUpgradeMSBuildWorkspaceAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<Microsoft.CodeAnalysis.Analyzers.UnitTests.UpgradeMSBuildWorkspaceAnalyzerTests.TestVisualBasicUpgradeMSBuildWorkspaceAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class UpgradeMSBuildWorkspaceAnalyzerTests
    {
        [Fact]
        public async Task CSharp_VerifyWithMSBuildWorkspace()
        {
            const string source1 = @"
namespace Microsoft.CodeAnalysis.MSBuild
{
    public class MSBuildWorkspace
    {
        public static MSBuildWorkspace Create() => null;
    }
}";

            const string source2 = @"
using Microsoft.CodeAnalysis.MSBuild;

class Usage
{
    void M()
    {
        var workspace = MSBuildWorkspace.Create();
    }
}";

            await new VerifyCS.Test { TestState = { Sources = { source1, source2 } } }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyWithoutMSBuildWorkspace()
        {
            const string source = @"
using Microsoft.CodeAnalysis.MSBuild;

class Usage
{
    void M()
    {
        var workspace = MSBuildWorkspace.Create();
    }
}";

            await new VerifyCS.Test
            {
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(2,30): error CS0234: The type or namespace name 'MSBuild' does not exist in the namespace 'Microsoft.CodeAnalysis' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(2, 30, 2, 37).WithArguments("MSBuild", "Microsoft.CodeAnalysis"),
                        // Test0.cs(8,25): error CS0103: The name 'MSBuildWorkspace' does not exist in the current context
                        DiagnosticResult.CompilerError("CS0103").WithSpan(8, 25, 8, 41).WithArguments("MSBuildWorkspace"),
                        GetCSharpExpectedDiagnostic(8, 25),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task VisualBasic_VerifyWithMSBuildWorkspace()
        {
            const string source1 = @"
Namespace Microsoft.CodeAnalysis.MSBuild
    Public Class MSBuildWorkspace
        Public Shared Function Create() As MSBuildWorkspace
            Return Nothing
        End Function
    End Class
End Namespace";

            const string source2 = @"
Imports Microsoft.CodeAnalysis.MSBuild

Class Usage
    Sub M()
        Dim workspace = MSBuildWorkspace.Create()
    End Sub
End Class";

            await new VerifyVB.Test { TestState = { Sources = { source1, source2 } } }.RunAsync();
        }

        [Fact]
        public async Task VisualBasic_VerifyWithoutMSBuildWorkspace()
        {
            const string source = @"
Imports Microsoft.CodeAnalysis.MSBuild

Class Usage
    Sub M()
        Dim workspace = MSBuildWorkspace.Create()
    End Sub
End Class";

            await new VerifyVB.Test
            {
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.vb(6) : error BC30451: 'MSBuildWorkspace' is not declared. It may be inaccessible due to its protection level.
                        DiagnosticResult.CompilerError("BC30451").WithSpan(6, 25, 6, 41).WithArguments("MSBuildWorkspace"),
                        GetBasicExpectedDiagnostic(6, 25),
                    },
                },
            }.RunAsync();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column)
            => GetExpectedDiagnostic(line, column);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column)
            => GetExpectedDiagnostic(line, column);

        private static DiagnosticResult GetExpectedDiagnostic(int line, int column)
        {
            return new DiagnosticResult(DiagnosticIds.UpgradeMSBuildWorkspaceRuleId, DiagnosticSeverity.Warning)
               .WithLocation(line, column)
               .WithMessageFormat(CodeAnalysisDiagnosticsResources.UpgradeMSBuildWorkspaceMessage);
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via new() constraint: https://github.com/dotnet/roslyn-analyzers/issues/3199")]
        internal class TestCSharpUpgradeMSBuildWorkspaceAnalyzer : CSharpUpgradeMSBuildWorkspaceAnalyzer
        {
            public TestCSharpUpgradeMSBuildWorkspaceAnalyzer()
                : base(performAssemblyChecks: false)
            {
            }
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via new() constraint: https://github.com/dotnet/roslyn-analyzers/issues/3199")]
        internal class TestVisualBasicUpgradeMSBuildWorkspaceAnalyzer : VisualBasicUpgradeMSBuildWorkspaceAnalyzer
        {
            public TestVisualBasicUpgradeMSBuildWorkspaceAnalyzer()
                : base(performAssemblyChecks: false)
            {
            }
        }
    }
}
