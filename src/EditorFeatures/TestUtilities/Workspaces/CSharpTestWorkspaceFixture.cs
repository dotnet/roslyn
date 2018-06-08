// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class CSharpTestWorkspaceFixture : TestWorkspaceFixture
    {
        protected override TestWorkspace CreateWorkspace()
        {
            return TestWorkspace.CreateCSharp2(
                new string[] { string.Empty, },
                new CSharpParseOptions[] { new CSharpParseOptions(kind: SourceCodeKind.Regular), });
        }
    }
}
