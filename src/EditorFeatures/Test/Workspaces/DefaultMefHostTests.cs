// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    // We put [UseExportProvider] here even though the test ironically doesn't actually use the ExportProvider that's provided;
    // setting this however still ensures the AfterTest portion runs which will clear out the existing catalog, and ensure that
    // no other test accidentally uses the default catalog later if that other test is missing [UseExportProvider].
    [UseExportProvider]
    public class DefaultMefHostTests
    {
        [Fact]
        public void TestDefaultCompositionIncludesFeaturesLayer()
        {
            // For this specific test, we want to test that our default container works, so we'll remove any hooks
            // that were created by other tests.
            MefHostServices.TestAccessor.HookServiceCreation(null);

            var ws = new AdhocWorkspace();

            var csservice = ws.Services.GetLanguageServices(LanguageNames.CSharp).GetService<Microsoft.CodeAnalysis.Completion.CompletionService>();
            Assert.NotNull(csservice);

            var vbservice = ws.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<Microsoft.CodeAnalysis.Completion.CompletionService>();
            Assert.NotNull(vbservice);
        }
    }
}
