﻿// Licensed to the .NET Foundation under one or more agreements.
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

    protected override TestComposition Composition => base.Composition.AddParts(typeof(CustomResolveHandler));

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
        var unencodedUri = JsonSerializer.Deserialize<LSP.DidOpenTextDocumentParams>(jsonDocument, JsonSerializerOptions)!.TextDocument.Uri;

        // Access the document using the unencoded URI to make sure we find it in the C# misc files.
        var (workspace, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = unencodedUri }, CancellationToken.None).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);
        var originalText = await lspDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", originalText.ToString());

        // Now make a request using the encoded document to ensure the server is able to find the document in misc C# files.
        var encodedUriString = @"git:/c:/Users/dabarbet/source/repos/ConsoleApp10/ConsoleApp10/Program.cs?%7B%7B%22path%22:%22c:%5C%5CUsers%5C%5Cdabarbet%5C%5Csource%5C%5Crepos%5C%5CConsoleApp10%5C%5CConsoleApp10%5C%5CProgram.cs%22,%22ref%22:%22~%22%7D%7D";
#pragma warning disable RS0030 // Do not use banned APIs
        var encodedUri = new Uri(encodedUriString, UriKind.Absolute);
#pragma warning restore RS0030 // Do not use banned APIs
        var info = await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { Uri = encodedUri }), CancellationToken.None);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);

        var (encodedWorkspace, _, encodedDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = encodedUri }, CancellationToken.None).ConfigureAwait(false);
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

#pragma warning disable RS0030 // Do not use banned APIs
        var upperCaseUri = new Uri(@"file:///C:/Users/dabarbet/source/repos/XUnitApp1/UnitTest1.cs", UriKind.Absolute);
        var lowerCaseUri = new Uri(@"file:///c:/Users/dabarbet/source/repos/XUnitApp1/UnitTest1.cs", UriKind.Absolute);
#pragma warning restore RS0030 // Do not use banned APIs

        // Execute the request as JSON directly to avoid the test client serializing System.Uri.
        var requestJson = $$$"""
            {
                "textDocument": {
                    "uri": "{{{upperCaseUri.OriginalString}}}",
                    "languageId": "csharp",
                    "text": "LSP text"
                }
            }
            """;
        var jsonDocument = JsonDocument.Parse(requestJson);
        await testLspServer.ExecutePreSerializedRequestAsync(LSP.Methods.TextDocumentDidOpenName, jsonDocument);

        // Access the document using the upper case to make sure we find it in the C# misc files.
        var (workspace, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = upperCaseUri }, CancellationToken.None).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);
        var originalText = await lspDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", originalText.ToString());

        // Now make a request using different case.
        var info = await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { Uri = lowerCaseUri }), CancellationToken.None);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);

        var (lowerCaseWorkspace, _, lowerCaseDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = lowerCaseUri }, CancellationToken.None).ConfigureAwait(false);
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

#pragma warning disable RS0030 // Do not use banned APIs
        var upperCaseUri = new Uri(@"git:/Blah", UriKind.Absolute);
        var lowerCaseUri = new Uri(@"git:/blah", UriKind.Absolute);
#pragma warning restore RS0030 // Do not use banned APIs

        // Execute the request as JSON directly to avoid the test client serializing System.Uri.
        var requestJson = $$$"""
            {
                "textDocument": {
                    "uri": "{{{upperCaseUri.OriginalString}}}",
                    "languageId": "csharp",
                    "text": "LSP text"
                }
            }
            """;
        var jsonDocument = JsonDocument.Parse(requestJson);
        await testLspServer.ExecutePreSerializedRequestAsync(LSP.Methods.TextDocumentDidOpenName, jsonDocument);

        // Access the document using the upper case to make sure we find it in the C# misc files.
        var (workspace, _, lspDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = upperCaseUri }, CancellationToken.None).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace?.Kind);
        Assert.Equal(LanguageNames.CSharp, lspDocument.Project.Language);
        var originalText = await lspDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("LSP text", originalText.ToString());

        // Now make a request using different case.  This should throw since we have not opened a document with the URI with different case (and not UNC).
        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { Uri = lowerCaseUri }), CancellationToken.None));
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotCrashIfUnableToDetermineLanguageInfo(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file that hasn't been saved with a name.
        var looseFileUri = ProtocolConversions.CreateAbsoluteUri(@"untitled:untitledFile");
        await testLspServer.OpenDocumentAsync(looseFileUri, "hello", languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = looseFileUri }, CancellationToken.None);
        Assert.True(workspace is LspMiscellaneousFilesWorkspace);
        AssertEx.NotNull(document);
        Assert.Equal(looseFileUri, document.GetURI());
        Assert.Equal(looseFileUri.OriginalString, document.FilePath);

        // Close the document (deleting the saved language information)
        await testLspServer.CloseDocumentAsync(looseFileUri);

        // Assert that the request throws but the server does not crash.
        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await testLspServer.ExecuteRequestAsync<CustomResolveParams, ResolvedDocumentInfo>(CustomResolveHandler.MethodName,
                new CustomResolveParams(new LSP.TextDocumentIdentifier { Uri = looseFileUri }), CancellationToken.None));
        Assert.False(testLspServer.GetServerAccessor().HasShutdownStarted());
        Assert.False(testLspServer.GetQueueAccessor()!.Value.IsComplete());
    }

    private record class ResolvedDocumentInfo(string WorkspaceKind, string ProjectLanguage);
    private record class CustomResolveParams([property: JsonPropertyName("textDocument")] LSP.TextDocumentIdentifier TextDocument);

    [ExportCSharpVisualBasicStatelessLspService(typeof(CustomResolveHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private class CustomResolveHandler() : ILspServiceDocumentRequestHandler<CustomResolveParams, ResolvedDocumentInfo>
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
