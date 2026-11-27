// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public sealed class LspWorkspaceRegistrationServiceTests : AbstractLanguageServerProtocolTests
{
    public LspWorkspaceRegistrationServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestDisposedWorkspaceDeregistered(bool mutatingLspWorkspace)
    {
        LspWorkspaceRegistrationEventListener listener;
        await using (var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace))
        {
            listener = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LspWorkspaceRegistrationEventListener>();

            // Verify both the singleton listener and the per-server LspWorkspaceRegistrationService see the workspace.
            Assert.Contains(testLspServer.TestWorkspace, listener.GetRegisteredWorkspaces());

            var perServerRegistrationService = testLspServer.GetRequiredLspService<LspWorkspaceRegistrationService>();
            Assert.Contains(testLspServer.TestWorkspace, perServerRegistrationService.GetAllRegistrations());
        }

        // After the workspace is disposed, the listener's StopListening should have fired.
        Assert.Empty(listener.GetRegisteredWorkspaces());
    }
}
