// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class UriTests : AbstractLanguageServerProtocolTests
{
    public UriTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(CustomResolveHandler));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/runtime/issues/89538")]
    public async Task TestMiscDocument_WithFileScheme(bool mutatingLspWorkspace)
    {
        var filePath = "C:\\\ud86d\udeac\ue25b.txt";

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file with a file URI.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """, languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (_, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri }, CancellationToken.None);
        Assert.NotNull(document);
        Assert.True(await testLspServer.GetManager().GetTestAccessor().IsMiscellaneousFilesDocumentAsync(document));
        Assert.Equal(looseFileUri, document.GetURI());
        Assert.Equal(filePath, document.FilePath);
    }

    [Theory, CombinatorialData]
    public async Task TestMiscDocument_WithOtherScheme(bool mutatingLspWorkspace)
    {

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file that hasn't been saved with a name.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"untitled:untitledFile");
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """, languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (_, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri }, CancellationToken.None);
        Assert.NotNull(document);
        Assert.True(await testLspServer.GetManager().GetTestAccessor().IsMiscellaneousFilesDocumentAsync(document));
        Assert.Equal(looseFileUri, document.GetURI());
        Assert.Equal(looseFileUri.UriString, document.FilePath);
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
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        var workspaceDocument = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var expectedDocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentFilePath);

        await testLspServer.OpenDocumentAsync(expectedDocumentUri).ConfigureAwait(false);

        // Verify file is not added to the misc file workspace.
        {
            var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = expectedDocumentUri }, CancellationToken.None);
            Assert.NotNull(document);
            Assert.False(await testLspServer.GetManager().GetTestAccessor().IsMiscellaneousFilesDocumentAsync(document));
            Assert.Equal(expectedDocumentUri, document.GetURI());
            Assert.Equal(documentFilePath, document.FilePath);
        }

        // Try again, this time with a uri with different case sensitivity.  This is supported, and is needed by Xaml.
        {
            var lowercaseUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentFilePath.ToLowerInvariant());
            Assert.NotEqual(expectedDocumentUri.GetRequiredParsedUri().AbsolutePath, lowercaseUri.GetRequiredParsedUri().AbsolutePath);
            var (_, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = lowercaseUri }, CancellationToken.None);
            Assert.NotNull(document);
            Assert.False(await testLspServer.GetManager().GetTestAccessor().IsMiscellaneousFilesDocumentAsync(document));
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

        var gitDocumentUri = new DocumentUri(fileDocumentUri.ToString().Replace("file", "git"));

        var gitDocumentText = "GitText";
        await testLspServer.OpenDocumentAsync(gitDocumentUri, gitDocumentText);

        // Verify file is added to the workspace and the text matches the file document
        var (workspace, _, fileDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileDocumentUri }, CancellationToken.None);
        AssertEx.NotNull(fileDocument);
        var fileTextResult = await fileDocument.GetTextAsync();
        Assert.Equal(fileDocumentUri, fileDocument.GetURI());
        Assert.Equal(fileDocumentText, fileTextResult.ToString());

        // Verify file is added to the workspace and the text matches the git document
        var (gitWorkspace, _, gitDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = gitDocumentUri }, CancellationToken.None);
        AssertEx.NotNull(gitDocument);
        var gitText = await gitDocument.GetTextAsync();
        Assert.Equal(gitDocumentUri, gitDocument.GetURI());
        Assert.Equal(gitDocumentText, gitText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task TestFindsExistingDocumentWhenUriHasDifferentEncodingAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Execute the request as JSON directly to avoid the test client serializing System.Uri using the encoded Uri to send to the server.
        var requestJson = """
            {
                "textDocument": {
                    "uri": "git:/c:/Users/dabarbet/source/repos/ConsoleApp10/ConsoleApp10/Program.cs?{{\"path\":\"c:\\\\Users\\\\dabarbet\\\\source\\\\repos\\\\ConsoleApp10\\\\ConsoleApp10\\\\Program.cs\",\"ref\":\"~\"}}",
                    "languageId": "csharp",
                    "text": "LSP text"
                }
            }
            """;
        var jsonDocument = JsonDocument.Parse(requestJson);
        await testLspServer.ExecutePreSerializedRequestAsync(LSP.Methods.TextDocumentDidOpenName, jsonDocument);

        // Retrieve the URI from the json - this is the unencoded (and not JSON escaped) version of the URI.
        var unencodedUri = JsonSerializer.Deserialize<LSP.DidOpenTextDocumentParams>(jsonDocument, JsonSerializerOptions)!.TextDocument.DocumentUri;

        // Access the document using the unencoded URI to make sure we find it in the C# misc files.
        var (workspace, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = unencodedUri }, CancellationToken.None).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);
        var originalText = await lspDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", originalText.ToString());

        // Now make a request using the encoded document to ensure the server is able to find the document in misc C# files.
        var encodedUriString = @"git:/c:/Users/dabarbet/source/repos/ConsoleApp10/ConsoleApp10/Program.cs?%7B%7B%22path%22:%22c:%5C%5CUsers%5C%5Cdabarbet%5C%5Csource%5C%5Crepos%5C%5CConsoleApp10%5C%5CConsoleApp10%5C%5CProgram.cs%22,%22ref%22:%22~%22%7D%7D";
        var encodedUri = new DocumentUri(encodedUriString);
        var info = await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { DocumentUri = encodedUri }), CancellationToken.None);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);

        var (encodedWorkspace, _, encodedDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = encodedUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Same(workspace, encodedWorkspace);
        AssertEx.NotNull(encodedDocument);
        Assert.Equal(LanguageNames.CSharp, encodedDocument.Project.Language);
        var encodedText = await encodedDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", encodedText.ToString());

        // The text we get back should be the exact same instance that was originally saved by the unencoded request.
        Assert.Same(originalText, encodedText);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2208409")]
    public async Task TestFindsExistingDocumentWhenUriHasDifferentCasingForCaseInsensitiveUriAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        var upperCaseUri = new DocumentUri(@"file:///C:/Users/dabarbet/source/repos/XUnitApp1/UnitTest1.cs");
        var lowerCaseUri = new DocumentUri(@"file:///c:/Users/dabarbet/source/repos/XUnitApp1/UnitTest1.cs");

        // Execute the request as JSON directly to avoid the test client serializing System.Uri.
        var requestJson = $$$"""
            {
                "textDocument": {
                    "uri": "{{{upperCaseUri.UriString}}}",
                    "languageId": "csharp",
                    "text": "LSP text"
                }
            }
            """;
        var jsonDocument = JsonDocument.Parse(requestJson);
        await testLspServer.ExecutePreSerializedRequestAsync(LSP.Methods.TextDocumentDidOpenName, jsonDocument);

        // Access the document using the upper case to make sure we find it in the C# misc files.
        var (workspace, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = upperCaseUri }, CancellationToken.None).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);
        var originalText = await lspDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", originalText.ToString());

        // Now make a request using different case.
        var info = await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { DocumentUri = lowerCaseUri }), CancellationToken.None);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);

        var (lowerCaseWorkspace, _, lowerCaseDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = lowerCaseUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Same(workspace, lowerCaseWorkspace);
        AssertEx.NotNull(lowerCaseDocument);
        Assert.Equal(LanguageNames.CSharp, lowerCaseDocument.Project.Language);
        var lowerCaseText = await lowerCaseDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", lowerCaseText.ToString());

        // The text we get back should be the exact same instance that was originally saved by the unencoded request.
        Assert.Same(originalText, lowerCaseText);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2208409")]
    public async Task TestUsesDifferentDocumentForDifferentCaseWithNonUncUriAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        var upperCaseUri = new DocumentUri(@"git:/Blah");
        var lowerCaseUri = new DocumentUri(@"git:/blah");

        // Execute the request as JSON directly to avoid the test client serializing System.Uri.
        var requestJson = $$$"""
            {
                "textDocument": {
                    "uri": "{{{upperCaseUri.UriString}}}",
                    "languageId": "csharp",
                    "text": "LSP text"
                }
            }
            """;
        var jsonDocument = JsonDocument.Parse(requestJson);
        await testLspServer.ExecutePreSerializedRequestAsync(LSP.Methods.TextDocumentDidOpenName, jsonDocument);

        // Access the document using the upper case to make sure we find it in the C# misc files.
        var (workspace, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = upperCaseUri }, CancellationToken.None).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);
        var originalText = await lspDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", originalText.ToString());

        // Now make a request using different case.  This should throw since we have not opened a document with the URI with different case (and not UNC).
        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { DocumentUri = lowerCaseUri }), CancellationToken.None));
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotCrashIfUnableToDetermineLanguageInfo(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file that hasn't been saved with a name.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"untitled:untitledFile");
        await testLspServer.OpenDocumentAsync(looseFileUri, "hello", languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri }, CancellationToken.None);
        Assert.NotNull(document);
        Assert.True(await testLspServer.GetManager().GetTestAccessor().IsMiscellaneousFilesDocumentAsync(document));
        Assert.Equal(looseFileUri, document.GetURI());
        Assert.Equal(looseFileUri.UriString, document.FilePath);

        // Close the document (deleting the saved language information)
        await testLspServer.CloseDocumentAsync(looseFileUri);

        // Assert that the request throws but the server does not crash.
        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri }), CancellationToken.None));
        Assert.False(testLspServer.GetServerAccessor().HasShutdownStarted());
        Assert.False(testLspServer.GetQueueAccessor()!.Value.IsComplete());
    }

    [Theory]
    // Invalid URIs
    [InlineData(true, "file://invalid^uri")]
    [InlineData(false, "file://invalid^uri")]
    [InlineData(true, "perforce://%239/some/file/here/source.cs")]
    [InlineData(false, "perforce://%239/some/file/here/source.cs")]
    // Valid URI, but System.Uri cannot parse it.
    [InlineData(true, "vscode-notebook-cell://dev-container+7b2/workspaces/devkit-crash/notebook.ipynb")]
    [InlineData(false, "vscode-notebook-cell://dev-container+7b2/workspaces/devkit-crash/notebook.ipynb")]
    // Valid URI, but System.Uri cannot parse it.
    [InlineData(true, "perforce://@=1454483/some/file/here/source.cs")]
    [InlineData(false, "perforce://@=1454483/some/file/here/source.cs")]
    public async Task TestOpenDocumentWithInvalidUri(bool mutatingLspWorkspace, string uriString)
    {
        // Create a server that supports LSP misc files
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open file with a URI System.Uri cannot parse.  This should not crash the server.
        var invalidUri = new DocumentUri(uriString);
        // ParsedUri should be null as System.Uri cannot parse it.
        Assert.Null(invalidUri.ParsedUri);
        await testLspServer.OpenDocumentAsync(invalidUri, string.Empty, languageId: "csharp").ConfigureAwait(false);

        // Verify requests succeed and that the file is in misc.
        var info = await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { DocumentUri = invalidUri }), CancellationToken.None);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, info!.WorkspaceKind);
        Assert.Equal(LanguageNames.CSharp, info.ProjectLanguage);

        // Verify we can modify the document in misc.
        await testLspServer.InsertTextAsync(invalidUri, (0, 0, "hello"));
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = invalidUri }, CancellationToken.None);
        Assert.Equal("hello", (await document!.GetTextAsync()).ToString());
    }

    [Theory]
    [InlineData(true, null, null)]
    [InlineData(false, "file://c:\\valid", null)]
    [InlineData(false, null, "file://c:\\valid")]
    [InlineData(true, "file://c:\\valid", "file://c:\\valid")]
    [InlineData(true, "file://c:\\valid", "file:///c:/valid")]
    [InlineData(true, "file://c:\\valid", "file://c:\\VALID")]
    [InlineData(false, "file://c:\\valid", "file://c:\\valid2")]
    public void TestUriEquality(bool areEqual, string? uriString1, string? uriString2)
    {
        var documentUri1 = uriString1 != null ? new DocumentUri(uriString1) : null;
        var documentUri2 = uriString2 != null ? new DocumentUri(uriString2) : null;

        Assert.True(areEqual == (documentUri1 == documentUri2));
        Assert.True(areEqual != (documentUri1 != documentUri2));
    }

    private sealed record class ResolvedDocumentInfo(string WorkspaceKind, string ProjectLanguage);
    private sealed record class CustomResolveParams([property: JsonPropertyName("textDocument")] LSP.TextDocumentIdentifier TextDocument);

    [ExportCSharpVisualBasicStatelessLspService(typeof(CustomResolveHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class CustomResolveHandler() : ILspServiceDocumentRequestHandler<CustomResolveParams, ResolvedDocumentInfo>
    {
        public const string MethodName = nameof(CustomResolveHandler);

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;
        public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(CustomResolveParams request) => request.TextDocument;
        public Task<ResolvedDocumentInfo> HandleRequestAsync(CustomResolveParams request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResolvedDocumentInfo(context.Workspace!.Kind!, context.GetRequiredDocument().Project.Language));
        }
    }
}
