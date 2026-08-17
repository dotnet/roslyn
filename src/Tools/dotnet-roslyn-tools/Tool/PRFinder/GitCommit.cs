// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder;

public struct GitCommit
{
    public string Author { get; set; }
    public string Committer { get; set; }
    public DateTime CommitDate { get; set; }
    public string Message { get; set; }
    public string CommitId { get; set; }
    public string RemoteUrl { get; set; }

    public readonly string MessageShort => Message.Split('\n')[0];
}
