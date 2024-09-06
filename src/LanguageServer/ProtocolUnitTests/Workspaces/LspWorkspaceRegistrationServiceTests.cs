// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;
public class LspWorkspaceRegistrationServiceTests : AbstractLanguageServerProtocolTests
{
    public LspWorkspaceRegistrationServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestDisposedWorkspaceDeregistered(bool mutatingLspWorkspace)
    {
        var markup = "";
        TestWorkspaceRegistrationService registrationService;
        await using (var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace))
        {
            registrationService = (TestWorkspaceRegistrationService)testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LspWorkspaceRegistrationService>();
        }

        Assert.Empty(registrationService.GetAllRegistrations());
    }
}
