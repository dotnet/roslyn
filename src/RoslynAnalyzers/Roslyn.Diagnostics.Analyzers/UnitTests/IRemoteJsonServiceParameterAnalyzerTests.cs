// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.IRemoteJsonServiceParameterAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class IRemoteJsonServiceParameterAnalyzerTests
    {
        private const string Boilerplate = """
            namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
            {
                public class RazorPinnedSolutionInfoWrapper
                {
                }
            }

            namespace Microsoft.CodeAnalysis.Razor.Remote
            {
                public interface IRemoteJsonService
                {
                }
            }

            namespace Microsoft.CodeAnalysis
            {
                public class DocumentId
                {
                }
            }
            """;

        [Fact]
        public Task RazorPinnedSolutionInfoWrapper_Report()
            => new VerifyCS.Test
            {
                TestCode = $$"""
                    using Microsoft.CodeAnalysis.ExternalAccess.Razor;
                    using Microsoft.CodeAnalysis.Razor.Remote;

                    interface ITestService : IRemoteJsonService
                    {
                        void TestMethod(RazorPinnedSolutionInfoWrapper {|RS0065:parameter|});
                    }

                    {{Boilerplate}}
                    """,
            }.RunAsync();

        [Fact]
        public Task NoProblematicTypes_NoReport()
            => new VerifyCS.Test
            {
                TestCode = $$"""
                    using Microsoft.CodeAnalysis.Razor.Remote;

                    interface ITestService : IRemoteJsonService
                    {
                        void TestMethod(string parameter);
                    }

                    {{Boilerplate}}
                    """,
            }.RunAsync();

        [Fact]
        public Task DocumentId_Report()
            => new VerifyCS.Test
            {
                TestCode = $$"""
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Razor.Remote;

                    interface ITestService : IRemoteJsonService
                    {
                        void TestMethod(DocumentId {|RS0065:parameter|});
                    }

                    {{Boilerplate}}
                    """,
            }.RunAsync();
    }
}
