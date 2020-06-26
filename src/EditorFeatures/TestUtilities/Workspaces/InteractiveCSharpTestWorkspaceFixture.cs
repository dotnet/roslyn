// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class InteractiveCSharpTestWorkspaceFixture : CSharpTestWorkspaceFixture
    {
        internal static TestWorkspace CreateInteractiveWorkspace(string fileContent, ExportProvider exportProvider)
        {
            var workspaceDefinition = $@"
<Workspace>
    <Submission Language=""C#"" CommonReferences=""true"">
<![CDATA[{fileContent}]]>
    </Submission>
</Workspace>
";
            return TestWorkspace.Create(XElement.Parse(workspaceDefinition), exportProvider: exportProvider, workspaceKind: WorkspaceKind.Interactive);
        }

        protected override TestWorkspace CreateWorkspace(ExportProvider exportProvider = null)
            => CreateInteractiveWorkspace(fileContent: "", exportProvider);
    }
}
