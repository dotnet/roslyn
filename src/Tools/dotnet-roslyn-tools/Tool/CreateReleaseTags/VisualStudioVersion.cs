// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.CreateReleaseTags;

public readonly struct VisualStudioVersion(string mainVersion, string? previewVersion, string commitSha, DateTime creationTime, string buildId)
{
    public readonly string MainVersion = mainVersion;
    public readonly string? PreviewVersion = previewVersion;
    public readonly string CommitSha = commitSha;
    public readonly DateTime CreationTime = creationTime;
    public readonly string BuildId = buildId;
}
