// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Rename;

public sealed class WillRenameTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => base.Composition
        .AddParts(typeof(TestWillRenameListener1))
        .AddParts(typeof(TestWillRenameListener2));

    [Theory, CombinatorialData]
    public async Task SetsCapabilities(bool mutatingLspWorkspace)
    {
        var clientCapabilities = new ClientCapabilities
        {
            Workspace = new WorkspaceClientCapabilities
            {
                FileOperations = new FileOperationsWorkspaceClientCapabilities
                {
                    WillRename = true
                },
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace, clientCapabilities);

        var capabilities = testLspServer.GetServerCapabilities();

        Assert.Collection(capabilities.Workspace.FileOperations.WillRename.Filters,
            filter => Assert.Equal("*.cs", filter.Pattern.Glob),
            filter => Assert.Equal("*.vb", filter.Pattern.Glob));
    }

    [Theory, CombinatorialData]
    public async Task CallsListener(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var listeners = ((IMefHostExportProvider)testLspServer.TestWorkspace.Services.HostServices).GetExportedValues<ILspWillRenameListener>().OfType<TestWillRenameListener1>().ToArray();

        // Need at least one change, or the result will be dropped
        listeners[0].Result = new WorkspaceEdit() { DocumentChanges = new TextDocumentEdit[] { new() { } } };

        var edit = await RunWillRenameAsync(testLspServer);
        Assert.NotNull(edit);
    }

    [Theory, CombinatorialData]
    public async Task CombinesDocumentChanges(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var listeners = ((IMefHostExportProvider)testLspServer.TestWorkspace.Services.HostServices).GetExportedValues<ILspWillRenameListener>().OfType<TestWillRenameListener1>().ToArray();

        var expected = new WorkspaceEdit()
        {
            DocumentChanges = new TextDocumentEdit[] {
                new() { TextDocument = new() { DocumentUri = new("file://file1.cs") } },
                new() { TextDocument = new() { DocumentUri = new("file://file2.cs") } }
            }
        };

        listeners[0].Result = new WorkspaceEdit() { DocumentChanges = new TextDocumentEdit[] { new() { TextDocument = new() { DocumentUri = new("file://file1.cs") } } } };
        listeners[1].Result = new WorkspaceEdit() { DocumentChanges = new TextDocumentEdit[] { new() { TextDocument = new() { DocumentUri = new("file://file2.cs") } } } };

        var edit = await RunWillRenameAsync(testLspServer);
        Assert.NotNull(edit);

        AssertJsonEquals(expected, edit);
    }

    [Theory, CombinatorialData]
    public async Task CombinesDocumentChanges_SumType(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var listeners = ((IMefHostExportProvider)testLspServer.TestWorkspace.Services.HostServices).GetExportedValues<ILspWillRenameListener>().OfType<TestWillRenameListener1>().ToArray();

        var expected = new WorkspaceEdit()
        {
            DocumentChanges = new SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] {
                new TextDocumentEdit() { TextDocument = new() { DocumentUri = new("file://file1.cs") } },
                new RenameFile() { OldDocumentUri = new("file://file2.cs") }
            }
        };

        listeners[0].Result = new WorkspaceEdit() { DocumentChanges = new TextDocumentEdit[] { new() { TextDocument = new() { DocumentUri = new("file://file1.cs") } } } };
        listeners[1].Result = new WorkspaceEdit() { DocumentChanges = new SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] { new RenameFile() { OldDocumentUri = new("file://file2.cs") } } };

        var edit = await RunWillRenameAsync(testLspServer);
        Assert.NotNull(edit);

        AssertJsonEquals(expected, edit);
    }

    [Theory, CombinatorialData]
    public async Task CombinesDocumentChanges_Changes(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var listeners = ((IMefHostExportProvider)testLspServer.TestWorkspace.Services.HostServices).GetExportedValues<ILspWillRenameListener>().OfType<TestWillRenameListener1>().ToArray();

        var expected = new WorkspaceEdit()
        {
            Changes = new Dictionary<string, TextEdit[]>
            {
                { "file://file1.cs", []},
                { "file://file2.cs", [] }
            }
        };

        listeners[0].Result = new WorkspaceEdit() { Changes = new Dictionary<string, TextEdit[]> { { "file://file1.cs", [] } } };
        listeners[1].Result = new WorkspaceEdit() { Changes = new Dictionary<string, TextEdit[]> { { "file://file2.cs", [] } } };

        var edit = await RunWillRenameAsync(testLspServer);
        Assert.NotNull(edit);

        AssertJsonEquals(expected, edit);
    }

    private static async Task<WorkspaceEdit> RunWillRenameAsync(TestLspServer testLspServer)
    {
        var renameParams = new RenameFilesParams();
        return await testLspServer.ExecuteRequestAsync<LSP.RenameFilesParams, LSP.WorkspaceEdit>(LSP.Methods.WorkspaceWillRenameFilesName, renameParams, CancellationToken.None);
    }

    [ExportLspWillRenameListener("*.cs")]
    [Shared]
    [PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private class TestWillRenameListener1() : ILspWillRenameListener
    {
        public WorkspaceEdit Result { get; set; }

        public async Task<WorkspaceEdit> HandleWillRenameAsync(RenameFilesParams renameParams, RequestContext context, CancellationToken cancellationToken)
        {
            return Result;
        }
    }

    [ExportLspWillRenameListener("*.vb")]
    [Shared]
    [PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private class TestWillRenameListener2() : TestWillRenameListener1
    {
    }
}
