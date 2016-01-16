// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class CSharpTestWorkspaceFixture : TestWorkspaceFixture
    {
        protected override Task<TestWorkspace> CreateWorkspaceAsync()
        {
            return CSharpWorkspaceFactory.CreateCSharpWorkspaceFromFilesAsync(
                new string[] { string.Empty, },
                new CSharpParseOptions[] { new CSharpParseOptions(kind: SourceCodeKind.Regular), });
        }
    }
}
