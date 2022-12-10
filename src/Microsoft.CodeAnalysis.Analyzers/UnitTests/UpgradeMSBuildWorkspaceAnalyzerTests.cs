// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.CSharpUpgradeMSBuildWorkspaceAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.VisualBasicUpgradeMSBuildWorkspaceAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class UpgradeMSBuildWorkspaceAnalyzerTests
    {
        [Fact]
        public async Task CSharp_VerifyWithMSBuildWorkspaceAsync()
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
        public async Task CSharp_VerifyWithoutMSBuildWorkspaceAsync()
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

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                // Test0.cs(2,30): error CS0234: The type or namespace name 'MSBuild' does not exist in the namespace 'Microsoft.CodeAnalysis' (are you missing an assembly reference?)
                DiagnosticResult.CompilerError("CS0234").WithSpan(2, 30, 2, 37).WithArguments("MSBuild", "Microsoft.CodeAnalysis"),
                // Test0.cs(8,25): error CS0103: The name 'MSBuildWorkspace' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(8, 25, 8, 41).WithArguments("MSBuildWorkspace"));
        }

        [Fact]
        public async Task VisualBasic_VerifyWithMSBuildWorkspaceAsync()
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
        public async Task VisualBasic_VerifyWithoutMSBuildWorkspaceAsync()
        {
            const string source = @"
Imports Microsoft.CodeAnalysis.MSBuild

Class Usage
    Sub M()
        Dim workspace = MSBuildWorkspace.Create()
    End Sub
End Class";

            await VerifyVB.VerifyAnalyzerAsync(
                source,
                // Test0.vb(6) : error BC30451: 'MSBuildWorkspace' is not declared. It may be inaccessible due to its protection level.
                DiagnosticResult.CompilerError("BC30451").WithSpan(6, 25, 6, 41).WithArguments("MSBuildWorkspace"));
        }
    }
}
