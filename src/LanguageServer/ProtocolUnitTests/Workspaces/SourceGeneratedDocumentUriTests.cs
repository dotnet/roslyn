// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public class SourceGeneratedDocumentUrisTests : AbstractLanguageServerProtocolTests
{
    public SourceGeneratedDocumentUrisTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task UrisRoundTrip()
    {
        await using var testLspServer = await CreateTestLspServerAsync("", false);

        // Create up an identity to test with; we'll use the real project ID since we implicitly look for that when deserializing to get the
        // project's debug name, but everything else we'll generate here to ensure we don't assume it exists during deserialization.
        const string HintName = "HintName.cs";

        var generatedDocumentId = DocumentId.CreateFromSerialized(testLspServer.TestWorkspace.Projects.Single().Id, Guid.NewGuid(), isSourceGenerated: true, debugName: HintName);

        var identity = new SourceGeneratedDocumentIdentity(generatedDocumentId, HintName,
            new SourceGeneratorIdentity("GeneratorAssembly", "Generator.dll", new Version(1, 0), "GeneratorType"), HintName);

        var uri = SourceGeneratedDocumentUri.Create(identity);
        Assert.Equal(SourceGeneratedDocumentUri.Scheme, uri.Scheme);
        var deserialized = SourceGeneratedDocumentUri.DeserializeIdentity(testLspServer.TestWorkspace.CurrentSolution, uri);

        AssertEx.NotNull(deserialized);
        Assert.Equal(identity, deserialized.Value);

        // Debug name is not considered as a the usual part of equality, but we want to ensure we pass this through too
        Assert.Equal(generatedDocumentId.DebugName, deserialized.Value.DocumentId.DebugName);
    }
}