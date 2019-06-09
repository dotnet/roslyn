// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class CSharpTestWorkspaceFixture : TestWorkspaceFixture
    {
        protected override TestWorkspace CreateWorkspace(ExportProvider exportProvider = null)
        {
            return TestWorkspace.CreateCSharp2(
                new string[] { string.Empty, },
                new CSharpParseOptions[] { new CSharpParseOptions(kind: SourceCodeKind.Regular), },
                exportProvider: exportProvider);
        }
    }
}
