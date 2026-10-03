// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Logging;
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

    [Fact]
    public async Task ConcurrentLogsReturnUniqueUris()
    {
        await using var server = await CreateLanguageServerAsync();

        var requests = Enumerable.Range(0, 2).Select(_ =>
            server.ExecuteRequestAsync<WorkspaceStructureLogParams, WorkspaceStructureLogResponse>(
                WorkspaceStructureLogHandler.MethodName,
                new WorkspaceStructureLogParams(),
                CancellationToken.None));

        var responses = await Task.WhenAll(requests);
        var filePaths = responses.Select(response => response!.Uri.GetDocumentFilePathFromUri()).ToArray();

        try
        {
            Assert.Equal(filePaths.Length, filePaths.Distinct().Count());
            Assert.All(filePaths, filePath => Assert.True(File.Exists(filePath), $"Expected log file to exist at {filePath}"));
        }
        finally
        {
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
    }
}
