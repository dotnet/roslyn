// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [UseExportProvider]
    public class WorkspaceTests
    {
        [Fact]
        public void TestDefaultCompositionIncludesFeaturesLayer()
        {
            var ws = new AdhocWorkspace();

            var csservice = ws.Services.GetLanguageServices(LanguageNames.CSharp).GetService<Microsoft.CodeAnalysis.Completion.CompletionService>();
            Assert.NotNull(csservice);

            var vbservice = ws.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<Microsoft.CodeAnalysis.Completion.CompletionService>();
            Assert.NotNull(vbservice);
        }
    }
}
