// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
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
