// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests.Testing;

public sealed class RunTestsTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task RunTestCodeLensExecutesSelectedTest(bool useMtp)
    {
        var workspaceContent = LspWorkspaceContent.Empty
            .WithFile("Tests.csproj", CreateProjectContent(useMtp))
            .WithMarkupFile("Tests.cs", """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                namespace TestProject;

                [TestClass]
                public sealed class Tests
                {
                    [TestMethod]
                    public void {|testMethod:SelectedTest|}()
                    {
                    }

                    [TestMethod]
                    public void NotSelectedTest()
                        => Assert.Fail("The method-level CodeLens should not run this test.");
                }
                """)
            .WithLoadPath("Tests.csproj")
            .WithRestore();

        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, LspServerLaunchOptions.Default);
        var testMethodLocation = testLspServer.GetLocations("testMethod").Single();

        var codeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]>(
            LSP.Methods.TextDocumentCodeLensName,
            new LSP.CodeLensParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = testMethodLocation.DocumentUri },
            },
            CancellationToken.None);

        Assert.NotNull(codeLenses);
        var runTestsCodeLens = Assert.Single(
            codeLenses,
            codeLens => codeLens.Range == testMethodLocation.Range &&
                codeLens.Command?.CommandIdentifier == CodeLensHandler.RunTestsCommandIdentifier &&
                codeLens.Command.Title == FeaturesResources.Run_Test);
        Assert.NotNull(runTestsCodeLens.Command);
        Assert.NotNull(runTestsCodeLens.Command.Arguments);

        var argument = Assert.IsType<JsonElement>(Assert.Single(runTestsCodeLens.Command.Arguments));
        var runTestsParams = argument.Deserialize<RunTestsParams>(ProtocolConversions.LspJsonSerializerOptions);
        Assert.NotNull(runTestsParams);

        var results = await testLspServer.ExecuteRequestAsync<RunTestsParams, RunTestsPartialResult[]>(
            "textDocument/runTests",
            runTestsParams,
            CancellationToken.None);

        Assert.NotNull(results);
        var finalProgress = results
            .Select(static result => result.Progress)
            .Where(static progress => progress.HasValue)
            .Select(static progress => progress.GetValueOrDefault())
            .Last();

        Assert.Equal(new TestProgress(TestsPassed: 1, TestsFailed: 0, TestsSkipped: 0, TotalTests: 1), finalProgress);
    }

    private static string CreateProjectContent(bool useMtp)
        => $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsTestProject>true</IsTestProject>
                <EnableMSTestRunner>{{useMtp.ToString().ToLowerInvariant()}}</EnableMSTestRunner>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
                <PackageReference Include="MSTest.TestAdapter" Version="3.10.4" />
                <PackageReference Include="MSTest.TestFramework" Version="3.10.4" />
              </ItemGroup>
            </Project>
            """;
}
