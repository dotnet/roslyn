// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Definitions
{
    public class GoToDefinitionTests : AbstractLanguageServerProtocolTests
    {
        public GoToDefinitionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestGotoDefinitionAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    string {|definition:aString|} = 'hello';
    void M()
    {
        var len = {|caret:|}aString.Length;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            // Verify that as originally serialized, the URI had a file scheme.
            Assert.True(results.Single().Uri.OriginalString.StartsWith("file"));
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoDefinitionAsync_DifferentDocument(bool mutatingLspWorkspace)
        {
            var markups = new string[]
            {
@"namespace One
{
    class A
    {
        public static int {|definition:aInt|} = 1;
    }
}",
@"namespace One
{
    class B
    {
        int bInt = One.A.{|caret:|}aInt;
    }
}"
            };

            await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoDefinitionAsync_MappedFile(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    string aString = 'hello';
    void M()
    {
        var len = aString.Length;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

            AddMappedDocument(testLspServer.TestWorkspace, markup);

            var position = new LSP.Position { Line = 5, Character = 18 };
            var results = await RunGotoDefinitionAsync(testLspServer, new LSP.Location
            {
                Uri = ProtocolConversions.CreateAbsoluteUri($"C:\\{TestSpanMapper.GeneratedFileName}"),
                Range = new LSP.Range { Start = position, End = position }
            });
            AssertLocationsEqual(ImmutableArray.Create(TestSpanMapper.MappedFileLocation), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoDefinitionAsync_InvalidLocation(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {{|caret:|}
        var len = aString.Length;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            Assert.Empty(results);
        }

        [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1264627")]
        public async Task TestGotoDefinitionAsync_NoResultsOnNamespace(bool mutatingLspWorkspace)
        {
            var markup =
@"namespace {|caret:M|}
{
    class A
    {
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            Assert.Empty(results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoDefinitionCrossLanguage(bool mutatingLspWorkspace)
        {
            var markup =
@"<Workspace>
    <Project Language=""C#"" Name=""Definition"" CommonReferences=""true"" FilePath=""C:\CSProj1.csproj"">
        <Document FilePath=""C:\A.cs"">
            public class {|definition:A|}
            {
            }
        </Document>
    </Project>
    <Project Language=""Visual Basic"" CommonReferences=""true"" FilePath=""C:\CSProj2.csproj"">
        <ProjectReference>Definition</ProjectReference>
        <Document FilePath=""C:\C.cs"">
            Class C
                Dim a As {|caret:A|}
            End Class
        </Document>
    </Project>
</Workspace>";
            await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5740")]
        public async Task TestGotoDefinitionPartialMethods(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using System;

                public partial class C
                {
                    partial void {|caret:|}P();
                }

                public partial class C
                {
                    partial void {|definition:P|}()
                    {
                        Console.WriteLine(");
                    }
                }
                """;
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5740")]
        public async Task TestGotoDefinitionPartialProperties(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using System;

                public partial class C
                {
                    partial int {|caret:|}Prop { get; set; }
                }

                public partial class C
                {
                    partial int {|definition:Prop|} { get => 1; set { } }
                }
                """;
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory]
        [InlineData("ValueTuple<int> valueTuple1;")]
        [InlineData("ValueTuple<int, int> valueTuple2;")]
        [InlineData("ValueTuple<int, int, int> valueTuple3;")]
        [InlineData("ValueTuple<int, int, int, int> valueTuple4;")]
        [InlineData("ValueTuple<int, int, int, int, int> valueTuple5;")]
        [InlineData("ValueTuple<int, int, int, int, int, int> valueTuple6;")]
        [InlineData("ValueTuple<int, int, int, int, int, int, int> valueTuple7;")]
        [InlineData("ValueTuple<int, int, int, int, int, int, int, int> valueTuple8;")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71680")]
        public async Task TestGotoDefinitionWithValueTuple(string statement)
        {
            var markup = $"using System; {{|caret:|}}{statement}";

            await using var testLspServer = await CreateTestLspServerAsync(markup, false);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            Assert.Single(results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoDefinitionAsync_SourceGeneratedDocument(bool mutatingLspWorkspace)
        {
            var source =
                """
                namespace M
                {
                    class A
                    {
                        public {|caret:|}B b;
                    }
                }
                """;
            var generated =
                """
                namespace M
                {
                    class B
                    {
                    }
                }
                """;

            await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
            await AddGeneratorAsync(new SingleFileTestGenerator(generated), testLspServer.TestWorkspace);

            var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            var result = Assert.Single(results);
            Assert.Equal(SourceGeneratedDocumentUri.Scheme, result.Uri.Scheme);
        }

        private static async Task<LSP.Location[]> RunGotoDefinitionAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(caret), CancellationToken.None);
        }
    }
}
