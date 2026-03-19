// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

public sealed class InteractiveCSharpTestWorkspaceFixture : CSharpTestWorkspaceFixture
{
    internal static EditorTestWorkspace CreateInteractiveWorkspace(string fileContent, TestComposition composition)
    {
        return EditorTestWorkspace.Create(XElement.Parse($"""
            <Workspace>
                <Submission Language="C#" CommonReferences="true">
            <![CDATA[
                        {fileContent}]]>
                </Submission>
            </Workspace>
            """), composition: composition, workspaceKind: WorkspaceKind.Interactive);
    }

    protected override EditorTestWorkspace CreateWorkspace(TestComposition composition = null)
        => CreateInteractiveWorkspace(fileContent: "", composition);
}
