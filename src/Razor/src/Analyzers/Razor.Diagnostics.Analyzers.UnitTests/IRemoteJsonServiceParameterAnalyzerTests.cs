// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Razor.Diagnostics.Analyzers.Test;

using VerifyCS = CSharpAnalyzerVerifier<IRemoteJsonServiceParameterAnalyzer>;

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
    public async Task RazorPinnedSolutionInfoWrapper_Report()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using Microsoft.CodeAnalysis.ExternalAccess.Razor;
                using Microsoft.CodeAnalysis.Razor.Remote;

                interface ITestService : IRemoteJsonService
                {
                    void TestMethod(RazorPinnedSolutionInfoWrapper {|RZD002:parameter|});
                }
                {{Boilerplate}}
                """
        }.RunAsync();
    }

    [Fact]
    public async Task NoProblematicTypes_NoReport()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using Microsoft.CodeAnalysis.Razor.Remote;
                
                interface ITestService : IRemoteJsonService
                {
                    void TestMethod(string parameter);
                }
                {{Boilerplate}}
                """
        }.RunAsync();
    }

    [Fact]
    public async Task DocumentId_Report()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Razor.Remote;

                interface ITestService : IRemoteJsonService
                {
                    void TestMethod(DocumentId {|RZD002:parameter|});
                }
                {{Boilerplate}}
                """
        }.RunAsync();
    }
}
