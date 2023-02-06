// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class CSharpTestWorkspaceFixture : TestWorkspaceFixture
    {
        protected override TestWorkspace CreateWorkspace(TestComposition composition = null)
        {
            return TestWorkspace.CreateWithSingleEmptySourceFile(
                LanguageNames.CSharp,
                compilationOptions: null,
                parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Regular),
                composition: composition);
        }
    }
}
