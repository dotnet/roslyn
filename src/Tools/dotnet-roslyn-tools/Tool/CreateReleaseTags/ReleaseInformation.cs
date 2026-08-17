// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.CreateReleaseTags;

public abstract class ReleaseInformation(string mainVersion, string? previewVersion, DateTime creationTime)
{
    public readonly string MainVersion = mainVersion;
    public readonly string? PreviewVersion = previewVersion;
    public readonly DateTime CreationTime = creationTime;
}
