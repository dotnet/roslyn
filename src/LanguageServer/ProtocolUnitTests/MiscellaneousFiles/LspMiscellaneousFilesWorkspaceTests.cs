// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;

/// <summary>
/// This class runs all the tests in <see cref="AbstractLspMiscellaneousFilesWorkspaceTests"/> against the base implementation.
/// </summary>
public sealed class LspMiscellaneousFilesWorkspaceTests : AbstractLspMiscellaneousFilesWorkspaceTests
{
    public LspMiscellaneousFilesWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    private protected override async ValueTask<Document> AddDocumentAsync(TestLspServer testLspServer, string filePath, string content)
    {
        var projectId = testLspServer.TestWorkspace.CurrentSolution.ProjectIds.Single();
        var documentId = DocumentId.CreateNewId(projectId, filePath);
        await testLspServer.TestWorkspace.AddDocumentAsync(
            DocumentInfo.Create(
                documentId,
                name: filePath,
                filePath: filePath,
                loader: new TestTextLoader(content)));

        return testLspServer.TestWorkspace.CurrentSolution.GetRequiredDocument(documentId);
    }

    private protected override Workspace GetHostWorkspace(TestLspServer testLspServer)
    {
        return testLspServer.TestWorkspace;
    }
}
