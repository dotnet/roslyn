// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

public sealed class VisualBasicTestWorkspaceFixture : TestWorkspaceFixture
{
    protected override EditorTestWorkspace CreateWorkspace(TestComposition composition = null)
    {
        return EditorTestWorkspace.CreateWithSingleEmptySourceFile(
            LanguageNames.VisualBasic,
            compilationOptions: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new VisualBasicParseOptions(kind: SourceCodeKind.Regular),
            composition: composition);
    }
}
