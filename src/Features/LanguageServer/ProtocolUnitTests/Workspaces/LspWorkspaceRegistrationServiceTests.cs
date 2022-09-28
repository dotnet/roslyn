﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;
public class LspWorkspaceRegistrationServiceTests : AbstractLanguageServerProtocolTests
{
    [Fact]
    public async Task TestDisposedWorkspaceDeregistered()
    {
        var markup = "";
        TestWorkspaceRegistrationService registrationService;
        await using (var testLspServer = await CreateTestLspServerAsync(markup))
        {
            registrationService = (TestWorkspaceRegistrationService)testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LspWorkspaceRegistrationService>();
        }

        Assert.Empty(registrationService.GetAllRegistrations());
    }
}
