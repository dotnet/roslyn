// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder.Formatters;

public class DefaultFormatter : IPRLogFormatter
{
    public virtual string FormatChangesHeader(string start, string startUrl, string end, string endUrl, string? path)
        => path is null
            ? $"### Changes from [{start}]({startUrl}) to [{end}]({endUrl}):"
            : $"### Changes from [{start}]({startUrl}) to [{end}]({endUrl}) under `{path}`:";

    public virtual string FormatCommitListItem(string comment, string shortSHA, string commitUrl)
        => $"- [{comment} ({shortSHA})]({commitUrl})";

    public virtual string FormatDiffHeader(string diffUrl)
        => $"[View Complete Diff of Changes]({diffUrl})";

    public virtual string FormatPRListItem(string comment, string prNumber, string prUrl)
    {
        // Replace "#{prNumber}" with "{prNumber}" so that AzDO won't linkify it
        comment = comment.Replace($"#{prNumber}", prNumber);

        return $"- [{comment}]({prUrl})";
    }

    public virtual string GetCommitSectionHeader()
        => "### Commits since last PR:";

    public virtual string GetPRSectionHeader()
        => "### Merged PRs:";
}
