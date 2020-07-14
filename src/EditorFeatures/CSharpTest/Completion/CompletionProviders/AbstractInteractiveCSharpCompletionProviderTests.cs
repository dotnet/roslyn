// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public abstract class AbstractInteractiveCSharpCompletionProviderTests : AbstractCSharpCompletionProviderTests<InteractiveCSharpTestWorkspaceFixture>
    {
        protected AbstractInteractiveCSharpCompletionProviderTests(InteractiveCSharpTestWorkspaceFixture workspaceFixture)
            : base(workspaceFixture)
        {
        }

        protected override TestWorkspace CreateWorkspace(string fileContents)
            => InteractiveCSharpTestWorkspaceFixture.CreateInteractiveWorkspace(fileContents, exportProvider: ExportProvider);
    }
}
