// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal abstract class BuildInformation(string commitSha, string buildId)
{
    public readonly string CommitSha = commitSha;
    public readonly string BuildId = buildId;
}
