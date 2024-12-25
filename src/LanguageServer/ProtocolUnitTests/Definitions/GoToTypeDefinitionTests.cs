﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
    public class GoToTypeDefinitionTests : AbstractLanguageServerProtocolTests
    {
        public GoToTypeDefinitionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_WithTypeSymbol(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    {|caret:|}A classA;
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_WithPropertySymbol(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    A class{|caret:|}A {;
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_WithFieldSymbol(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    A class{|caret:|}A;
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_WithLocalSymbol(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    void Method()
    {
        var class{|caret:|}A = new A();
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_WithParameterSymbol(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    void Method(A class{|caret:|}A)
    {
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_DifferentDocument(bool mutatingLspWorkspace)
        {
            var markups = new string[]
            {
@"namespace One
{
    class {|definition:A|}
    {
    }
}",
@"namespace One
{
    class B
    {
        A class{|caret:|}A;
    }
}"
            };

            await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_InvalidLocation(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    A classA;
    {|caret:|}
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            Assert.Empty(results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_MappedFile(bool mutatingLspWorkspace)
        {
            var source =
                """
                namespace M
                {
                    class A
                    {
                        public B b{|caret:|};
                    }
                }
                """;
            var mapped =
                """
                namespace M
                {
                    class B
                    {
                    }
                }
                """;

            await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace);

            AddMappedDocument(testLspServer.TestWorkspace, mapped);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            var result = Assert.Single(results);
            AssertLocationsEqual([TestSpanMapper.MappedFileLocation], results);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_SourceGeneratedDocument(bool mutatingLspWorkspace)
        {
            var source =
                """
                namespace M
                {
                    class A
                    {
                        public B b{|caret:|};
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

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            var result = Assert.Single(results);
            Assert.Equal(SourceGeneratedDocumentUri.Scheme, result.Uri.Scheme);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_MetadataAsSource(bool mutatingLspWorkspace)
        {
            var source =
                """
                using System;
                class A
                {
                    void Rethrow(NotImplementedException exception)
                    {
                        throw {|caret:exception|};
                    }
                }
                """;

            // Create a server with LSP misc file workspace and metadata service.
            await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

            // Get the metadata definition.
            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

            // Open the metadata file and verify it gets added to the metadata workspace.
            await testLspServer.OpenDocumentAsync(results.Single().Uri, text: string.Empty).ConfigureAwait(false);

            Assert.Equal(WorkspaceKind.MetadataAsSource, (await GetWorkspaceForDocument(testLspServer, results.Single().Uri)).Kind);
        }

        [Theory, CombinatorialData]
        public async Task TestGotoTypeDefinitionAsync_CrossLanguage(bool mutatingLspWorkspace)
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
                Dim {|caret:a|} As A
            End Class
        </Document>
    </Project>
</Workspace>";
            await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
        }

        private static async Task<LSP.Location[]> RunGotoTypeDefinitionAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentTypeDefinitionName,
                           CreateTextDocumentPositionParams(caret), CancellationToken.None);
        }

        private static async Task<Workspace> GetWorkspaceForDocument(TestLspServer testLspServer, Uri fileUri)
        {
            var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = fileUri }, CancellationToken.None);
            return lspWorkspace!;
        }
    }
}
