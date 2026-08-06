// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed partial class HtmlDocumentSynchronizer
{
    internal readonly struct RazorDocumentVersion(int workspaceVersion, Checksum checksum)
    {
        internal int WorkspaceVersion => workspaceVersion;
        internal Checksum Checksum => checksum;

        public override string ToString()
            => $"Checksum {checksum} from workspace version {workspaceVersion}";

        internal static async Task<RazorDocumentVersion> CreateAsync(TextDocument razorDocument, CancellationToken cancellationToken)
        {
            var workspaceVersion = razorDocument.Project.Solution.SolutionStateContentVersion;

            var checksum = await razorDocument.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

            return new RazorDocumentVersion(workspaceVersion, checksum);
        }
    }
}
