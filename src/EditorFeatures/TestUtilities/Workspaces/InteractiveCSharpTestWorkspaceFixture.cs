// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using static Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo.AbstractNavigateToTests;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class InteractiveCSharpTestWorkspaceFixture : CSharpTestWorkspaceFixture
    {
        internal static TestWorkspace CreateInteractiveWorkspace(string fileContent, TestComposition composition)
        {
            var workspaceDefinition = $@"
<Workspace>
    <Submission Language=""C#"" CommonReferences=""true"">
<![CDATA[
            {fileContent}]]>
    </Submission>
</Workspace>
";
            return TestWorkspace.Create(XElement.Parse(workspaceDefinition), composition: composition, workspaceKind: WorkspaceKind.Interactive);
        }

        protected override TestWorkspace CreateWorkspace(TestComposition composition = null)
            => CreateInteractiveWorkspace(fileContent: "", composition);
    }
}
