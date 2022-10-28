// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder;

public interface IPRLogFormatter
{
    string FormatChangesHeader(string previous, string previousUrl, string current, string currentUrl);
    string FormatDiffHeader(string diffUrl);
    string GetCommitSectionHeader();
    string GetPRSectionHeader();
    string FormatCommitListItem(string comment, string shortSHA, string commitUrl);
    string FormatPRListItem(string comment, string prNumber, string prUrl);
}
