// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.CreateReleaseTags;

public struct VisualStudioVersion
{
    public readonly string MainVersion;
    public readonly string? PreviewVersion;
    public readonly string CommitSha;
    public readonly DateTime CreationTime;
    public readonly string BuildId;

    public VisualStudioVersion(string mainVersion, string? previewVersion, string commitSha, DateTime creationTime, string buildId)
    {
        MainVersion = mainVersion;
        PreviewVersion = previewVersion;
        CommitSha = commitSha;
        CreationTime = creationTime;
        BuildId = buildId;
    }
}
