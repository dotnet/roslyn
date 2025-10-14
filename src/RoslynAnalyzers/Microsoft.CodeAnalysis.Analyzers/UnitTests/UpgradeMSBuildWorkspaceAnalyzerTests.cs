// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
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
        private static readonly ReferenceAssemblies s_withDesktopWorkspaces = ReferenceAssemblies.NetFramework.Net46.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis.Workspaces.Common", "2.9.0")));

        private static readonly ReferenceAssemblies s_withMSBuildWorkspaces = ReferenceAssemblies.NetFramework.Net461.Default.AddPackages(ImmutableArray.Create(
            new PackageIdentity("Microsoft.CodeAnalysis.Workspaces.MSBuild", "2.9.0"),
            new PackageIdentity("Microsoft.CodeAnalysis.CSharp.Workspaces", "2.9.0"),
            new PackageIdentity("Microsoft.Build.Locator", "1.0.18")));

        private static Task VerifyCSharpAsync(string source, ReferenceAssemblies referenceAssemblies)
            => new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                ReferenceAssemblies = referenceAssemblies,
            }.RunAsync();

        private static Task VerifyVisualBasicAsync(string source, ReferenceAssemblies referenceAssemblies)
            => new VerifyVB.Test()
            {
                TestCode = source,
                FixedCode = source,
                ReferenceAssemblies = referenceAssemblies,
            }.RunAsync();

        [Fact]
        public Task CSharp_VerifyWithMSBuildWorkspaceAsync()
            => VerifyCSharpAsync("""
                using Microsoft.CodeAnalysis.MSBuild;

                class Usage
                {
                    void M()
                    {
                        var workspace = MSBuildWorkspace.Create();
                    }
                }
                """, s_withMSBuildWorkspaces);

        [Fact]
        public Task CSharp_VerifyWithoutMSBuildWorkspaceAsync()
            => VerifyCSharpAsync("""
                using Microsoft.CodeAnalysis.{|CS0234:MSBuild|};

                class Usage
                {
                    void M()
                    {
                        var workspace = [|{|CS0103:MSBuildWorkspace|}|].Create();
                    }
                }
                """, s_withDesktopWorkspaces);

        [Fact]
        public Task VisualBasic_VerifyWithMSBuildWorkspaceAsync()
            => VerifyVisualBasicAsync("""
                Imports Microsoft.CodeAnalysis.MSBuild

                Class Usage
                    Sub M()
                        Dim workspace = MSBuildWorkspace.Create()
                    End Sub
                End Class
                """, s_withMSBuildWorkspaces);

        [Fact]
        public Task VisualBasic_VerifyWithoutMSBuildWorkspaceAsync()
            => VerifyVisualBasicAsync("""
                Imports Microsoft.CodeAnalysis.MSBuild

                Class Usage
                    Sub M()
                        Dim workspace = [|{|BC30451:MSBuildWorkspace|}|].Create()
                    End Sub
                End Class
                """, s_withDesktopWorkspaces);
    }
}
