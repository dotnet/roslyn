// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
