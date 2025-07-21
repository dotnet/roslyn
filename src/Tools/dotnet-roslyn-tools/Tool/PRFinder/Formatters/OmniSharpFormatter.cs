// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder.Formatters;

public class OmniSharpFormatter : DefaultFormatter
{
    public override string FormatPRListItem(string comment, string prNumber, string prUrl)
    {
        // Remove the PR Number from the comment
        comment = comment.Replace($"(#{prNumber})", "");

        return $@"* {comment} (PR: [#{prNumber}]({prUrl}))";
    }
}
