// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
