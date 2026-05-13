// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer.Handler.Logging;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class WorkspaceStructureLogTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task LogReturnsUriToExistingXmlFile()
    {
        await using var server = await CreateLanguageServerAsync();

        var response = await server.ExecuteRequestAsync<WorkspaceStructureLogParams, WorkspaceStructureLogResponse>(
            WorkspaceStructureLogHandler.MethodName,
            new WorkspaceStructureLogParams(),
            CancellationToken.None);

        Assert.NotNull(response);

        string? filePath = null;
        try
        {
            filePath = response!.Uri.GetDocumentFilePathFromUri();
            Assert.True(File.Exists(filePath), $"Expected log file to exist at {filePath}");

            var doc = XDocument.Load(filePath);
            Assert.Equal("workspace", doc.Root!.Name.LocalName);
        }
        finally
        {
            if (filePath is not null && File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
