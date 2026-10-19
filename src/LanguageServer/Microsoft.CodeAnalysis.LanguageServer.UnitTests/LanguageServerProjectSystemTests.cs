// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.NET.ProjectData;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectSystemTests : TestBase
{
    [Fact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/9885")]
    public void CachedDocumentUsesProjectRelativeFolders()
    {
        var documentPath = Path.Combine(TempRoot.Root, "B", "C", "File.cs");
        var item = new ProjectDataItem(documentPath, KeyValueCollection.Empty);

        var documentFileInfo = LanguageServerProjectSystem.CreateDocumentFileInfo(item, TempRoot.Root);

        Assert.Equal(["B", "C"], documentFileInfo.Folders);
    }
}
