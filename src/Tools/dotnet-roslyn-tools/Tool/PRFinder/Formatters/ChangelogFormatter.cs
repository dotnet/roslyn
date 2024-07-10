// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.RoslynTools.PRFinder.Formatters
{
    internal class ChangelogFormatter : IPRLogFormatter
    {
        public string FormatChangesHeader(string previous, string previousUrl, string current, string currentUrl)
            => $"### Changes from [{previous}]({previousUrl}) to [{current}]({currentUrl}):";

        public virtual string FormatCommitListItem(string comment, string shortSHA, string commitUrl)
            => $"- [{comment} ({shortSHA})]({commitUrl})";

        public virtual string FormatDiffHeader(string diffUrl)
            => $"[View Complete Diff of Changes]({diffUrl})";

        public virtual string FormatPRListItem(string comment, string prNumber, string prUrl)
        {
            prNumber = prNumber.StartsWith("#")
                ? prNumber
                : $"#{prNumber}";

            return $"  * {comment} (PR: [{prNumber}]({prUrl}))";
        }

        public virtual string GetCommitSectionHeader()
            => "### Commits since last PR:";

        public virtual string GetPRSectionHeader()
            => "### Merged PRs:";
    }
}
