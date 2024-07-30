// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References;
public sealed class FindAllReferencesHandlerFeaturesTests(ITestOutputHelper? testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => LspTestCompositions.LanguageServerProtocol
        .AddParts(typeof(TestDocumentTrackingService))
        .AddParts(typeof(TestWorkspaceRegistrationService));

    [Theory, CombinatorialData]
    public async Task TestFindAllReferencesAsync_DoesNotUseVSTypes(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                public int {|reference:someInt|} = 1;
                void M()
                {
                    var i = {|reference:someInt|} + 1;
                }
            }
            class B
            {
                int someInt = A.{|reference:someInt|} + 1;
                void M2()
                {
                    var j = someInt + A.{|caret:|}{|reference:someInt|};
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.ClientCapabilities());

        var results = await FindAllReferencesHandlerTests.RunFindAllReferencesNonVSAsync(testLspServer, testLspServer.GetLocations("caret").First());
        AssertLocationsEqual(testLspServer.GetLocations("reference"), results.Select(result => result));
    }

    [Theory, CombinatorialData]
    public async Task TestFindAllReferencesAsync_LargeNumberOfReferences(bool mutatingLspWorkspace)
    {
        var markup =
            """
            using System.Threading.Tasks
            class A
            {
                private {|caret:Task|} someTask = Task.CompletedTask;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.ClientCapabilities());

        for (var i = 0; i < 100; i++)
        {
            var source = $$"""
            using System.Threading.Tasks
            class SomeClass{{i}}
            {
                private Task someTask;
            }
            """;

            var testDocument = new EditorTestHostDocument(text: source, displayName: @$"C:\SomeFile{i}.cs", exportProvider: testLspServer.TestWorkspace.ExportProvider, filePath: @$"C:\SomeFile{i}.cs");
            testLspServer.TestWorkspace.AddTestProject(new EditorTestHostProject(testLspServer.TestWorkspace, documents: new[] { testDocument }));
        }

        await WaitForWorkspaceOperationsAsync(testLspServer.TestWorkspace);

        var results = await FindAllReferencesHandlerTests.RunFindAllReferencesNonVSAsync(testLspServer, testLspServer.GetLocations("caret").First());
        Assert.Equal(103, results.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestFindAllReferencesAsync_LinkedFile(bool mutatingLspWorkspace, [CombinatorialRange(0, 10)] int iteration)
    {
        _ = iteration;
        var markup =
            """
            using System.Threading.Tasks
            class A
            {
                private void SomeMethod()
                {
                    Do({|caret:Task|}.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                    Do(Task.CompletedTask);
                }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
                    <Document FilePath="C:\C.cs">{markup}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document IsLinkFile="true" LinkFilePath="C:\C.cs" LinkAssemblyName="CSProj1"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace, initializationOptions: new InitializationOptions
        {
            ClientCapabilities = new LSP.ClientCapabilities()
        });

        await WaitForWorkspaceOperationsAsync(testLspServer.TestWorkspace);

        var results = await FindAllReferencesHandlerTests.RunFindAllReferencesNonVSAsync(testLspServer, testLspServer.GetLocations("caret").First());
        Assert.Equal(46, results.Length);
    }
}
