// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

public class CSharpTestWorkspaceFixture : TestWorkspaceFixture
{
    protected override EditorTestWorkspace CreateWorkspace(TestComposition composition = null)
    {
        return EditorTestWorkspace.CreateWithSingleEmptySourceFile(
            LanguageNames.CSharp,
            compilationOptions: null,
            parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Regular),
            composition: composition);
    }
}
