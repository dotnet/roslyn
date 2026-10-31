// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public sealed class SourceGeneratedDocumentUrisTests : AbstractLanguageServerProtocolTests
{
    public SourceGeneratedDocumentUrisTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void SchemeComparisonIsCaseInsensitive()
        => Assert.True(ProtocolConversions.IsSourceGeneratedScheme(SourceGeneratedDocumentUri.Scheme.ToUpperInvariant()));

    [Theory]
    [InlineData("Generated.cs", null)]
    [InlineData("Generated.cs", @"C:\Generators\Generator.dll")]
    [InlineData("Hint Name.cs", "Generator Path.dll")]
    [InlineData("Hint&Name.cs", "Generator&Path.dll")]
    [InlineData("Hint+Name.cs", "Generator+Path.dll")]
    [InlineData("Hint=Name.cs", "Generator=Path.dll")]
    [InlineData("Hint#Name.cs", "Generator#Path.dll")]
    [InlineData("Hint%26Name.cs", "Generator%2BPath.dll")]
    [InlineData("Hint%Name.cs", "Generator%Path.dll")]
    [InlineData("Hint\u00e9\u4e2d.cs", "Generator\u00e9\u4e2d.dll")]
    public async Task UrisRoundTrip(string hintName, string? assemblyPath)
    {
        await using var testLspServer = await CreateTestLspServerAsync("", false);

        // Create up an identity to test with; we'll use the real project ID since we implicitly look for that when deserializing to get the
        // project's debug name, but everything else we'll generate here to ensure we don't assume it exists during deserialization.
        var generatedDocumentId = DocumentId.CreateFromSerialized(testLspServer.TestWorkspace.Projects.Single().Id, Guid.NewGuid(), isSourceGenerated: true, debugName: hintName);

        var identity = new SourceGeneratedDocumentIdentity(generatedDocumentId, hintName,
            new SourceGeneratorIdentity("Generator" + hintName, assemblyPath, new Version(1, 0), "GeneratorType+Nested" + hintName), hintName);

        var uri = SourceGeneratedDocumentUri.Create(identity);
        var parsedUri = ParsedUri.Parse(uri.UriString);
        Assert.Equal(SourceGeneratedDocumentUri.Scheme, parsedUri.Scheme);
        Assert.Equal("/" + hintName, parsedUri.Path);

        AssertRoundTrip(parsedUri);

        // The new client parses the server URI and serializes it with vscode-uri's default encoding.
        AssertRoundTrip(ParsedUri.Parse(parsedUri.ToString(skipEncoding: false)));

        void AssertRoundTrip(ParsedUri documentUri)
        {
            var deserialized = SourceGeneratedDocumentUri.DeserializeIdentity(testLspServer.TestWorkspace.CurrentSolution, documentUri);
            Assert.NotNull(deserialized);
            Assert.Equal(identity, deserialized.Value);

            // Debug name is not considered as a usual part of equality.
            Assert.Equal(generatedDocumentId.DebugName, deserialized.Value.DocumentId.DebugName);
        }
    }
}
