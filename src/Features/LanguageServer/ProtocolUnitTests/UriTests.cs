// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;
public class UriTests : AbstractLanguageServerProtocolTests
{
    public UriTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestMiscDocument_WithFileScheme(bool mutatingLspWorkspace)
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

        // Open an empty loose file with a file URI.
        var filePath = @"C:\Users\user\someFile.txt";
        var looseFileUri = new Uri(filePath, UriKind.Absolute);
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
        var looseFileUri = new Uri(@"untitled:untitledFile", UriKind.Absolute);
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
@$"<Workspace>
    <Project Language=""C#"" Name=""CSProj1"" CommonReferences=""true"" FilePath=""C:\CSProj1.csproj"">
        <Document FilePath=""{documentFilePath}"">
            public class A
            {{
            }}
        </Document>
    </Project>
</Workspace>";
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var workspaceDocument = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var expectedDocumentUri = new Uri(documentFilePath, UriKind.Absolute);

        await testLspServer.OpenDocumentAsync(expectedDocumentUri).ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = expectedDocumentUri }, CancellationToken.None);
        Assert.False(workspace is LspMiscellaneousFilesWorkspace);
        AssertEx.NotNull(document);
        Assert.Equal(expectedDocumentUri, document.GetURI());
        Assert.Equal(documentFilePath, document.FilePath);
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
        var gitDocumentUri = new Uri(fileDocumentUri.ToString().Replace("file", "git"));
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
