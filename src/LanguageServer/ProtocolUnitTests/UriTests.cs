// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;
public class UriTests : AbstractLanguageServerProtocolTests
{
    public UriTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/runtime/issues/89538")]
    public async Task TestMiscDocument_WithFileScheme(bool mutatingLspWorkspace)
    {
        var source =
@"class A
{
    void M()
    {
    }
}";
        var filePath = "C:\\\ud86d\udeac\ue25b.txt";

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file with a file URI.
        var looseFileUri = ProtocolConversions.CreateAbsoluteUri(filePath);
        await testLspServer.OpenDocumentAsync(looseFileUri, source, languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = looseFileUri }, CancellationToken.None);
        Assert.True(workspace is LspMiscellaneousFilesWorkspace);
        AssertEx.NotNull(document);
        Assert.Equal(looseFileUri, document.GetURI());
        Assert.Equal(filePath, document.FilePath);
    }

    [Theory, CombinatorialData]
    public async Task TestMiscDocument_WithOtherScheme(bool mutatingLspWorkspace)
    {
        var source =
@"class A
{
    void M()
    {
    }
}";

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file that hasn't been saved with a name.
        var looseFileUri = ProtocolConversions.CreateAbsoluteUri(@"untitled:untitledFile");
        await testLspServer.OpenDocumentAsync(looseFileUri, source, languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = looseFileUri }, CancellationToken.None);
        Assert.True(workspace is LspMiscellaneousFilesWorkspace);
        AssertEx.NotNull(document);
        Assert.Equal(looseFileUri, document.GetURI());
        Assert.Equal(looseFileUri.OriginalString, document.FilePath);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDocument_WithFileScheme(bool mutatingLspWorkspace)
    {
        var documentFilePath = @"C:\A.cs";
        var markup =
            $$"""
            <Workspace>
                <Project Language="C#" Name="CSProj1" CommonReferences="true" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="{{documentFilePath}}">
                        public class A
                        {
                        }
                    </Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var workspaceDocument = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var expectedDocumentUri = ProtocolConversions.CreateAbsoluteUri(documentFilePath);

        await testLspServer.OpenDocumentAsync(expectedDocumentUri).ConfigureAwait(false);

        // Verify file is not added to the misc file workspace.
        {
            var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = expectedDocumentUri }, CancellationToken.None);
            Assert.False(workspace is LspMiscellaneousFilesWorkspace);
            AssertEx.NotNull(document);
            Assert.Equal(expectedDocumentUri, document.GetURI());
            Assert.Equal(documentFilePath, document.FilePath);
        }

        // Try again, this time with a uri with different case sensitivity.  This is supported, and is needed by Xaml.
        {
            var lowercaseUri = ProtocolConversions.CreateAbsoluteUri(documentFilePath.ToLowerInvariant());
            Assert.NotEqual(expectedDocumentUri.AbsolutePath, lowercaseUri.AbsolutePath);
            var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = lowercaseUri }, CancellationToken.None);
            Assert.False(workspace is LspMiscellaneousFilesWorkspace);
            AssertEx.NotNull(document);
            Assert.Equal(expectedDocumentUri, document.GetURI());
            Assert.Equal(documentFilePath, document.FilePath);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDocument_WithFileAndGitScheme(bool mutatingLspWorkspace)
    {
        // Start with an empty workspace.
        await using var testLspServer = await CreateTestLspServerAsync(
            "Initial Disk Contents", mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        var fileDocumentUri = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetURI();
        var fileDocumentText = "FileText";
        await testLspServer.OpenDocumentAsync(fileDocumentUri, fileDocumentText);

        // Add a git version of this document. Instead of "file://FILEPATH" the uri is "git://FILEPATH"

#pragma warning disable RS0030 // Do not use banned APIs
        var gitDocumentUri = new Uri(fileDocumentUri.ToString().Replace("file", "git"));
#pragma warning restore

        var gitDocumentText = "GitText";
        await testLspServer.OpenDocumentAsync(gitDocumentUri, gitDocumentText);

        // Verify file is added to the workspace and the text matches the file document
        var (workspace, _, fileDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = fileDocumentUri }, CancellationToken.None);
        AssertEx.NotNull(fileDocument);
        var fileTextResult = await fileDocument.GetTextAsync();
        Assert.Equal(fileDocumentUri, fileDocument.GetURI());
        Assert.Equal(fileDocumentText, fileTextResult.ToString());

        // Verify file is added to the workspace and the text matches the git document
        var (gitWorkspace, _, gitDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = gitDocumentUri }, CancellationToken.None);
        AssertEx.NotNull(gitDocument);
        var gitText = await gitDocument.GetTextAsync();
        Assert.Equal(gitDocumentUri, gitDocument.GetURI());
        Assert.Equal(gitDocumentText, gitText.ToString());
    }
}
