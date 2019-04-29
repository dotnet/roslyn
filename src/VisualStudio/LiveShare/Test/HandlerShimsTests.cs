// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class LiveShareRequestHandlerShimsTests : AbstractLiveShareRequestHandlerTests
    {
        // For now we're just testing that the right liveshare handlers are exported.
        // This ensures that for shims the right handler is found from roslyn handlers.
        // Functionality will be tested in the code analysis language server layer.
        [Fact]
        public void TestLiveShareRequestHandlersExported()
        {
            var (solution, _) = CreateTestSolution(string.Empty);

            var workspace = (TestWorkspace)solution.Workspace;
            var handlers = workspace.ExportProvider.GetExportedValues<ILspRequestHandler>(LiveShareConstants.RoslynContractName).ToArray();

            // Verify there are exactly the number of liveshare request handlers as expected.
            // Liveshare shims will verify there is a matching roslyn request handler when they are created.
            Assert.Equal(21, handlers.Length);
        }
    }
}
