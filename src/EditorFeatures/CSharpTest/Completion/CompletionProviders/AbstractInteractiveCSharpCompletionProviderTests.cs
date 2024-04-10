// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public abstract class AbstractInteractiveCSharpCompletionProviderTests : AbstractCSharpCompletionProviderTests<InteractiveCSharpTestWorkspaceFixture>
    {
        protected override EditorTestWorkspace CreateWorkspace(string fileContents)
            => InteractiveCSharpTestWorkspaceFixture.CreateInteractiveWorkspace(fileContents, composition: GetComposition());
    }
}
